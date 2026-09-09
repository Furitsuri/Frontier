using System.Collections.Generic;

namespace Frontier.UI
{
    /// <summary>
    /// トークウィンドウで表示する1件の発言データです。
    /// Assets/Resources/TalkData/{シーン名}.json からデシリアライズされます。
    /// </summary>
    public class TalkEntryData
    {
        public string Id;           // シーン内で発言を一意に識別するキー
        public string SpeakerName;  // 話者名
        public string PortraitKey;  // Resources.Load<Sprite>用のキー(空文字/未設定なら画像非表示)
        public string Message;      // セリフ本文
    }

    /// <summary>
    /// 1シーン分のトークウィンドウ発言データ一覧です。
    /// </summary>
    public class TalkWindowSceneData
    {
        public List<TalkEntryData> Entries;
    }
}
