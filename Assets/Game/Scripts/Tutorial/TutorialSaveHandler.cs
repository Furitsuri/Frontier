using System.IO;
using UnityEngine;
using Frontier.Tutorial;

public class TutorialSaveHandler : ISaveHandler<TutorialSaveData>
{
    private readonly string _filePath;

    public TutorialSaveHandler()
    {
        _filePath = Path.Combine(Application.persistentDataPath, "tutorial.json");
    }

    public void Save(TutorialSaveData data)
    {
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(_filePath, json);
    }

    public TutorialSaveData Load()
    {
        if (!File.Exists(_filePath))
            return new TutorialSaveData();

        string json = File.ReadAllText(_filePath);
        return JsonUtility.FromJson<TutorialSaveData>(json);
    }
}