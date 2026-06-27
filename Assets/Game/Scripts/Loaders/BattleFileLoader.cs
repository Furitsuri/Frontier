using Frontier.Battle;
using Frontier.Combat;
using Frontier.Combat.Skill;
using Frontier.Entities;
using Frontier.Registries;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Zenject;

namespace Frontier.Loaders
{
    public class BattleFileLoader : MonoBehaviour
    {
        [Header( "各味方キャラクターのパラメータ参照先" )]
        [SerializeField] public string[] PlayerParamFilePath;

        [Header( "近接攻撃時のカメラパラメータの参照先" )]
        [SerializeField] public string CloseAtkCameraParamFilePath;

        [Header( "遠隔攻撃時のカメラパラメータの参照先" )]
        [SerializeField] public string RangedAtkCameraParamFilePath;

        [Inject] private BattleRoutineController _btlRtnCtrl = null;
        [Inject] private CharacterFactory _characterFactory = null;
        [Inject] private FilePathRegistry _filePathReg      = null;

        private void Awake()
        {
            int fieldCount = typeof( CharacterDeployData ).GetFields().Length;
            Debug.Assert(
                fieldCount == ( int ) CharacterDeployParamTag.NUM,
                $"CharacterDeployData のフィールド数 ({fieldCount}) が CharacterDeployParamTag.NUM ({( int ) CharacterDeployParamTag.NUM}) と一致しません。" );
        }

        [Serializable]
        public struct CharacterDeployData
        {
            public int Prefab;
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
            public int ThinkType;
            public int[] Skills;
        }

        [System.Serializable]
        public class PlayerParamContainer
        {
            public CharacterDeployData[] CharacterStatus;
        }

        [System.Serializable]
        public class CharacterStatusContainer
        {
            public CharacterDeployData[] CharacterStatuses;
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
                new List<string> { _filePathReg.GetEnemyParamFilePath( stageIndex ) },
                new List<string> { _filePathReg.GetOtherParamFilePath( stageIndex ) },
            };

            // プレイヤーは既に生成されているためスキップ
            for( int i = ( int ) CHARACTER_TAG.PLAYER + 1; i < ( int ) CHARACTER_TAG.NUM; ++i )
            {
                if( ParamFilePaths[i].Count <= 0 ) continue;
                // そのステージに該当陣営のキャラクターが配置されていない場合はスキップ
                if( !File.Exists( ParamFilePaths[i][0] ) ) continue;

                // JSONファイルの読み込み
                string json = File.ReadAllText( ParamFilePaths[i][0] );
                // JSONデータのデシリアライズ
                var dataContainer = JsonUtility.FromJson<CharacterStatusContainer>( json );
                if( dataContainer == null ) { return; }

                // デシリアライズされたデータを配列に格納
                foreach( var status in dataContainer.CharacterStatuses )
                {
                    int prefabIndex = status.Prefab;
                    Character chara = _characterFactory.CreateCharacter( ( CHARACTER_TAG ) i, prefabIndex, status );
                    if( null == chara ) { continue; }

                    _btlRtnCtrl.BtlCharaCdr.AddCharacterToList( chara, status.InitGridIndex, ( Direction ) status.InitDir );
                }
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

                _btlRtnCtrl.BtlCharaCdr.AddCharacterToList( player, 0, Direction.NONE );
            }
            else
            {
                Enemy enemy = _characterFactory.CreateCharacter( CHARACTER_TAG.ENEMY, prefabIndex ) as Enemy;
                if( enemy == null ) { return; }

                enemy.Init();
                enemy.GetStatusRef = param;

                _btlRtnCtrl.BtlCharaCdr.AddCharacterToList( enemy, 0, Direction.NONE );
            }
        }
#endif  // DEVELOPMENT_BUILD || UNITY_EDITOR
    }
}