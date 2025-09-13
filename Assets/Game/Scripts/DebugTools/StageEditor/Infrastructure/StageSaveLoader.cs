using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using Frontier.Stage;

public class StageSaveLoader
{
    private const string SaveFolder = "UserStages";

    static public void Save(StageData data, string filename)
    {
        var json = JsonUtility.ToJson(data, true);
        File.WriteAllText(Path.Combine(Application.persistentDataPath, SaveFolder, filename + ".json"), json);
    }

    static public StageData Load(string filename)
    {
        var path = Path.Combine(Application.persistentDataPath, SaveFolder, filename + ".json");
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return JsonUtility.FromJson<StageData>(json);
    }
}