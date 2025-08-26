using Frontier.Battle;
using Frontier.Combat;
using Frontier.Entities;
using UnityEngine;
using System;
using System.IO;
using TMPro.SpriteAssetUtilities;
using Palmmedia.ReportGenerator.Core.Common;
using System.Collections.Generic;
using Newtonsoft.Json;
using Frontier.Stage;
using Zenject;
using static UnityEngine.EventSystems.EventTrigger;

namespace Frontier
{
    public class BattleFileLoader : MonoBehaviour
    {
        [Header("各味方キャラクターのプレハブ")]
        [SerializeField]
        public GameObject[] PlayersPrefab;

        [Header("各敵キャラクターのプレハブ")]
        [SerializeField]
        public GameObject[] EnemiesPrefab;

        [Header("各第三勢力キャラクターのプレハブ")]
        [SerializeField]
        public GameObject[] OthersPrefab;

        [Header("各味方キャラクターのパラメータ参照先")]
        [SerializeField]
        public string[] PlayerParamFilePath;

        [Header("各敵キャラクターのパラメータ参照先")]
        [SerializeField]
        public string[] EnemyParamFilePath;

        [Header("各第三軍勢キャラクターのパラメータ参照先")]
        [SerializeField]
        public string[] OtherParamFilePath;

        [Header("各スキルデータのパラメータ参照先")]
        [SerializeField]
        public string SkillDataFilePath;

        [Header("近接攻撃時のカメラパラメータの参照先")]
        [SerializeField]
        public string CloseAtkCameraParamFilePath;

        [Header("遠隔攻撃時のカメラパラメータの参照先")]
        [SerializeField]
        public string RangedAtkCameraParamFilePath;

        private HierarchyBuilderBase _hierarchyBld = null;

        // バトルマネージャ
        private BattleRoutineController _btlRtnCtrl;

        [System.Serializable]
        public struct CharacterParamData
        {
            public int CharacterTag;
            public int CharacterIndex;
            public int MaxHP;
            public int Atk;
            public int Def;
            public int MoveRange;
            public int AtkRange;
            public int ActGaugeMax;
            public int ActRecovery;
            public int InitGridIndex;
            public int InitDir;
            public int Prefab;
            public int ThinkType;
            public int[] Skills;
        }

        [System.Serializable]
        public struct FileSkillData
        {
            public string Name;
            public int Cost;
            public int Type;
            public int Duration;
            public float AddAtkMag;
            public float AddDefMag;
            public int AddAtkNum;
            public float Param1;
            public float Param2;
            public float Param3;
            public float Param4;
        }

        [System.Serializable]
        public class PlayerParamContainer
        {
            public CharacterParamData[] CharacterParams;
        }

        [System.Serializable]
        public class CharacterParamContainer
        {
            public CharacterParamData[] CharacterParams;
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
        /// Diコンテナから引数を注入します
        /// </summary>
        /// <param name="btlRtnCtrl"></param>
        /// <param name="hierarchyBld"></param>
        [Inject]
        void Construct(BattleRoutineController btlRtnCtrl, HierarchyBuilderBase hierarchyBld)
        {
            _btlRtnCtrl     = btlRtnCtrl;
            _hierarchyBld   = hierarchyBld;
        }

        /// <summary>
        /// 該当ステージの全キャラクター情報をロードし、バトルマネージャ上に設置します
        /// </summary>
        /// <param name="stageIndex">ステージナンバー</param>
        public void CharacterLoad(int stageIndex)
        {
            List<string>[] ParamFilePaths = new List<string>[]
            {
                new List<string>(PlayerParamFilePath),
                new List<string>(EnemyParamFilePath),
                new List<string>(OtherParamFilePath),
            };

            List<GameObject>[] CharacterPrefabs = new List<GameObject>[]
            {
                new List<GameObject>(PlayersPrefab),
                new List<GameObject>(EnemiesPrefab),
                new List<GameObject>(OthersPrefab),
            };

            for (int i = 0; i < (int)CHARACTER_TAG.NUM; ++i)
            {
                if ( ParamFilePaths[i].Count <= 0 ) continue;

                // JSONファイルの読み込み
                string json = File.ReadAllText(ParamFilePaths[i][stageIndex]);
                // JSONデータのデシリアライズ
                var dataContainer = JsonUtility.FromJson<CharacterParamContainer>(json);
                if (dataContainer == null) return;
                
                // デシリアライズされたデータを配列に格納
                foreach ( var param in dataContainer.CharacterParams )
                {
                    int prefabIndex = param.Prefab;
                    Character chara = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<Character>(CharacterPrefabs[i][prefabIndex], true, false, typeof(Character).Name);
                    chara.Init();

                    // 弾オブジェクトが設定されていれば生成
                    // 使用時まで非アクティブにする
                    if (chara.BulletObject != null)
                    {
                        Bullet bullet = _hierarchyBld.CreateComponentNestedNewDirectoryWithDiContainer<Bullet>(chara.BulletObject, chara.gameObject, "Bullet", false, false);
                        chara.SetBullet(bullet);
                    }

                    chara.Params.CharacterParam.Apply( param ); // ファイルから読み込んだパラメータを設定

                    if ( !chara.Params.CharacterParam.IsMatchCharacterTag(CHARACTER_TAG.PLAYER) )
                    {
                        var npc = chara as Npc;
                        npc.SetThinkType((Npc.ThinkingType)param.ThinkType);
                    }

                    _btlRtnCtrl.BtlCharaCdr.AddCharacterToList(chara);

                }
            }
        }

        /// <summary>
        /// 各スキルのデータをロードします
        /// </summary>
        public void SkillDataLord()
        {
            string json = File.ReadAllText(SkillDataFilePath);
            var dataContainer = JsonUtility.FromJson<SkillDataContainer>(json);
            if (dataContainer == null) return;
            for (int i = 0; i < (int)SkillsData.ID.SKILL_NUM; ++i)
            {
                ApplySkillsData(ref SkillsData.data[i], dataContainer.SkillsData[i]);
            }
        }

        /// <summary>
        /// カメラのパラメータを読み込みます
        /// </summary>
        public void CameraParamLord( BattleCameraController cameraController )
        {
            string json = File.ReadAllText(CloseAtkCameraParamFilePath);
            var dataContainer = JsonConvert.DeserializeObject<CameraParamContainer>(json);
            if (dataContainer == null) return;
            List<BattleCameraController.CameraParamData[]> closeParams = dataContainer.CameraParams;

            json = File.ReadAllText(RangedAtkCameraParamFilePath);
            dataContainer = JsonConvert.DeserializeObject<CameraParamContainer>(json);
            if (dataContainer == null) return;
            List<BattleCameraController.CameraParamData[]> rangedParams = dataContainer.CameraParams;

            cameraController.SetCameraParamDatas(closeParams, rangedParams);
        }

        /// <summary>
        /// スキルデータを適応させます
        /// </summary>
        /// <param name="data">適応先のスキルデータ</param>
        /// <param name="fdata">適応元のファイルから読み取ったスキルデータ</param>
        private void ApplySkillsData(ref SkillsData.Data data, in FileSkillData fdata)
        {
            data.Name = fdata.Name;
            data.Cost = fdata.Cost;
            data.Type = (SkillsData.SituationType)fdata.Type;
            data.Duration = fdata.Duration;
            data.AddAtkMag = fdata.AddAtkMag;
            data.AddDefMag = fdata.AddDefMag;
            data.AddAtkNum = fdata.AddAtkNum;
            data.Param1 = fdata.Param1;
            data.Param2 = fdata.Param2;
            data.Param3 = fdata.Param3;
            data.Param4 = fdata.Param4;
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        /// <summary>
        /// デバッグ用にユニットを生成します
        /// </summary>
        public void DebugBattleLoadUnit(int prefabIndex, ref CharacterParameter param)
        {
            if (param.characterTag == CHARACTER_TAG.PLAYER)
            {
                Player player = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<Player>(PlayersPrefab[prefabIndex], true, false, typeof(Character).Name);
                if (player == null) return;

                player.Init();
                player.Params.CharacterParam = param;

                _btlRtnCtrl.BtlCharaCdr.AddCharacterToList(player);
            }
            else
            {
                Enemy enemy = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<Enemy>(PlayersPrefab[prefabIndex], true, false, typeof(Character).Name);
                if (enemy == null) return;

                enemy.Init();
                enemy.Params.CharacterParam = param;

                _btlRtnCtrl.BtlCharaCdr.AddCharacterToList(enemy);
            }
        }
#endif  // DEVELOPMENT_BUILD || UNITY_EDITOR
            }
}