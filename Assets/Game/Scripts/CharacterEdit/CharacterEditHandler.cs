using Frontier.Entities;
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
    /// 編集対象のキャラクター一覧・現在のindexはCharacterEditContext(参照型)で管理する。
    /// レベルアップ画面(LevelUpHandler)を開いている間は、割り振り状態が特定の1体に紐づくため
    /// L1/R1によるキャラクター切り替えを一時的に無効化する(今後実装するSkillEquipHandlerは
    /// 対象キャラクターに依存しない想定であれば、切り替えを維持したまま実装してよい)。
    /// </summary>
    public class CharacterEditHandler : MonoBehaviour
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private UserDomain _userDomain = null;

        private CharacterEditPresenter _presenter = null;
        private CharacterParameterPresenter _paramPresenter = null;
        private LevelUpHandler _levelUpHandler = null;
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

            // isNeedCamera:false ... 上部のパラメータ表示では3Dモデルの描画は行わない(TroopEdit画面と同じ方針)
            _paramPresenter = _hierarchyBld.InstantiateWithDiContainer<CharacterParameterPresenter>(
                new object[] { _presenter.CharacterParamUI, false }, false );
            _paramPresenter.Init();
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
            _presenter.SetHeaderInfo( _userDomain.Money, _userDomain.Exp );
            RefreshCharacterParamDisplay();

            RegisterCharacterSwitchInputCodes();
            RegisterNavInputCodes();
        }

        private void RefreshCharacterParamDisplay()
        {
            _paramPresenter.AssignCharacter( _context.CurrentCharacter, LAYER_MASK_INDEX_CHARACTER );
            _paramPresenter.SetActive( true );
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
            RefreshCharacterParamDisplay();

            return true;
        }

        private bool AcceptNextCharacter( InputContext context )
        {
            if ( !context.GetButton( GameButton.Sub2 ) ) return false;

            _context.MoveNext();
            _presenter.RefreshCharacterInfo( _context.CurrentCharacter );
            RefreshCharacterParamDisplay();

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
        /// レベルアップ画面を開きます。メニューのナビゲーション入力に加え、L1/R1による
        /// キャラクター切り替えも一時的に無効化します(レベルアップ画面の仮の割り振り状態は
        /// 特定の1体に紐づくため、割り振り中に対象キャラクターが切り替わる事態を避けるため)。
        /// </summary>
        private void OpenLevelUpScreen()
        {
            EnsureLevelUpHandler();

            InputFacade.Instance.UnregisterInputCodes( _navHashCode );
            InputFacade.Instance.UnregisterInputCodes( _switchHashCode );

            _levelUpHandler.Show( _context.CurrentCharacter, OnLevelUpClosed );
        }

        private void EnsureLevelUpHandler()
        {
            if ( _levelUpHandler != null ) return;

            _levelUpHandler = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<LevelUpHandler>( true, false, nameof( LevelUpHandler ) );
            _levelUpHandler.Setup( _presenter.LevelUpView );
        }

        /// <summary>
        /// レベルアップ画面が閉じた際に呼ばれます。決定・キャンセルいずれの場合も、
        /// レベルやステータス・所持ポイントが変化した可能性があるため表示を更新した上で、
        /// メニューのナビゲーション入力・L1/R1切り替えを復帰させます。
        /// </summary>
        private void OnLevelUpClosed()
        {
            _presenter.RefreshCharacterInfo( _context.CurrentCharacter );
            _presenter.SetHeaderInfo( _userDomain.Money, _userDomain.Exp );
            RefreshCharacterParamDisplay();

            RegisterCharacterSwitchInputCodes();
            RegisterNavInputCodes();
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
            _paramPresenter.ClearCharacter();

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
