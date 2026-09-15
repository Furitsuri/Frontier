using Frontier.StateMachine;
using Zenject;

namespace Frontier.UI
{
    /// <summary>
    /// 会話ウィンドウ形式(TalkWindowConfirmUI)のYes/No確認ダイアログを扱うPresenter。
    /// IConfirmPresenterを実装しているため、ConfirmPhaseStateBase派生StateのAssignPresenter()で
    /// このインスタンスを割り当てれば、既存のConfirmUIベースの確認画面と同じ手順(SetActiveConfirmUI/
    /// ApplyColor2Options)のまま見た目だけを会話ウィンドウ形式に置き換えられる。
    /// ConfirmUIType.SubButtons指定時は、十字キーの代わりにSUB1/SUB2でYes/Noを選択する形式になり、
    /// 選択肢の横にInputFacade経由で取得した入力ガイドと同じSUB1/SUB2アイコンを表示する。
    /// </summary>
    public class TalkWindowConfirmPresenter : IConfirmPresenter
    {
        [Inject] private IUiSystem _uiSystem = null;
        [Inject] private ILocalizationService _localization = null;

        private LocKey _speakerKey = LocKey.None;
        private LocKey _messageKey = LocKey.None;

        private TalkWindowConfirmUI View => _uiSystem.GeneralUi.TalkWindowView as TalkWindowConfirmUI;

        /// <summary>
        /// 会話ウィンドウに表示する話者名・メッセージを設定します。
        /// </summary>
        public void SetConfirmMessage( LocKey speakerKey, LocKey messageKey )
        {
            _speakerKey = speakerKey;
            _messageKey = messageKey;

            RefreshMessage();
        }

        public void SetActiveConfirmUI( bool isActive, ConfirmUIType uiType )
        {
            if ( !isActive )
            {
                // 通常の会話(挨拶等)で同じウィンドウを使い回した際に選択肢が残らないよう、
                // 非表示にする際は必ず選択肢自体もOFFにしておく
                View.SetOptionsActive( false );
                View.SetOptionIconsVisible( false );
                View.Hide();
                return;
            }

            // メッセージの反映(Show()によるgameObjectの表示化を含む)は、この直後に呼ばれる
            // SetConfirmMessage()に委ねる。ここで先にLocKey.None(既定値)のまま表示しようとすると
            // 未設定のキーでローカライズ解決を試みてしまうため、位置・選択肢の準備のみ行う
            // 画面右下(入力ガイドバー直上)に配置する。呼び出し元(RecruitGridStateBase)が
            // グリッド絞り込み・パラメータパネル移動のアニメーション完了後にこのUIを表示するため、
            // ここでは瞬時に表示してよい
            View.SetPositionBottomRight();
            View.SetOptionTexts( _localization.Get( LocKey.UI_CONFIRM_YES ), _localization.Get( LocKey.UI_CONFIRM_NO ) );
            View.SetOptionsActive( true );

            bool showIcons = uiType == ConfirmUIType.SubButtons;
            View.SetOptionIconsVisible( showIcons );
            if ( showIcons )
            {
                View.SetOptionIcons(
                    InputFacade.Instance.GetGuideIconSprite( GuideIcon.SUB1 ),
                    InputFacade.Instance.GetGuideIconSprite( GuideIcon.SUB2 ) );
            }
        }

        public void ApplyColor2Options( int selectIndex ) => View.ApplyOptionColor( selectIndex );

        /// <summary>
        /// SetActiveConfirmUI(false, ...)で隠した会話ウィンドウを、直前にSetConfirmMessage()で
        /// 設定した内容のまま再表示します。ステータス確認画面等の子Stateから確認画面へ戻る際
        /// (RestartState)に使用します。
        /// </summary>
        public void RedisplayConfirmMessage() => RefreshMessage();

        private void RefreshMessage()
        {
            View.Show( _localization.Get( _speakerKey ), _localization.Get( _messageKey ), null );
        }
    }
}
