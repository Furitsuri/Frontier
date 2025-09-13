using Frontier.Stage;
using System;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    /// <summary>
    /// StageDataのシリアライズとデシリアライズを行うクラス
    /// </summary>
    /// <remarks>
    /// StageDataはMonoBehaviourを継承しているため、JsonUtilityで直接シリアライズできない。
    /// そのため、StageDataのデータを保持するクラスを作成し、それをシリアライズする。
    /// </remarks>
    static public class StageDataSerializer
    {
        private static string FolderPath => Path.Combine(Application.persistentDataPath, "StageData");

        static public bool Save(StageData data, string fileName)
        {
            try
            {
                Directory.CreateDirectory(FolderPath);
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(Path.Combine(FolderPath, $"{fileName}.json"), json);

                Debug.Log(JsonUtility.ToJson(data, true));

                return true; // 成功
            }
            catch (Exception e)
            {
                Debug.LogError($"ステージデータの保存に失敗しました: {e.Message}");
                return false; // 失敗
            }
        }


        static public StageData Load(string fileName)
        {
            string path = Path.Combine(FolderPath, $"{fileName}.json");
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<StageData>(json);
        }
    }
}

#endif // UNITY_EDITOR