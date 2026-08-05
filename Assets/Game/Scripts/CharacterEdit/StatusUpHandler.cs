using Frontier.Entities;
using Frontier.UI;
using System;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.CharacterEdit
{
    /// <summary>
    /// キャラクター編集画面の「ステータス上昇」項目から遷移する画面の入力受付・
    /// ライフサイクルを担当するハンドラ。LevelUpHandlerと同様、特定のシーンに紐づかない
    /// 独立したクラスとしている。割り振りの実データ(StatusUpContext)はここが保持し、
    /// 決定(OK)操作が行われた際にのみCharacter.Statusへ反映する
    /// (それまでは全て仮の値であり、キャンセルすれば破棄される)。
    /// </summary>
    public class StatusUpHandler : MonoBehaviour
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        private StatusUpPresenter _presenter = null;
        private StatusUpContext _context = null;
        private Action _onClosed = null;
        private int _navHashCode;

        /// <summary>
        /// Presenterを生成します(一度だけ呼び出してください)。
        /// </summary>
        /// <param name="view">キャラクター編集画面のヒエラルキー内に配置済みのStatusUpUIへの参照</param>
        public void Setup( StatusUpUI view )
        {
            _presenter = _hierarchyBld.InstantiateWithDiContainer<StatusUpPresenter>( new object[] { view }, false );
        }

        /// <summary>
        /// ステータス上昇画面を開き、入力コードの受付を開始します。
        /// </summary>
        /// <param name="character">ステータス上昇対象のキャラクター</param>
        /// <param name="onClosed">画面を閉じた際(決定・キャンセルいずれも)に呼ばれるコールバック</param>
        public void Show( Character character, Action onClosed )
        {
            _context = new StatusUpContext( character );
            _onClosed = onClosed;

            _presenter.Show( _context );

            RegisterNavInputCodes();
        }

        /// <summary>
        /// 入力受付は開始せず、指定キャラクターの内容をプレビュー表示のみ行います
        /// (キャラクター編集画面のメニューでSTATUS UP項目にカーソルが当たっている間の表示用)。
        /// 決定・キャンセル操作は受け付けないため、割り振り状態は変更されません。
        /// </summary>
        public void ShowPreview( Character character )
        {
            _presenter.Show( new StatusUpContext( character ) );
        }

        /// <summary>
        /// ShowPreview()で表示したプレビューを非表示にします。
        /// </summary>
        public void HidePreview() => _presenter.Hide();

        private void RegisterNavInputCodes()
        {
            _navHashCode = Hash.GetStableHash( nameof( StatusUpHandler ) + "_Nav" );
            InputFacade.Instance.RegisterInputCodes(
                new InputCode( GuideIcon.ALL_CURSOR, "SELECT", InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, _navHashCode ) { RepeatDelay = DIRECTION_INPUT_REPEAT_DELAY },
                ( GuideIcon.CONFIRM, "OK",   InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptConfirm ), 0.0f, _navHashCode ),
                ( GuideIcon.CANCEL,  "BACK", InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptCancel ),  0.0f, _navHashCode )
            );
        }

        /// <summary>
        /// 上下でメニューカーソル(MaxHP/Atk/Def/MoveRange/JumpForce/MaxActionGauge/OK)を移動し、
        /// 左右で選択中の能力値の仮割り振りを増減します(OK選択中は左右とも無効)。
        /// </summary>
        private bool AcceptDirection( InputContext context )
        {
            switch ( context.Cursor )
            {
                case Direction.FORWARD:
                case Direction.BACK:
                    return _presenter.MoveSelection( context.Cursor );

                case Direction.RIGHT:
                    return _presenter.Increase();

                case Direction.LEFT:
                    return _presenter.Decrease();

                default:
                    return false;
            }
        }

        private bool AcceptConfirm( InputContext context )
        {
            if ( !context.GetButton( GameButton.Confirm ) ) return false;
            if ( !_presenter.IsOkSelected() ) return false;

            Commit();
            Close();

            return true;
        }

        /// <summary>
        /// 仮の割り振り結果をCharacter.Statusへ反映します。
        /// MaxHPを上げた分だけCurHPも同時に回復させます(不足分をそのまま維持する形)。
        /// </summary>
        private void Commit()
        {
            ref var status = ref _context.Character.GetStatusRef;

            int addMaxHp = _context.GetAllocated( StatusUpContext.StatKind.MaxHP );
            status.MaxHP          += addMaxHp;
            status.CurHP          += addMaxHp;
            status.Atk            += _context.GetAllocated( StatusUpContext.StatKind.Atk );
            status.Def            += _context.GetAllocated( StatusUpContext.StatKind.Def );
            status.moveRange           += _context.GetAllocated( StatusUpContext.StatKind.MoveRange );
            status.jumpForce           += _context.GetAllocated( StatusUpContext.StatKind.JumpForce );
            status.maxActionGauge      += _context.GetAllocated( StatusUpContext.StatKind.MaxActionGauge );
            status.recoveryActionGauge += _context.GetAllocated( StatusUpContext.StatKind.RecoveryActionGauge );

            status.AddStatusPoint( _context.TentativeStatusPoint - _context.OriginalStatusPoint );
        }

        private bool AcceptCancel( InputContext context )
        {
            if ( !context.GetButton( GameButton.Cancel ) ) return false;

            Close();

            return true;
        }

        /// <summary>
        /// 画面を閉じます(決定・キャンセルいずれも、仮の割り振り状態を破棄してこのメソッドへ合流します。
        /// 決定の場合はCommit()が先に実データへ反映済みです)。
        /// </summary>
        private void Close()
        {
            _presenter.Hide();
            InputFacade.Instance.UnregisterInputCodes( _navHashCode );

            // UnregisterInputCodesだけでは入力ガイド表示が更新されないため、空登録でガイドの再描画を促す
            InputFacade.Instance.RegisterInputCodes();

            var callback = _onClosed;
            _onClosed = null;
            _context = null;

            callback?.Invoke();
        }
    }
}
