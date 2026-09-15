namespace Frontier.StateMachine
{
    /// <summary>
    /// 会話ウィンドウ形式の二択確認画面(ConfirmPhaseStateBase)における選択操作の方式。
    /// </summary>
    public enum ConfirmUIType
    {
        /// <summary>左右の十字キーでYes/Noを選択する、既存の方式</summary>
        HorizontalCursor = 0,

        /// <summary>SUB1でYes、SUB2でNoを直接選択する方式。十字キーを他の操作
        /// (キャラクター選択等)に使いたい画面向け。選択肢の横にSUB1/SUB2のガイドアイコンを表示する</summary>
        SubButtons,
    }
}
