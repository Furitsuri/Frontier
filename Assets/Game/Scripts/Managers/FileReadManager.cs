using UnityEngine;
using System;
using System.IO;
using TMPro.SpriteAssetUtilities;
using Palmmedia.ReportGenerator.Core.Common;
using System.Collections.Generic;
using Newtonsoft.Json;
using static Frontier.SkillsData;
using Frontier.Stage;

namespace Frontier
{
    public class FileReadManager : Singleton<FileReadManager>
    {
        [Header("バトルマネージャ")]
        [SerializeField]
        private BattleManager _btlMgr;

        [Header("各味方キャラクターのプレハブ")]
        [SerializeField]
        public GameObject[] PlayersPrefab;

        [Header("各敵キャラクターのプレハブ")]
        [SerializeField]
        public GameObject[] EnemiesPrefab;

        [Header("各味方キャラクターのパラメータ参照先")]
        [SerializeField]
        public string[] PlayerParamFilePath;

        [Header("各敵キャラクターのパラメータ参照先")]
        [SerializeField]
        public string[] EnemyParamFilePath;

        [Header("各スキルデータのパラメータ参照先")]
        [SerializeField]
        public string SkillDataFilePath;

        [Header("近接攻撃時のカメラパラメータの参照先")]
        [SerializeField]
        public string CloseAtkCameraParamFilePath;

        [Header("遠隔攻撃時のカメラパラメータの参照先")]
        [SerializeField]
        public string RangedAtkCameraParamFilePath;

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
        public struct NpcParamData
        {
            public CharacterParamData Param;
            public int ThinkType;
        }

        [System.Serializable]
        public class PlayerParamContainer
        {
            public CharacterParamData[] CharacterParams;
        }

        [System.Serializable]
        public class NpcParamContainer
        {
            public NpcParamData[] CharacterParams;
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

        protected override void OnStart()
        {
            Debug.Assert(_btlMgr != null);

            base.OnStart();
        }

        /// <summary>
        /// プレイヤー情報をロードし、バトルマネージャ上に設置します
        /// </summary>
        /// <param name="stageIndex">ステージナンバー</param>
        public void PlayerLoad(int stageIndex, float gridLength)
        {
            // JSONファイルの読み込み
            string json = File.ReadAllText(PlayerParamFilePath[stageIndex]);
            // JSONデータのデシリアライズ
            var dataContainer = JsonUtility.FromJson<PlayerParamContainer>(json);
            if (dataContainer == null) return;
            // デシリアライズされたデータを配列に格納
            CharacterParamData[] Params = dataContainer.CharacterParams;

            for (int i = 0; i < Params.Length; ++i)
            {
                int prefabIndex = Params[i].Prefab;
                GameObject playerObject = Instantiate(PlayersPrefab[prefabIndex]);
                if (playerObject == null) continue;

                Player player = playerObject.GetComponent<Player>();
                if (player == null) continue;

                // ファイルから読み込んだパラメータを設定
                ApplyCharacterParams(ref player.param, Params[i]);
                player.Init(_btlMgr, ManagerProvider.Instance.GetService<StageController>());
                playerObject.SetActive(true);

                _btlMgr.AddPlayerToList(player);
            }
        }

        /// <summary>
        /// 敵情報をロードし、バトルマネージャ上に設置します
        /// </summary>
        /// <param name="stageIndex">ステージナンバー</param>
        public void EnemyLord(int stageIndex, float gridLength)
        {
            string json = File.ReadAllText(EnemyParamFilePath[stageIndex]);
            var dataContainer = JsonUtility.FromJson<NpcParamContainer>(json);
            if (dataContainer == null) return;
            NpcParamData[] Params = dataContainer.CharacterParams;

            for (int i = 0; i < Params.Length; ++i)
            {
                int prefabIndex = Params[i].Param.Prefab;
                GameObject enemyObject = Instantiate(EnemiesPrefab[prefabIndex]);
                if (enemyObject == null) continue;

                Enemy enemy = enemyObject.GetComponent<Enemy>();
                if (enemy == null) continue;

                // ファイルから読み込んだパラメータを設定
                ApplyCharacterParams(ref enemy.param, Params[i].Param);
                enemy.Init(_btlMgr, ManagerProvider.Instance.GetService<StageController>());
                enemy.SetThinkType((Enemy.ThinkingType)Params[i].ThinkType);
                enemyObject.SetActive(true);

                _btlMgr.AddEnemyToList(enemy);
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
            for (int i = 0; i < (int)ID.SKILL_NUM; ++i)
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
        /// キャラクターパラメータを適応させます
        /// </summary>
        /// <param name="param">適応先のキャラクターパラメータ</param>
        /// <param name="fdata">適応元のキャラクターパラメータ</param>
        private void ApplyCharacterParams(ref Character.Parameter param, in CharacterParamData fdata)
        {
            param.characterTag = (Character.CHARACTER_TAG)fdata.CharacterTag;
            param.characterIndex = fdata.CharacterIndex;
            param.CurHP = param.MaxHP = fdata.MaxHP;
            param.Atk = fdata.Atk;
            param.Def = fdata.Def;
            param.moveRange = fdata.MoveRange;
            param.attackRange = fdata.AtkRange;
            param.curActionGauge = param.maxActionGauge = fdata.ActGaugeMax;
            param.recoveryActionGauge = fdata.ActRecovery;
            param.initGridIndex = fdata.InitGridIndex;
            param.initDir = (Constants.Direction)fdata.InitDir;
            for (int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i)
            {
                param.equipSkills[i] = (SkillsData.ID)fdata.Skills[i];
            }
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
        public void DebugBattleLoadUnit(int prefabIndex, ref Character.Parameter param)
        {
            GameObject unitObject = Instantiate(PlayersPrefab[prefabIndex]);
            if (unitObject == null) return;

            if (param.characterTag == Character.CHARACTER_TAG.PLAYER)
            {
                Player player = unitObject.GetComponent<Player>();
                if (player == null) return;

                player.param = param;
                player.Init(_btlMgr, ManagerProvider.Instance.GetService<StageController>());
                unitObject.SetActive(true);

                _btlMgr.AddPlayerToList(player);
            }
            else
            {
                Enemy enemy = unitObject.GetComponent<Enemy>();
                if (enemy == null) return;

                enemy.param = param;
                enemy.Init(_btlMgr, ManagerProvider.Instance.GetService<StageController>());
                unitObject.SetActive(true);

                _btlMgr.AddEnemyToList(enemy);
            }
        }
#endif  // DEVELOPMENT_BUILD || UNITY_EDITOR
    }
}