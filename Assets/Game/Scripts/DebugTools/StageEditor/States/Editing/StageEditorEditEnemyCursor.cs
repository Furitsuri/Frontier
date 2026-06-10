using System;
using UnityEngine;
using Zenject;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    /// <summary>
    /// 敵編集モードのデフォルトクラス。カーソル移動のみを行います。
    /// Sub1 で新規敵配置サブモードへ遷移します。
    /// カーソル下に配置済み敵がいる場合、Sub2 でその敵のパラメータ編集サブモードへ遷移します。
    /// Cancel でサブモードからカーソルモードへ戻ります。
    /// </summary>
    public class StageEditorEditEnemyCursor : StageEditorEditBase
    {
        private enum SubMode { Cursor, NewPlacement, EditExisting }

        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        private SubMode                       _subMode      = SubMode.Cursor;
        private StageEditorEditEnemyNew      _newEdit       = null;
        private StageEditorEditEnemyExisting _existingEdit  = null;

        private Action<EditActionContext> _placeEnemyCallback = null;
        private Action<EditActionContext> _editEnemyCallback  = null;

        // ──── 初期化 ──────────────────────────────────────────────────────

        /// <summary>新規配置・既存編集それぞれのコールバックを設定します。Init() より前に呼んでください。</summary>
        public void SetEnemyCallbacks( Action<EditActionContext> placeEnemyCb, Action<EditActionContext> editEnemyCb )
        {
            _placeEnemyCallback = placeEnemyCb;
            _editEnemyCallback  = editEnemyCb;
        }

        public override void Init( Action<EditActionContext> callback )
        {
            // 基底の Setup のみ実行（callback は使わず独自コールバックを使用）
            _context = new EditActionContext();
            _context.Setup();

            _subMode      = SubMode.Cursor;
            _newEdit      = _hierarchyBld.InstantiateWithDiContainer<StageEditorEditEnemyNew>( false );
            _existingEdit = _hierarchyBld.InstantiateWithDiContainer<StageEditorEditEnemyExisting>( false );

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

            // カーソル位置の敵有無をフレームごとに更新
            if ( _refParams != null ) _refParams.EnemyAtCursor = IsEnemyAtCursor();
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
            if ( _subMode == SubMode.Cursor )            return IsEnemyAtCursor();
            if ( _subMode == SubMode.NewPlacement )      return _newEdit?.CanAcceptConfirm() ?? false;
            if ( _subMode == SubMode.EditExisting )      return _existingEdit?.CanAcceptConfirm() ?? false;
            return false;
        }

        public override bool CanAcceptCancel()
        {
            return _subMode != SubMode.Cursor;
        }

        public override bool CanAcceptSub1()
        {
            if ( _subMode == SubMode.Cursor )            return true;
            if ( _subMode == SubMode.NewPlacement )      return _newEdit?.CanAcceptSub1() ?? false;
            if ( _subMode == SubMode.EditExisting )      return _existingEdit?.CanAcceptSub1() ?? false;
            return false;
        }

        public override bool CanAcceptSub2()
        {
            if ( _subMode == SubMode.Cursor )            return false;  // Cursor モードでは Confirm で EditExisting へ遷移
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
                if ( !IsEnemyAtCursor() )                        return false;
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

        /// <summary>カーソルモード: 無効（EditExisting 遷移は Confirm に変更）。その他: 次のパラメータへ。</summary>
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
            _newEdit.OnCompleted       = null;  // Confirm 後はモード維持。Cancel でカーソルモードへ戻る。
            _newEdit.Init( _placeEnemyCallback );
            SyncRefParamsFlags();
            RefreshInputCodes?.Invoke();
        }

        private void EnterEditExisting()
        {
            int gridIndex = _gridCursor.X() + _gridCursor.Y() * _refParams.Col;
            _refParams.TryLoadEnemyAtGridIndex?.Invoke( gridIndex );
            _refParams.EditingEnemyGridIndex = gridIndex;

            _subMode = SubMode.EditExisting;
            _existingEdit.RefreshInputCodes = RefreshInputCodes;
            _existingEdit.OnCompleted = () =>
            {
                // Confirm 後: キャラクターマップと GridIndexToEnemyListIndex を新位置に更新
                int newGridIndex = _gridCursor.X() + _gridCursor.Y() * _refParams.Col;
                _newEdit?.UpdatePlacedCharacterGridIndex( gridIndex, newGridIndex );
                if ( newGridIndex != gridIndex && _refParams.GridIndexToEnemyListIndex.TryGetValue( gridIndex, out int listIdx ) )
                {
                    _refParams.GridIndexToEnemyListIndex.Remove( gridIndex );
                    _refParams.GridIndexToEnemyListIndex[newGridIndex] = listIdx;
                }
                ReturnToCursor();
            };

            // 配置済みキャラクターをカーソルにバインド
            var boundChar = _newEdit?.GetPlacedCharacterAt( gridIndex );
            _existingEdit.SetBoundCharacter( boundChar );
            _existingEdit.Init( _editEnemyCallback );
            SyncRefParamsFlags();
            RefreshInputCodes?.Invoke();
        }

        private void ReturnToCursor()
        {
            // NewPlacement から戻る場合は配置済みキャラクターを保持したまま遷移
            if ( _subMode == SubMode.NewPlacement )      _newEdit?.ExitKeepPlaced();
            else if ( _subMode == SubMode.EditExisting ) _existingEdit?.Exit();
            _subMode = SubMode.Cursor;
            SyncRefParamsFlags();
            RefreshInputCodes?.Invoke();
        }

        /// <summary>_refParams の UI 表示フラグを現在のサブモードに合わせて更新します。</summary>
        private void SyncRefParamsFlags()
        {
            if ( _refParams == null ) return;
            _refParams.EnemySubModeActive = _subMode != SubMode.Cursor;
        }

        // ──── ユーティリティ ──────────────────────────────────────────────

        private bool IsEnemyAtCursor()
        {
            if ( _refParams == null || _gridCursor == null ) return false;
            int gridIndex = _gridCursor.X() + _gridCursor.Y() * _refParams.Col;
            return _refParams.GridIndexToEnemyListIndex.ContainsKey( gridIndex );
        }
    }
}

#endif // UNITY_EDITOR
