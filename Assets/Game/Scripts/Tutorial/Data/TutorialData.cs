using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TutorialData", menuName = "ScriptableObjects/Tutorial/TutorialData")]
public class TutorialData : ScriptableObject
{
    [SerializeField]
    private TutorialFacade.TriggerType _triggerType; // チュートリアルのトリガータイプ

    [SerializeField]
    private string _title;         // チュートリアルのタイトル

    [SerializeField]
    private int flagBitIndex;      // 0〜31までの番号（2の冪になる）

    [SerializeField]
    private List<TutorialElement> _tutorialElements = new List<TutorialElement>();

    public TutorialFacade.TriggerType TriggerType => _triggerType;

    public int GetFlagBitIdx => flagBitIndex;

    /// <summary>
    /// チュートリアルの表示要素を取得します
    /// </summary>
    public List<TutorialElement> GetTutorialElements => _tutorialElements;
}

/// <summary>
/// チュートリアル閲覧時、各ページ内に表示される要素
/// </summary>
[System.Serializable]
public class TutorialElement
{
    [SerializeField]
    private string _headline;       // 見出し

    [SerializeField]
    private string _explain;        // 表示メッセージ

    [SerializeField]
    private Sprite _image;          // チュートリアル用の画像

    public string Headline => _headline;
    public string Explain => _explain;
    public Sprite Image => _image;
}