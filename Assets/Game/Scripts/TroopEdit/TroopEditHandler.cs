using Frontier.CharacterEdit;
using Frontier.UI;
using System;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.TroopEdit
{
    /// <summary>
    /// 部隊編集画面の入力受付・選択状態・キャラクターデータのライフサイクルを担当するハンドラ。
    /// フィールドに限らず、複数のシーン・呼び出し元から共通して開かれる可能性があるため、
    /// 特定のシーンに紐づかない独立したクラスとしている(SaveLoadHandlerと同様)。
    /// 呼び出し元からShow()で開かれ、閉じた際はコールバックで呼び出し元へ通知する。
    /// 表示用のCharacterや選択インデックスといった実データはここが保持し、Presenterは
    /// Viewへの単純な転送のみを行う(将来のレベルアップ画面遷移等で、選択中のCharacterを
    /// Handlerが直接扱えるようにするため)。
    /// </summary>
    public class TroopEditHandler : MonoBehaviour
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private UserDomain _userDomain = null;
        [Inject] private GeneralHeaderPresenter _headerPresenter = null;

        private TroopEditPresenter _presenter = null;
        private CharacterParameterPresenter _paramPresenter = null;
        private TroopGridController _gridController = null;
        private CharacterEditHandler _characterEditHandler = null;
        private Action _onClosed = null;
        private int _navHashCode;

        /// <summary>
        /// Presenterを生成します(一度だけ呼び出してください)。
        /// </summary>
        public void Setup()
        {
            _presenter = _hierarchyBld.InstantiateWithDiContainer<TroopEditPresenter>( false );
            _presenter.Init();

            // isNeedCamera:false ... 選択中キャラクターの3Dモデル描画は行わない(枠外の一覧グリッド側で表示済みのため)
            _paramPresenter = _hierarchyBld.InstantiateWithDiContainer<CharacterParameterPresenter>(
                new object[] { _presenter.CharacterParamUI, false }, false );
            _paramPresenter.Init();

            _gridController = _hierarchyBld.InstantiateWithDiContainer<TroopGridController>( false );
            _gridController.Init( _presenter, _paramPresenter );
        }

        /// <summary>
        /// 部隊編集画面を開き、入力コードの受付を開始します。
        /// カーソルは先頭(左上)のキャラクターにリセットされます。
        /// </summary>
        /// <param name="onClosed">画面を閉じた際に呼ばれるコールバック(呼び出し元がメニューへ戻る処理を行う)</param>
        public void Show( Action onClosed )
        {
            _onClosed = onClosed;

            _headerPresenter.SetStateTitle( LocKey.UI_CMD_TROOPS );

            _gridController.Show( _userDomain.Members, CHARACTER_SELECTION_OFFSET_Z );

            RegisterNavInputCodes();
        }

        /// <summary>
        /// 画面内でのカーソル移動・キャンセルに対応する入力コードを登録します。
        /// </summary>
        private void RegisterNavInputCodes()
        {
            _navHashCode = Hash.GetStableHash( nameof( TroopEditHandler ) + "_Nav" );
            InputFacade.Instance.RegisterInputCodes(
                new InputCode( GuideIcon.ALL_CURSOR, "SELECT", InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptDirection ), GRID_DIRECTION_INPUT_INTERVAL, _navHashCode ) { RepeatDelay = DIRECTION_INPUT_REPEAT_DELAY },
                ( GuideIcon.CONFIRM,    "CONFIRM", InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptConfirm ),   0.0f, _navHashCode ),
                ( GuideIcon.CANCEL,     "BACK",    InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptCancel ),    0.0f, _navHashCode )
            );
        }

        private bool AcceptDirection( InputContext context )
        {
            return _gridController.MoveSelection( context.Cursor );
        }

        private bool AcceptConfirm( InputContext context )
        {
            if ( !context.GetButton( GameButton.Confirm ) ) return false;
            if ( _gridController.SpawnedCharacters.Count == 0 ) return false;

            OpenCharacterEditScreen();

            return true;
        }

        private void EnsureCharacterEditHandler()
        {
            if ( _characterEditHandler != null ) return;

            _characterEditHandler = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<CharacterEditHandler>( true, false, nameof( CharacterEditHandler ) );
            _characterEditHandler.Setup();
        }

        /// <summary>
        /// 選択中キャラクターの編集画面を開きます。グリッド表示は非表示にし、
        /// 復帰時にL1/R1で切り替わった可能性のある最終的な選択位置を反映します。
        /// </summary>
        private void OpenCharacterEditScreen()
        {
            EnsureCharacterEditHandler();

            _presenter.Hide();

            InputFacade.Instance.UnregisterInputCodes( _navHashCode );
            InputFacade.Instance.RegisterInputCodes();

            var context = new CharacterEditContext( _gridController.SpawnedCharacters, _gridController.SelectedIndex );
            _characterEditHandler.Show( context, OnCharacterEditClosed );
        }

        private void OnCharacterEditClosed( int finalIndex )
        {
            _gridController.SetSelectedIndex( finalIndex );

            _presenter.Show();

            RegisterNavInputCodes();
        }

        private bool AcceptCancel( InputContext context )
        {
            if ( !context.GetButton( GameButton.Cancel ) ) return false;

            Close();

            return true;
        }

        private void Close()
        {
            _gridController.Close();
            _headerPresenter.ClearStateTitle();

            InputFacade.Instance.UnregisterInputCodes( _navHashCode );

            // UnregisterInputCodesだけでは入力ガイド表示が更新されないため、空登録でガイドの再描画を促す
            InputFacade.Instance.RegisterInputCodes();

            var callback = _onClosed;
            _onClosed = null;
            callback?.Invoke();
        }
    }
}
