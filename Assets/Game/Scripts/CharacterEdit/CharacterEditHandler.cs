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
        private StatusUpHandler _statusUpHandler = null;
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

            // isNeedCamera:true ... 上部のパラメータ表示の左側にキャラクター3Dモデルを表示する
            // (TroopEditのグリッド一覧側で既に個々のポートレートを表示しているため、
            // TroopEdit側のパラメータパネルではisNeedCamera:falseのまま据え置く)
            _paramPresenter = _hierarchyBld.InstantiateWithDiContainer<CharacterParameterPresenter>(
                new object[] { _presenter.CharacterParamUI, true }, false );
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
            UpdatePreview();

            RegisterCharacterSwitchInputCodes();
            RegisterNavInputCodes();
        }

        /// <summary>
        /// 現在カーソルが当たっているメニュー項目に応じて、右側のプレビュー表示を切り替えます
        /// (LEVEL UPならレベルアップ画面、STATUS UPならステータス上昇画面、それ以外(装備スキル設定は
        /// 未実装)は何も表示しない)。まだ確定操作は行っていない、カーソル移動だけの状態で呼ばれる想定。
        /// </summary>
        private void UpdatePreview()
        {
            switch ( _presenter.SelectedOption )
            {
                case CHARACTER_EDIT_MENU_OPTION_TAG.LEVEL_UP:
                    EnsureLevelUpHandler();
                    _statusUpHandler?.HidePreview();
                    _levelUpHandler.ShowPreview( _context.CurrentCharacter );
                    break;

                case CHARACTER_EDIT_MENU_OPTION_TAG.STATUS_UP:
                    EnsureStatusUpHandler();
                    _levelUpHandler?.HidePreview();
                    _statusUpHandler.ShowPreview( _context.CurrentCharacter );
                    break;

                default:
                    _levelUpHandler?.HidePreview();
                    _statusUpHandler?.HidePreview();
                    break;
            }
        }

        private void RefreshCharacterParamDisplay()
        {
            _paramPresenter.AssignCharacter( _context.CurrentCharacter, LAYER_MASK_INDEX_CHARACTER );
            _paramPresenter.SetActive( true );
        }

        /// <summary>
        /// L1/R1でのキャラクター切替時、3DモデルをfromCharacterから現在のキャラクターへスライドさせながら表示します
        /// (TroopEdit/CharacterEditのキャラクターはいずれも常駐しており、切替前後で見た目が同一だと
        /// 切り替わったこと自体が分かりづらいための演出)。
        /// </summary>
        private void SlideCharacterParamDisplay( Character fromCharacter, SlideDirection direction )
        {
            _paramPresenter.AssignCharacterWithSlide( fromCharacter, _context.CurrentCharacter, LAYER_MASK_INDEX_CHARACTER, direction );
            _paramPresenter.SetActive( true );
        }

        // isNeedCamera:trueで生成したCharacterParameterPresenterは、カメラのRenderを
        // 毎フレーム能動的に呼び出さない限りポートレートが更新されない(BattleRoutinePresenter/
        // DeploymentPhasePresenter等、他の呼び出し元も同様に自身のUpdate内で呼び出している)。
        private void Update()
        {
            _paramPresenter?.Update();
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

            var fromCharacter = _context.CurrentCharacter;
            _context.MovePrevious();
            _presenter.RefreshCharacterInfo( _context.CurrentCharacter );
            SlideCharacterParamDisplay( fromCharacter, SlideDirection.RIGHT );
            UpdatePreview();

            return true;
        }

        private bool AcceptNextCharacter( InputContext context )
        {
            if ( !context.GetButton( GameButton.Sub2 ) ) return false;

            var fromCharacter = _context.CurrentCharacter;
            _context.MoveNext();
            _presenter.RefreshCharacterInfo( _context.CurrentCharacter );
            SlideCharacterParamDisplay( fromCharacter, SlideDirection.LEFT );
            UpdatePreview();

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
            if ( !_presenter.MoveSelection( context.Cursor ) ) return false;

            UpdatePreview();

            return true;
        }

        private bool AcceptConfirm( InputContext context )
        {
            if ( !context.GetButton( GameButton.Confirm ) ) return false;

            switch ( _presenter.ConfirmSelection() )
            {
                case CharacterEditConfirmResult.RequestLevelUpScreen:
                    OpenLevelUpScreen();
                    break;

                case CharacterEditConfirmResult.RequestStatusUpScreen:
                    OpenStatusUpScreen();
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
            _presenter.LockMenu();

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
            _presenter.UnlockMenu();
            UpdatePreview();

            RegisterCharacterSwitchInputCodes();
            RegisterNavInputCodes();
        }

        /// <summary>
        /// ステータス上昇画面を開きます。メニューのナビゲーション入力に加え、L1/R1による
        /// キャラクター切り替えも一時的に無効化します(仮の割り振り状態が特定の1体に紐づくため、
        /// 割り振り中に対象キャラクターが切り替わる事態を避けるため)。
        /// </summary>
        private void OpenStatusUpScreen()
        {
            EnsureStatusUpHandler();

            InputFacade.Instance.UnregisterInputCodes( _navHashCode );
            InputFacade.Instance.UnregisterInputCodes( _switchHashCode );
            _presenter.LockMenu();

            _statusUpHandler.Show( _context.CurrentCharacter, OnStatusUpClosed );
        }

        private void EnsureStatusUpHandler()
        {
            if ( _statusUpHandler != null ) return;

            _statusUpHandler = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<StatusUpHandler>( true, false, nameof( StatusUpHandler ) );
            _statusUpHandler.Setup( _presenter.StatusUpView );
        }

        /// <summary>
        /// ステータス上昇画面が閉じた際に呼ばれます。決定・キャンセルいずれの場合も、
        /// ステータス・所持StatusPointが変化した可能性があるため表示を更新した上で、
        /// メニューのナビゲーション入力・L1/R1切り替えを復帰させます。
        /// </summary>
        private void OnStatusUpClosed()
        {
            _presenter.RefreshCharacterInfo( _context.CurrentCharacter );
            RefreshCharacterParamDisplay();
            _presenter.UnlockMenu();
            UpdatePreview();

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
            _levelUpHandler?.HidePreview();
            _statusUpHandler?.HidePreview();

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
