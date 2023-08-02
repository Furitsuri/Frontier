using UnityEngine;
using System;
using System.IO;
using TMPro.SpriteAssetUtilities;
using Palmmedia.ReportGenerator.Core.Common;
using System.Collections.Generic;
using Newtonsoft.Json;
using static SkillsData;

public class FileReadManager : Singleton<FileReadManager>
{
    [SerializeField]
    public GameObject[] PlayersPrefab;

    [SerializeField]
    public GameObject[] EnemiesPrefab;

    [SerializeField]
    public string[] PlayerParamFilePath;

    [SerializeField]
    public string[] EnemyParamFilePath;

    [SerializeField]
    public string SkillDataFilePath;

    [SerializeField]
    public string CloseAtkCameraParamFilePath;

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

    /// <summary>
    /// プレイヤー情報をロードし、バトルマネージャ上に設置します
    /// </summary>
    /// <param name="stageIndex">ステージナンバー</param>
    public void PlayerLoad( int stageIndex )
    {
        // JSONファイルの読み込み
        string json = File.ReadAllText(PlayerParamFilePath[stageIndex]);
        // JSONデータのデシリアライズ
        var dataContainer = JsonUtility.FromJson<PlayerParamContainer>(json);
        if (dataContainer == null) return;
        // デシリアライズされたデータを配列に格納
        CharacterParamData[] Params = dataContainer.CharacterParams;
        
        for( int i = 0; i < Params.Length; ++i )
        {
            int prefabIndex = Params[i].Prefab;
            GameObject playerObject = Instantiate(PlayersPrefab[prefabIndex]);
            if (playerObject == null) continue;

            Player player = playerObject.GetComponent<Player>();
            if (player == null) continue;

            // ファイルから読み込んだパラメータを設定
            ApplyCharacterParams(ref player.param, Params[i]);
            player.Init();
            playerObject.SetActive(true);

            BattleManager.Instance.AddPlayerToList(player);
        }
    }

    /// <summary>
    /// 敵情報をロードし、バトルマネージャ上に設置します
    /// </summary>
    /// <param name="stageIndex">ステージナンバー</param>
    public void EnemyLord( int stageIndex )
    {
        string json = File.ReadAllText(EnemyParamFilePath[stageIndex]);
        var dataContainer = JsonUtility.FromJson<NpcParamContainer>(json);
        if( dataContainer == null ) return;
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
            enemy.Init((Enemy.ThinkingType)Params[i].ThinkType);
            enemyObject.SetActive(true);

            BattleManager.Instance.AddEnemyToList(enemy);
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
        for( int i = 0; i < (int)ID.SKILL_NUM; ++i )
        {
            ApplySkillsData(ref SkillsData.data[i], dataContainer.SkillsData[i]);
        }
    }

    /// <summary>
    /// カメラのパラメータを読み込みます
    /// </summary>
    public void CameraParamLord()
    {
        string json = File.ReadAllText(CloseAtkCameraParamFilePath);
        var dataContainer = JsonConvert.DeserializeObject<CameraParamContainer>(json);
        if (dataContainer == null) return;
        List <BattleCameraController.CameraParamData[]> closeParams = dataContainer.CameraParams;

        json = File.ReadAllText(RangedAtkCameraParamFilePath);
        dataContainer = JsonConvert.DeserializeObject<CameraParamContainer>(json);
        if (dataContainer == null) return;
        List<BattleCameraController.CameraParamData[]> rangedParams = dataContainer.CameraParams;

        BattleCameraController.Instance.SetCameraParamDatas(closeParams, rangedParams);
    }

    /// <summary>
    /// キャラクターパラメータを適応させます
    /// </summary>
    /// <param name="param">適応先のキャラクターパラメータ</param>
    /// <param name="fdata">適応元のキャラクターパラメータ</param>
    private void ApplyCharacterParams( ref Character.Parameter param, in CharacterParamData fdata )
    {
        param.characterTag                          = (Character.CHARACTER_TAG)fdata.CharacterTag;
        param.characterIndex                        = fdata.CharacterIndex;
        param.CurHP = param.MaxHP                   = fdata.MaxHP;
        param.Atk                                   = fdata.Atk;
        param.Def                                   = fdata.Def;
        param.moveRange                             = fdata.MoveRange;
        param.attackRange                           = fdata.AtkRange;
        param.curActionGauge = param.maxActionGauge = fdata.ActGaugeMax;
        param.recoveryActionGauge                   = fdata.ActRecovery;
        param.initGridIndex                         = fdata.InitGridIndex;
        param.initDir                               = (Constants.Direction)fdata.InitDir;
        for( int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i )
        {
            param.equipSkills[i] = (SkillsData.ID)fdata.Skills[i];
        }
    }

    /// <summary>
    /// スキルデータを適応させます
    /// </summary>
    /// <param name="data">適応先のスキルデータ</param>
    /// <param name="fdata">適応元のファイルから読み取ったスキルデータ</param>
    private void ApplySkillsData( ref SkillsData.Data data, in FileSkillData fdata )
    {
        data.Name       = fdata.Name;
        data.Cost       = fdata.Cost;
        data.Type       = ( SkillsData.SituationType )fdata.Type;
        data.Duration   = fdata.Duration;
        data.AddAtkMag  = fdata.AddAtkMag;
        data.AddDefMag  = fdata.AddDefMag;
        data.AddAtkNum  = fdata.AddAtkNum;
        data.Param1     = fdata.Param1;
        data.Param2     = fdata.Param2;
        data.Param3     = fdata.Param3;
        data.Param4     = fdata.Param4;
}
}
