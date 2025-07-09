using Frontier.Stage;
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
    public static class StageDataSerializer
    {
        private static string FolderPath => Path.Combine(Application.persistentDataPath, "StageData");

        public static void Save(StageData data, string fileName)
        {
            Directory.CreateDirectory(FolderPath);
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(Path.Combine(FolderPath, $"{fileName}.json"), json);
        }

        public static StageData Load(string fileName)
        {
            string path = Path.Combine(FolderPath, $"{fileName}.json");
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<StageData>(json);
        }
    }
}

#endif // UNITY_EDITOR