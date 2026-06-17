using Frontier.Entities;
using System;
using Zenject;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    /// <summary>
    /// StageProp 編集モードのデフォルトクラス。カーソル移動のみを行います。
    /// Sub1 で新規プロップ配置サブモードへ遷移します。
    /// カーソル下に配置済みプロップがいる場合、Confirm でそのプロップのパラメータ編集サブモードへ遷移します。
    /// Cancel でサブモードからカーソルモードへ戻ります。
    /// </summary>
    public class StageEditorEditStagePropCursor : StageEditorEditBase
    {
        private enum SubMode { Cursor, NewPlacement, EditExisting }

        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        private SubMode                          _subMode      = SubMode.Cursor;
        private StageEditorEditStagePropNew      _newEdit      = null;
        private StageEditorEditStagePropExisting _existingEdit = null;

        private Action<EditActionContext> _placeStagePropCallback = null;
        private Action<EditActionContext> _editStagePropCallback  = null;

        // ──── 初期化 ──────────────────────────────────────────────────────

        /// <summary>新規配置・既存編集それぞれのコールバックを設定します。Init() より前に呼んでください。</summary>
        public void SetStagePropCallbacks( Action<EditActionContext> placeCb, Action<EditActionContext> editCb )
        {
            _placeStagePropCallback = placeCb;
            _editStagePropCallback  = editCb;
        }

        public override void Init( Action<EditActionContext> callback )
        {
            _context = new EditActionContext();
            _context.Setup();

            _subMode      = SubMode.Cursor;
            _newEdit      = _hierarchyBld.InstantiateWithDiContainer<StageEditorEditStagePropNew>( false );
            _existingEdit = _hierarchyBld.InstantiateWithDiContainer<StageEditorEditStagePropExisting>( false );

            SyncRefParamsFlags();
        }

        public override void Exit()
        {
            if ( _subMode == SubMode.NewPlacement )      _newEdit?.Exit();
            else if ( _subMode == SubMode.EditExisting ) _existingEdit?.Exit();
            _subMode = SubMode.Cursor;
            SyncRefParamsFlags();
        }

        public override void Update()
        {
            base.Update();
            if ( _subMode == SubMode.NewPlacement )      _newEdit?.Update();
            else if ( _subMode == SubMode.EditExisting ) _existingEdit?.Update();

            if ( _refParams != null ) _refParams.StagePropAtCursor = IsStagePropAtCursor();
        }

        // ──── ラベル ──────────────────────────────────────────────────────

        public override string GetSub12Label()
        {
            return _subMode switch
            {
                SubMode.Cursor       => "NEW\nPLACE",
                SubMode.NewPlacement => _newEdit?.GetSub12Label(),
                SubMode.EditExisting => _existingEdit?.GetSub12Label(),
                _                    => null,
            };
        }

        public override string GetSub34Label()
        {
            return _subMode switch
            {
                SubMode.Cursor       => null,
                SubMode.NewPlacement => _newEdit?.GetSub34Label(),
                SubMode.EditExisting => _existingEdit?.GetSub34Label(),
                _                    => null,
            };
        }

        // ──── 入力受付判定 ────────────────────────────────────────────────

        public override bool CanAcceptConfirm()
        {
            if ( _subMode == SubMode.Cursor )            return IsStagePropAtCursor();
            if ( _subMode == SubMode.NewPlacement )      return _newEdit?.CanAcceptConfirm() ?? false;
            if ( _subMode == SubMode.EditExisting )      return _existingEdit?.CanAcceptConfirm() ?? false;
            return false;
        }

        public override bool CanAcceptCancel() { return _subMode != SubMode.Cursor; }

        public override bool CanAcceptSub1()
        {
            if ( _subMode == SubMode.Cursor )            return true;
            if ( _subMode == SubMode.NewPlacement )      return _newEdit?.CanAcceptSub1() ?? false;
            if ( _subMode == SubMode.EditExisting )      return _existingEdit?.CanAcceptSub1() ?? false;
            return false;
        }

        public override bool CanAcceptSub2()
        {
            if ( _subMode == SubMode.Cursor )            return false;
            if ( _subMode == SubMode.NewPlacement )      return _newEdit?.CanAcceptSub2() ?? false;
            if ( _subMode == SubMode.EditExisting )      return _existingEdit?.CanAcceptSub2() ?? false;
            return false;
        }

        public override bool CanAcceptSub3()
        {
            if ( _subMode == SubMode.NewPlacement )      return _newEdit?.CanAcceptSub3() ?? false;
            if ( _subMode == SubMode.EditExisting )      return _existingEdit?.CanAcceptSub3() ?? false;
            return false;
        }

        public override bool CanAcceptSub4()
        {
            if ( _subMode == SubMode.NewPlacement )      return _newEdit?.CanAcceptSub4() ?? false;
            if ( _subMode == SubMode.EditExisting )      return _existingEdit?.CanAcceptSub4() ?? false;
            return false;
        }

        // ──── 入力処理 ────────────────────────────────────────────────────

        public override bool AcceptConfirm( InputContext context )
        {
            if ( _subMode == SubMode.Cursor )
            {
                if ( !context.GetButton( GameButton.Confirm ) ) return false;
                if ( !IsStagePropAtCursor() )                   return false;
                EnterEditExisting();
                return true;
            }
            if ( _subMode == SubMode.NewPlacement )      return _newEdit?.AcceptConfirm( context ) ?? false;
            if ( _subMode == SubMode.EditExisting )      return _existingEdit?.AcceptConfirm( context ) ?? false;
            return false;
        }

        public override bool AcceptCancel( InputContext context )
        {
            if ( !context.GetButton( GameButton.Cancel ) ) return false;
            ReturnToCursor();
            return true;
        }

        /// <summary>カーソルモード: 新規配置サブモードへ遷移。その他: 前のパラメータへ。</summary>
        public override bool AcceptSub1( InputContext context )
        {
            if ( _subMode == SubMode.Cursor )
            {
                if ( !context.GetButton( GameButton.Sub1 ) ) return false;
                EnterNewPlacement();
                return true;
            }
            if ( _subMode == SubMode.NewPlacement )      return _newEdit?.AcceptSub1( context ) ?? false;
            if ( _subMode == SubMode.EditExisting )      return _existingEdit?.AcceptSub1( context ) ?? false;
            return false;
        }

        public override bool AcceptSub2( InputContext context )
        {
            if ( _subMode == SubMode.NewPlacement )      return _newEdit?.AcceptSub2( context ) ?? false;
            if ( _subMode == SubMode.EditExisting )      return _existingEdit?.AcceptSub2( context ) ?? false;
            return false;
        }

        public override bool AcceptSub3( InputContext context )
        {
            if ( _subMode == SubMode.NewPlacement )      return _newEdit?.AcceptSub3( context ) ?? false;
            if ( _subMode == SubMode.EditExisting )      return _existingEdit?.AcceptSub3( context ) ?? false;
            return false;
        }

        public override bool AcceptSub4( InputContext context )
        {
            if ( _subMode == SubMode.NewPlacement )      return _newEdit?.AcceptSub4( context ) ?? false;
            if ( _subMode == SubMode.EditExisting )      return _existingEdit?.AcceptSub4( context ) ?? false;
            return false;
        }

        // ──── サブモード切り替え ──────────────────────────────────────────

        private void EnterNewPlacement()
        {
            _subMode = SubMode.NewPlacement;
            _newEdit.RefreshInputCodes = RefreshInputCodes;
            _newEdit.OnCompleted       = null;
            _newEdit.Init( _placeStagePropCallback );
            SyncRefParamsFlags();
            RefreshInputCodes?.Invoke();
        }

        private void EnterEditExisting()
        {
            int gridIndex = _gridCursorCtrl.GetGridCursorX() + _gridCursorCtrl.GetGridCursorY() * _refParams.Col;
            _refParams.TryLoadStagePropAtGridIndex?.Invoke( gridIndex );
            _refParams.EditingStagePropGridIndex = gridIndex;
            // 編集中は占有マップから除去してカーソル移動が size 分スキップされるのを防ぐ
            _refParams.UnregisterStagePropOccupied?.Invoke( gridIndex, _refParams.StagePropSize );

            _subMode = SubMode.EditExisting;
            _existingEdit.RefreshInputCodes = RefreshInputCodes;
            _existingEdit.OnCompleted = () =>
            {
                int newGridIndex = _gridCursorCtrl.GetGridCursorX() + _gridCursorCtrl.GetGridCursorY() * _refParams.Col;
                if ( newGridIndex != gridIndex )
                {
                    if ( _refParams.GridIndexToStageProp.TryGetValue( gridIndex, out var movedProp ) )
                    {
                        _refParams.GridIndexToStageProp.Remove( gridIndex );
                        _refParams.GridIndexToStageProp[newGridIndex] = movedProp;
                    }
                    if ( _refParams.GridIndexToStagePropListIndex.TryGetValue( gridIndex, out int listIdx ) )
                    {
                        _refParams.GridIndexToStagePropListIndex.Remove( gridIndex );
                        _refParams.GridIndexToStagePropListIndex[newGridIndex] = listIdx;
                    }
                }
                ReturnToCursor();
            };

            _refParams.GridIndexToStageProp.TryGetValue( gridIndex, out var boundProp );
            _existingEdit.SetBoundProp( boundProp );
            _existingEdit.Init( _editStagePropCallback );
            SyncRefParamsFlags();
            RefreshInputCodes?.Invoke();
        }

        private void ReturnToCursor()
        {
            if ( _subMode == SubMode.NewPlacement )      _newEdit?.ExitKeepPlaced();
            else if ( _subMode == SubMode.EditExisting ) _existingEdit?.Exit();
            _subMode = SubMode.Cursor;
            SyncRefParamsFlags();
            RefreshInputCodes?.Invoke();
        }

        private void SyncRefParamsFlags()
        {
            if ( _refParams == null ) return;
            _refParams.StagePropSubModeActive = _subMode != SubMode.Cursor;
        }

        private bool IsStagePropAtCursor()
        {
            if ( _refParams == null || _gridCursorCtrl == null ) return false;
            int gridIndex = _gridCursorCtrl.GetGridCursorX() + _gridCursorCtrl.GetGridCursorY() * _refParams.Col;
            return _refParams.GridIndexToStagePropListIndex.ContainsKey( gridIndex );
        }
    }
}

#endif // UNITY_EDITOR
