using UnityEngine;
using System;
using System.IO;
using TMPro.SpriteAssetUtilities;
using Palmmedia.ReportGenerator.Core.Common;
using System.Collections.Generic;
using Newtonsoft.Json;

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
    /// 
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

    private void ApplyCharacterParams( ref Character.Parameter param, in CharacterParamData data )
    {
        param.characterTag              = (Character.CHARACTER_TAG)data.CharacterTag;
        param.characterIndex            = data.CharacterIndex;
        param.CurHP = param.MaxHP       = data.MaxHP;
        param.Atk                       = data.Atk;
        param.Def                       = data.Def;
        param.moveRange                 = data.MoveRange;
        param.attackRange               = data.AtkRange;
        param.initGridIndex             = data.InitGridIndex;
        param.initDir                   = (Constants.Direction)data.InitDir;
    }
}
