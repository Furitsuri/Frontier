using UnityEngine;

public static class TutorialSaveSystem
{
    private const string KEY = "TutorialFlags";

    public static int Flags
    {
        get => PlayerPrefs.GetInt(KEY, 0);
        set => PlayerPrefs.SetInt(KEY, value);
    }

    /// <summary>
    /// チュートリアルが既に表示されたかどうかを取得します
    /// </summary>
    /// <param name="data"></param>
    /// <returns>表示済みか否か</returns>
    public static bool HasSeen(TutorialData data)
    {
        return (Flags & (1 << data.GetFlagBitIdx)) != 0;
    }

    /// <summary>
    /// チュートリアルを表示済みとしてマークします
    /// </summary>
    /// <param name="data"></param>
    public static void MarkAsSeen(TutorialData data)
    {
        Flags |= (1 << data.GetFlagBitIdx);
        PlayerPrefs.SetInt(KEY, Flags);
    }

    public static void ResetAll()
    {
        PlayerPrefs.SetInt(KEY, 0);
        PlayerPrefs.Save();
    }
}