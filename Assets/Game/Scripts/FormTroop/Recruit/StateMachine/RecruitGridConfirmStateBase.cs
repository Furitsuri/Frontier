using Frontier.StateMachine;
using Frontier.TroopEdit;
using Frontier.UI;
using System;
using static Constants;

namespace Frontier.FormTroop
{
    /// <summary>
    /// 雇用/解雇の完了確認画面(RecruitEmployState/RecruitDismissStateの子)の共通基底クラス。
    /// 会話ウィンドウ形式のYes/No確認(ConfirmUIType.SubButtonsによりSUB1/SUB2で選択)に加え、
    /// 絞り込み後のキャラクター一覧を十字キーで選択でき、INFO入力でステータス確認画面へ遷移できる。
    /// 十字キーをYes/No選択ではなくキャラクター選択に使うため、Yes/No選択にはSUB1/SUB2を用いる
    /// ConfirmUIType.SubButtonsを固定で使用する。キャラクター選択の実体(TroopGridController)は
    /// 親State(RecruitGridStateBase)が保持するものをそのまま使い回すため、複製・再構築は行わない。
    /// </summary>
    public abstract class RecruitGridConfirmStateBase : ConfirmPhaseStateBase
    {
        /// <summary>
        /// このクラスを使う画面に共通する子Stateの並び。RecruitPhaseHandler.CreateTree()での
        /// AddChild()呼び出し順と一致させること。
        /// </summary>
        private enum ConfirmChildTag
        {
            CHARACTER_STATUS = 0,
        }

        protected TroopGridController _gridController = null;
        protected int ToggledCount { get; private set; }

        private Action _commitCallback = null;

        protected override ConfirmUIType UIType => ConfirmUIType.SubButtons;

        public override void Init( object context )
        {
            base.Init( context );

            RecruitGridConfirmContext gridConfirmContext = null;
            ReceiveContext( ref gridConfirmContext, context );
            NullCheck.AssertNotNull( gridConfirmContext, nameof( gridConfirmContext ) );

            _gridController = gridConfirmContext.GridController;
            _commitCallback = gridConfirmContext.CommitCallback;
            ToggledCount     = gridConfirmContext.ToggledCount;

            NullCheck.AssertNotNull( _gridController, nameof( _gridController ) );
        }

        /// <summary>
        /// Yesが選択された際、親State(RecruitEmployState/RecruitDismissState)の確定処理
        /// (CommitEmployment/CommitDismissal)をコールバック経由で呼びます。
        /// </summary>
        protected void CommitConfirmedSelection()
        {
            _commitCallback?.Invoke();
        }

        /// <summary>
        /// ステータス確認画面等の子Stateへ遷移する際に呼ばれます。子State表示中は
        /// 会話ウィンドウ(店主のメッセージ・Yes/No選択肢)が重なって表示され続けてしまうため、隠します。
        /// </summary>
        public override object PauseState()
        {
            _confirmPresenter.SetActiveConfirmUI( false, UIType );

            return base.PauseState();
        }

        /// <summary>
        /// 子Stateから確認画面へ戻った際に呼ばれます。PauseState()で隠した会話ウィンドウを、
        /// 直前の内容のまま再表示します。
        /// </summary>
        public override void RestartState()
        {
            base.RestartState();

            _confirmPresenter.SetActiveConfirmUI( true, UIType );
            ( _confirmPresenter as TalkWindowConfirmPresenter )?.RedisplayConfirmMessage();
        }

        /// <summary>
        /// 入力コードを登録します。Yes/No選択(SUB1/SUB2)は基底クラスに委ね、
        /// ここではキャラクター選択(十字キー)とステータス確認(INFO)を追加登録します。
        /// </summary>
        public override void RegisterInputCodes()
        {
            base.RegisterInputCodes();

            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (GuideIcon.ALL_CURSOR, "SELECT\nUNIT", CanAcceptDefault, new AcceptContextInput( AcceptDirection ), GRID_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.INFO,       "STATUS",       CanAcceptInfo,    new AcceptContextInput( AcceptInfo ),      0.0f, hashCode)
            );
        }

        /// <summary>
        /// 十字キーで、絞り込み後のキャラクター一覧のカーソルを移動します
        /// (Yes/No選択はSUB1/SUB2で行うため、ここでは競合しません)。
        /// </summary>
        protected override bool AcceptDirection( InputContext context )
        {
            return _gridController.MoveSelection( context.Cursor );
        }

        /// <summary>
        /// 選択中キャラクターが存在しない場合はステータス表示への遷移を受け付けない。
        /// </summary>
        protected override bool CanAcceptInfo()
        {
            return _gridController.SelectedCharacter != null;
        }

        protected override bool AcceptInfo( InputContext context )
        {
            if( !base.AcceptInfo( context ) ) { return false; }

            SetSendTransitionContext( _gridController.SelectedCharacter );
            TransitState( ( int ) ConfirmChildTag.CHARACTER_STATUS );

            return true;
        }
    }
}
