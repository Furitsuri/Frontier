namespace Frontier.SaveLoad
{
    /// <summary>
    /// セーブ/ロード画面(SaveLoadUI)の表示モード。
    /// 画面上部のタイトル表示と、確定操作(Confirm)時の挙動のみが異なり、レイアウトは完全に共有する。
    /// </summary>
    public enum SaveLoadMode
    {
        Save,
        Load,
    }
}
