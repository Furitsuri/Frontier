using Frontier.Entities;
using Frontier.UI;
using System;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.CharacterEdit
{
    /// <summary>
    /// キャラクター編集画面の「装備スキル設定」項目から遷移する装備スキル設定画面の入力受付・
    /// ライフサイクルを担当するハンドラ。LevelUpHandler/StatusUpHandlerと同様、特定のシーンに
    /// 紐づかない独立したクラスとしている。割り振りの実データ(SkillEquipContext)はここが保持し、
    /// 左側ウィンドウでOKが確定された際にのみCharacter.Status.EquipSkills/UserDomain.SkillInventoryへ
    /// 反映する(それまでは全て仮の値であり、キャンセルすれば破棄される)。
    /// </summary>
    public class SkillEquipHandler : MonoBehaviour
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private UserDomain _userDomain = null;

        private SkillEquipPresenter _presenter = null;
        private SkillEquipContext _context = null;
        private Action _onClosed = null;
        private int _navHashCode;

        /// <summary>
        /// Presenterを生成します(一度だけ呼び出してください)。
        /// </summary>
        /// <param name="view">キャラクター編集画面のヒエラルキー内に配置済みのSkillEquipUIへの参照</param>
        public void Setup( SkillEquipUI view )
        {
            _presenter = _hierarchyBld.InstantiateWithDiContainer<SkillEquipPresenter>( new object[] { view }, false );
        }

        /// <summary>
        /// 装備スキル設定画面を開き、入力コードの受付を開始します。
        /// </summary>
        /// <param name="character">編集対象のキャラクター</param>
        /// <param name="onClosed">画面を閉じた際(決定・キャンセルいずれも)に呼ばれるコールバック</param>
        public void Show( Character character, Action onClosed )
        {
            _context = new SkillEquipContext( character, _userDomain );
            _onClosed = onClosed;

            _presenter.Show( _context );

            RegisterNavInputCodes();
        }

        /// <summary>
        /// 入力受付は開始せず、指定キャラクターの内容をプレビュー表示のみ行います
        /// (キャラクター編集画面のメニューでEQUIP SKILLS項目にカーソルが当たっている間の表示用)。
        /// 決定・キャンセル操作は受け付けないため、割り振り状態は変更されません。
        /// </summary>
        public void ShowPreview( Character character )
        {
            _presenter.ShowPreview( new SkillEquipContext( character, _userDomain ) );
        }

        /// <summary>
        /// ShowPreview()で表示したプレビューを非表示にします。
        /// </summary>
        public void HidePreview() => _presenter.Hide();

        private void RegisterNavInputCodes()
        {
            _navHashCode = Hash.GetStableHash( nameof( SkillEquipHandler ) + "_Nav" );
            InputFacade.Instance.RegisterInputCodes(
                ( GuideIcon.VERTICAL_CURSOR, "SELECT",  InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, _navHashCode ),
                ( GuideIcon.CONFIRM,         "CONFIRM", CanAcceptConfirm,              new AcceptContextInput( AcceptConfirm ),   0.0f, _navHashCode ),
                ( GuideIcon.CANCEL,          "BACK",    InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptCancel ),    0.0f, _navHashCode )
            );
        }

        /// <summary>
        /// CONFIRMの入力受付可否を判定します。左側ウィンドウでは常に受付可能ですが、右側ウィンドウでは
        /// 現在フォーカスしている項目(スキルを外す/所持スキル)が実際に選択可能な場合のみ受け付けます
        /// (在庫が無い、編集中の枠と同じスキル、外す対象の枠が元々未装備、等の場合は受け付けません)。
        /// </summary>
        private bool CanAcceptConfirm()
        {
            if ( _presenter.IsLeftPane ) return true;

            return _presenter.CanConfirmRightSelection();
        }

        /// <summary>
        /// 上下でカーソルを移動します(装備枠の入れ替えに左右の値調整は使わないため、上下のみ扱います)。
        /// </summary>
        private bool AcceptDirection( InputContext context )
        {
            switch ( context.Cursor )
            {
                case Direction.FORWARD:
                case Direction.BACK:
                    return _presenter.MoveSelection( context.Cursor );

                default:
                    return false;
            }
        }

        /// <summary>
        /// 左側ウィンドウでOKが選択されていれば確定して画面を閉じ、いずれかの枠が選択されていれば
        /// 右側ウィンドウへカーソルを遷移させます。右側ウィンドウでは選択中のスキルをその枠へ装備します。
        /// </summary>
        private bool AcceptConfirm( InputContext context )
        {
            if ( !context.GetButton( GameButton.Confirm ) ) return false;

            if ( _presenter.IsLeftPane )
            {
                if ( _presenter.IsOkSelected() )
                {
                    Commit();
                    Close();
                    return true;
                }

                return _presenter.EnterRightPane();
            }

            return _presenter.ConfirmRightSelection();
        }

        /// <summary>
        /// 仮の装備構成・所持数をCharacter.Status.EquipSkills/UserDomain.SkillInventoryへ反映します。
        /// </summary>
        private void Commit() => _context.Commit();

        /// <summary>
        /// 左側ウィンドウであれば画面全体を閉じ(仮の割り振りは全て破棄)、右側ウィンドウであれば
        /// 左側ウィンドウへ戻るだけに留めます。
        /// </summary>
        private bool AcceptCancel( InputContext context )
        {
            if ( !context.GetButton( GameButton.Cancel ) ) return false;

            if ( _presenter.IsLeftPane )
            {
                Close();
            }
            else
            {
                _presenter.ExitRightPane();
            }

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
