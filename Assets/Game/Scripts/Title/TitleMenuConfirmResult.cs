namespace Frontier.Title
{
    /// <summary>
    /// TitleMenuPresenter.ConfirmSelection() の結果。TitleMenuHandlerが後続の入力コード制御を
    /// 判断するために使用する。
    /// </summary>
    public enum TitleMenuConfirmResult
    {
        /// <summary>何も起きない。</summary>
        None = 0,

        /// <summary>NEW_GAMEが選択された。</summary>
        RequestNewGame,

        /// <summary>LOAD_GAMEが選択された。</summary>
        RequestLoadGame,

        /// <summary>OPTIONが選択された。OptionHandlerへ処理を譲るため、メニューを一時的に隠す。</summary>
        SuspendForOption,

        /// <summary>EXIT_GAMEが選択された。ゲーム終了確認ダイアログを表示する。</summary>
        RequestExitGameConfirm,
    }
}
