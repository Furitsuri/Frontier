using Frontier.Battle;
using Frontier.Combat;
using Frontier.Entities;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Frontier.Stage;
using Zenject;
using Frontier.Combat.Skill;
using UnityEngine.UI;
using Frontier.Registries;

namespace Frontier
{
    public class BattleFileLoader : MonoBehaviour
    {
        [Header( "各味方キャラクターのパラメータ参照先" )]
        [SerializeField] public string[] PlayerParamFilePath;

        [Header( "各敵キャラクターのパラメータ参照先" )]
        [SerializeField] public string[] EnemyParamFilePath;

        [Header( "各第三軍勢キャラクターのパラメータ参照先" )]
        [SerializeField] public string[] OtherParamFilePath;

        [Header( "各スキルデータのパラメータ参照先" )]
        [SerializeField] public string SkillDataFilePath;

        [Header( "近接攻撃時のカメラパラメータの参照先" )]
        [SerializeField] public string CloseAtkCameraParamFilePath;

        [Header( "遠隔攻撃時のカメラパラメータの参照先" )]
        [SerializeField] public string RangedAtkCameraParamFilePath;

        [Inject] private BattleRoutineController _btlRtnCtrl = null;
        [Inject] private CharacterFactory _characterFactory = null;

        [Serializable]
        public struct CharacterStatusData
        {
            public int CharacterTag;
            public int CharacterIndex;
            public string Name;
            public int Level;
            public int MaxHP;
            public int Atk;
            public int Def;
            public int MoveRange;
            public int JumpForce;
            public int AtkRange;
            public int ActGaugeMax;
            public int ActRecovery;
            public int InitGridIndex;
            public int InitDir;
            public int Prefab;
            public int ThinkType;
            public int[] Skills;
        }

        [Serializable]
        public struct FileSkillData
        {
            public string Name;
            public int Cost;
            public int Type;
            public int Flags;
            public int Duration;
            public float AddAtkMag;
            public float AddDefMag;
            public int AddAtkNum;
            public float Param1;
            public float Param2;
            public float Param3;
            public float Param4;
            public string ExplainTextKey;
        }

        [System.Serializable]
        public class PlayerParamContainer
        {
            public CharacterStatusData[] CharacterStatus;
        }

        [System.Serializable]
        public class CharacterStatusContainer
        {
            public CharacterStatusData[] CharacterStatuses;
        }

        [System.Serializable]
        public class SkillDataContainer
        {
            public FileSkillData[] SkillsData;
        }

        [System.Serializable]
        public class CameraParamContainer
        {
            public List<BattleCameraController.CameraParamData[]> CameraParams;
        }

        /// <summary>
        /// 該当ステージの全キャラクター情報をロードし、バトルマネージャ上に設置します
        /// </summary>
        /// <param name="stageIndex">ステージナンバー</param>
        public void CharacterLoad( int stageIndex )
        {
            List<string>[] ParamFilePaths = new List<string>[]
            {
                new List<string>(PlayerParamFilePath),
                new List<string>(EnemyParamFilePath),
                new List<string>(OtherParamFilePath),
            };

            // プレイヤーは既に生成されているためスキップ
            for( int i = ( int ) CHARACTER_TAG.PLAYER + 1; i < ( int ) CHARACTER_TAG.NUM; ++i )
            {
                if( ParamFilePaths[i].Count <= 0 ) continue;

                // JSONファイルの読み込み
                string json = File.ReadAllText( ParamFilePaths[i][stageIndex] );
                // JSONデータのデシリアライズ
                var dataContainer = JsonUtility.FromJson<CharacterStatusContainer>( json );
                if( dataContainer == null ) { return; }

                // デシリアライズされたデータを配列に格納
                foreach( var status in dataContainer.CharacterStatuses )
                {
                    int prefabIndex = status.Prefab;
                    Character chara = _characterFactory.CreateCharacter( ( CHARACTER_TAG ) i, prefabIndex, status );
                    if( null == chara ) { continue; }

                    _btlRtnCtrl.BtlCharaCdr.AddCharacterToList( chara );
                }
            }
        }

        /// <summary>
        /// 各スキルのデータをロードします
        /// </summary>
        public void LoadSkillsData()
        {
            string json = File.ReadAllText( SkillDataFilePath );
            var dataContainer = JsonUtility.FromJson<SkillDataContainer>( json );
            if( dataContainer == null ) return;
            for( int i = 0; i < ( int ) ID.SKILL_NUM; ++i )
            {
                ApplySkillsData( ref SkillsData.data[i], dataContainer.SkillsData[i] );
            }
        }

        /// <summary>
        /// カメラのパラメータを読み込みます
        /// </summary>
        public void LoadCameraParams( BattleCameraController cameraController )
        {
            string json = File.ReadAllText( CloseAtkCameraParamFilePath );
            var dataContainer = JsonConvert.DeserializeObject<CameraParamContainer>( json );
            if( dataContainer == null ) return;
            List<BattleCameraController.CameraParamData[]> closeParams = dataContainer.CameraParams;

            json = File.ReadAllText( RangedAtkCameraParamFilePath );
            dataContainer = JsonConvert.DeserializeObject<CameraParamContainer>( json );
            if( dataContainer == null ) return;
            List<BattleCameraController.CameraParamData[]> rangedParams = dataContainer.CameraParams;

            cameraController.SetCameraParamDatas( closeParams, rangedParams );
        }

        /// <summary>
        /// スキルデータを適応させます
        /// </summary>
        /// <param name="data">適応先のスキルデータ</param>
        /// <param name="fdata">適応元のファイルから読み取ったスキルデータ</param>
        private void ApplySkillsData( ref SkillsData.Data data, in FileSkillData fdata )
        {
            data.Name = fdata.Name;
            data.Cost = fdata.Cost;
            data.Type = ( SituationType ) fdata.Type;
            data.Flags = fdata.Flags;
            data.Duration = fdata.Duration;
            data.AddAtkMag = fdata.AddAtkMag;
            data.AddDefMag = fdata.AddDefMag;
            data.AddAtkNum = fdata.AddAtkNum;
            data.Param1 = fdata.Param1;
            data.Param2 = fdata.Param2;
            data.Param3 = fdata.Param3;
            data.Param4 = fdata.Param4;
            data.ExplainTextKey = fdata.ExplainTextKey;
            // TODO : nullアクセス防止の暫定対応。後で消すこと
            if( null == data.ExplainTextKey ) { data.ExplainTextKey = ""; }
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        /// <summary>
        /// デバッグ用にユニットを生成します
        /// </summary>
        public void DebugBattleLoadUnit( int prefabIndex, ref Status param )
        {
            if( param.characterTag == CHARACTER_TAG.PLAYER )
            {
                Player player = _characterFactory.CreateCharacter( CHARACTER_TAG.PLAYER, prefabIndex ) as Player;
                if( player == null ) { return; }

                player.Init();
                player.GetStatusRef = param;

                _btlRtnCtrl.BtlCharaCdr.AddCharacterToList( player );
            }
            else
            {
                Enemy enemy = _characterFactory.CreateCharacter( CHARACTER_TAG.ENEMY, prefabIndex ) as Enemy;
                if( enemy == null ) { return; }

                enemy.Init();
                enemy.GetStatusRef = param;

                _btlRtnCtrl.BtlCharaCdr.AddCharacterToList( enemy );
            }
        }
#endif  // DEVELOPMENT_BUILD || UNITY_EDITOR
    }
}