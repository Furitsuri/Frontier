namespace Frontier.CharacterEdit
{
    /// <summary>
    /// CharacterEditPresenter.ConfirmSelection() の結果。CharacterEditHandlerが後続の
    /// 画面遷移を判断するために使用する。
    /// </summary>
    public enum CharacterEditConfirmResult
    {
        /// <summary>何も起きない。</summary>
        None = 0,

        /// <summary>レベルアップが選択された。レベルアップ画面を表示する。</summary>
        RequestLevelUpScreen,

        /// <summary>ステータス上昇が選択された。ステータス上昇画面を表示する。</summary>
        RequestStatusUpScreen,

        /// <summary>装備スキル設定が選択された。装備スキル設定画面を表示する。</summary>
        RequestSkillEquipScreen,
    }
}
