namespace Frontier.Field
{
    /// <summary>
    /// FieldMenuPresenter.ConfirmSelection() の結果。FieldMenuHandlerが後続の入力コード制御を
    /// 判断するために使用する。
    /// </summary>
    public enum FieldMenuConfirmResult
    {
        /// <summary>何も起きない。フィールドメニューを開いたまま操作を続ける。</summary>
        None = 0,

        /// <summary>OPTIONが選択された。OptionHandlerへ処理を譲るため、メニューを一時的に隠す。</summary>
        SuspendForOption,

        /// <summary>EXIT_GAMEが選択された。ゲーム終了確認ダイアログを表示する。</summary>
        RequestExitGameConfirm,

        /// <summary>SAVEが選択された。セーブ画面を表示する。</summary>
        RequestSaveScreen,

        /// <summary>STATUSが選択された。部隊編集画面を表示する。</summary>
        RequestTroopEditScreen,
    }
}
