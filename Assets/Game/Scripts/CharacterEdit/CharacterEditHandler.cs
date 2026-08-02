using System;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.CharacterEdit
{
    /// <summary>
    /// 1体のキャラクターに対する編集画面(レベルアップ/装備スキル設定)の入力受付・
    /// ライフサイクルを担当するハンドラ。SaveLoadHandler/TroopEditHandlerと同様、
    /// 特定のシーンに紐づかない独立したクラスとしている(TroopEdit以外からも呼び出せる)。
    /// 編集対象のキャラクター一覧・現在のindexはCharacterEditContext(参照型)で管理し、
    /// 今後実装するLevelUpHandler/SkillEquipHandlerにも同じ参照をそのまま渡すことで、
    /// どの階層でL1/R1によりキャラクターが切り替わっても状態を共有できるようにする。
    /// </summary>
    public class CharacterEditHandler : MonoBehaviour
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        private CharacterEditPresenter _presenter = null;
        private CharacterEditContext _context = null;
        private Action<int> _onClosed = null;
        private int _navHashCode;
        private int _switchHashCode;

        /// <summary>
        /// Presenterを生成します(一度だけ呼び出してください)。
        /// </summary>
        public void Setup()
        {
            _presenter = _hierarchyBld.InstantiateWithDiContainer<CharacterEditPresenter>( false );
            _presenter.Init();
        }

        /// <summary>
        /// 編集画面を開き、入力コードの受付を開始します。
        /// </summary>
        /// <param name="context">編集対象のキャラクター一覧・現在のindex(参照型。呼び出し元と共有される)</param>
        /// <param name="onClosed">画面を閉じた際に呼ばれるコールバック。L1/R1で切り替えた最終的なindexを渡す</param>
        public void Show( CharacterEditContext context, Action<int> onClosed )
        {
            _context = context;
            _onClosed = onClosed;

            _presenter.Show( _context.CurrentCharacter );

            RegisterCharacterSwitchInputCodes();
            RegisterNavInputCodes();
        }

        /// <summary>
        /// L1/R1によるキャラクター切り替えは、配下のレベルアップ/装備スキル設定画面が
        /// 開いている間も有効であるべきなので、メニューのナビゲーション入力とは別に登録し、
        /// この画面全体を閉じるまで解除しない。
        /// </summary>
        private void RegisterCharacterSwitchInputCodes()
        {
            _switchHashCode = Hash.GetStableHash( nameof( CharacterEditHandler ) + "_Switch" );
            InputFacade.Instance.RegisterInputCodes(
                ( GuideIcon.SUB1, "PREV\nCHARA", InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptPreviousCharacter ), 0.0f, _switchHashCode ),
                ( GuideIcon.SUB2, "NEXT\nCHARA", InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptNextCharacter ),     0.0f, _switchHashCode )
            );
        }

        private bool AcceptPreviousCharacter( InputContext context )
        {
            if ( !context.GetButton( GameButton.Sub1 ) ) return false;

            _context.MovePrevious();
            _presenter.RefreshCharacterInfo( _context.CurrentCharacter );

            return true;
        }

        private bool AcceptNextCharacter( InputContext context )
        {
            if ( !context.GetButton( GameButton.Sub2 ) ) return false;

            _context.MoveNext();
            _presenter.RefreshCharacterInfo( _context.CurrentCharacter );

            return true;
        }

        /// <summary>
        /// メニュー(レベルアップ/装備スキル設定)のカーソル移動・決定・キャンセルに対応する
        /// 入力コードを登録します。サブ画面(レベルアップ等)を開いている間は一時的に解除される想定。
        /// </summary>
        private void RegisterNavInputCodes()
        {
            _navHashCode = Hash.GetStableHash( nameof( CharacterEditHandler ) + "_Nav" );
            InputFacade.Instance.RegisterInputCodes(
                ( GuideIcon.VERTICAL_CURSOR, "SELECT",  InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, _navHashCode ),
                ( GuideIcon.CONFIRM,         "CONFIRM", InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptConfirm ),   0.0f, _navHashCode ),
                ( GuideIcon.CANCEL,          "BACK",    InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptCancel ),    0.0f, _navHashCode )
            );
        }

        private bool AcceptDirection( InputContext context )
        {
            return _presenter.MoveSelection( context.Cursor );
        }

        private bool AcceptConfirm( InputContext context )
        {
            if ( !context.GetButton( GameButton.Confirm ) ) return false;

            switch ( _presenter.ConfirmSelection() )
            {
                case CharacterEditConfirmResult.RequestLevelUpScreen:
                    OpenLevelUpScreen();
                    break;

                case CharacterEditConfirmResult.RequestSkillEquipScreen:
                    OpenSkillEquipScreen();
                    break;
            }

            return true;
        }

        /// <summary>
        /// TODO: LevelUpHandlerを実装したら、ここでナビゲーション入力(_navHashCode)を一時解除して
        /// メニューのハイライトはそのまま維持しつつ、LevelUpHandler.Show( _context, onClosed: メニューへ入力復帰 ) を呼ぶ。
        /// </summary>
        private void OpenLevelUpScreen()
        {
            Debug.Log( "[CharacterEditHandler] レベルアップ画面は未実装です。" );
        }

        /// <summary>
        /// TODO: SkillEquipHandlerを実装したら、ここでナビゲーション入力(_navHashCode)を一時解除して
        /// メニューのハイライトはそのまま維持しつつ、SkillEquipHandler.Show( _context, onClosed: メニューへ入力復帰 ) を呼ぶ。
        /// </summary>
        private void OpenSkillEquipScreen()
        {
            Debug.Log( "[CharacterEditHandler] 装備スキル設定画面は未実装です。" );
        }

        private bool AcceptCancel( InputContext context )
        {
            if ( !context.GetButton( GameButton.Cancel ) ) return false;

            Close();

            return true;
        }

        private void Close()
        {
            _presenter.Hide();

            InputFacade.Instance.UnregisterInputCodes( _navHashCode );
            InputFacade.Instance.UnregisterInputCodes( _switchHashCode );

            // UnregisterInputCodesだけでは入力ガイド表示が更新されないため、空登録でガイドの再描画を促す
            InputFacade.Instance.RegisterInputCodes();

            var callback = _onClosed;
            int finalIndex = _context.CurrentIndex;
            _onClosed = null;
            _context = null;

            callback?.Invoke( finalIndex );
        }
    }
}
