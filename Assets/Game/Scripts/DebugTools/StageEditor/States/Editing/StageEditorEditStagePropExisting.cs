using Frontier.Entities;
using Frontier.Registries;
using Frontier.Stage;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    /// <summary>
    /// 配置済み StageProp のパラメータを編集するクラス。
    /// Sub1/Sub2 で選択パラメータを前後に移動し、Sub3/Sub4 で値を増減します。
    /// Confirm で編集内容をコールバックに渡して反映し、カーソルモードへ戻ります。
    /// Cancel で編集前の状態に完全復元してカーソルモードへ戻ります。
    /// ExtraIntValues[0]=旧グリッドインデックス、[1]=新グリッドインデックス。
    /// </summary>
    public class StageEditorEditStagePropExisting : StageEditorEditBase
    {
        private struct ParamDescriptor
        {
            public string      Name;
            public int         Min;
            public int         Max;
            public Func<int>   Getter;
            public Action<int> Setter;
        }

        [Inject] private IStageDataProvider    _stageDataProvider = null;
        [Inject] private HierarchyBuilderBase  _hierarchyBld      = null;
        [Inject] private PrefabRegistry        _prefabReg         = null;

        private List<ParamDescriptor> _params      = new List<ParamDescriptor>();
        private StageProp             _boundProp   = null;
        private bool                  _confirmed   = false;
        private int                   _builtPrefab = -1;   // 現在ビジュアルとして生成しているプレハブのインデックス

        // Cancel 時に復元するためのスナップショット
        private Vector3 _snapPropPosition;
        private int     _snapCursorTileIndex;
        private int     _snapPrefab;
        private int     _snapSize;
        private int     _snapTileIndex;
        private int     _snapDirection;

        public override string GetSub12Label() => "PREV/NEXT\nPARAM";
        public override string GetSub34Label() => "DEC/INC\nVALUE";

        /// <summary>カーソルと連動させるプロップを設定します。Init() より前に呼んでください。</summary>
        public void SetBoundProp( StageProp prop ) { _boundProp = prop; }

        public override void Init( Action<EditActionContext> callback )
        {
            base.Init( callback );
            BuildParamDescriptors();
            _refParams.SelectedStagePropParamIndex = 0;
            _confirmed   = false;
            _builtPrefab = _refParams.StagePropPrefab;   // 編集開始時のビジュアルはロード済みデータのプレハブ

            _refParams.SetGridCursorSize?.Invoke( _refParams.StagePropSize );

            // 編集前の状態をスナップショット
            _snapPrefab          = _refParams.StagePropPrefab;
            _snapSize            = _refParams.StagePropSize;
            _snapTileIndex       = _refParams.StagePropTileIndex;
            _snapDirection       = _refParams.StagePropDirection;
            _snapCursorTileIndex = _gridCursorCtrl != null ? _gridCursorCtrl.GetCurrentGridIndex() : 0;
            _snapPropPosition    = _boundProp != null ? _boundProp.transform.position : Vector3.zero;
        }

        public override void Exit()
        {
            if ( !_confirmed )
            {
                // Cancel による退出: パラメータとプロップ位置を編集前に戻す
                _refParams.StagePropPrefab    = _snapPrefab;
                _refParams.StagePropSize      = _snapSize;
                _refParams.StagePropTileIndex = _snapTileIndex;
                _refParams.StagePropDirection = _snapDirection;

                _refParams.SetGridCursorSize?.Invoke( _snapSize );

                // プレハブを変更していた場合はビジュアルも編集前のプレハブへ戻す
                RebuildBoundProp();

                if ( _boundProp != null )
                {
                    _boundProp.SetSize( _snapSize );
                    _boundProp.SetPosition( _snapPropPosition );
                    _boundProp.SetRotation( ( Direction ) _snapDirection );
                }

                // 編集開始時に除去した占有情報を元の位置・サイズで復元する
                _refParams.RestoreStagePropOccupied?.Invoke( _snapTileIndex, _snapSize );

                if ( _gridCursorCtrl != null )
                {
                    _gridCursorCtrl.SetGridCursorTileIndex( _snapCursorTileIndex );
                    _gridCursorCtrl.SyncGridCursorPosition();
                }
            }
            _boundProp = null;
        }

        public override void Update()
        {
            base.Update();

            // カーソル位置から TileIndex をリアルタイム更新
            if ( _gridCursorCtrl != null && _refParams != null )
            {
                _refParams.StagePropTileIndex = _gridCursorCtrl.GetGridCursorX() + _gridCursorCtrl.GetGridCursorY() * _refParams.Col;
            }

            if ( _boundProp == null || _gridCursorCtrl == null ) return;
            var center = GridPositionUtility.CalcSizeAwareCenter(
                _refParams.StagePropTileIndex, _refParams.StagePropSize, _stageDataProvider );
            _boundProp.SetPosition( center );
            _boundProp.SetRotation( ( Direction ) _refParams.StagePropDirection );
        }

        private void BuildParamDescriptors()
        {
            _params.Clear();
            // Prefab: 変更可能。値変更時に RebuildBoundProp() でビジュアルを差し替える
            _params.Add( new ParamDescriptor { Name = "Prefab",    Min = 0,                        Max = (int)STAGE_PROPS.NUM - 1, Getter = () => _refParams.StagePropPrefab,    Setter = v => _refParams.StagePropPrefab    = v } );
            _params.Add( new ParamDescriptor { Name = "Size",      Min = Constants.GRID_SIZE_MIN,  Max = Constants.GRID_SIZE_MAX, Getter = () => _refParams.StagePropSize,      Setter = v => _refParams.StagePropSize      = v } );
            // TileIndex: カーソル位置から自動設定されるため読み取り専用
            _params.Add( new ParamDescriptor { Name = "TileIndex", Min = int.MaxValue,             Max = int.MinValue,            Getter = () => _refParams.StagePropTileIndex, Setter = v => { } } );
            _params.Add( new ParamDescriptor { Name = "Direction", Min = 0,                        Max = 3,                       Getter = () => _refParams.StagePropDirection, Setter = v => _refParams.StagePropDirection = v } );
        }

        public override bool CanAcceptConfirm() { return true; }
        public override bool CanAcceptSub1() { return 0 < _refParams.SelectedStagePropParamIndex; }
        public override bool CanAcceptSub2() { return _refParams.SelectedStagePropParamIndex < _params.Count - 1; }

        public override bool CanAcceptSub3()
        {
            if ( _params.Count == 0 ) return false;
            var p = _params[_refParams.SelectedStagePropParamIndex];
            return p.Min < p.Getter();
        }

        public override bool CanAcceptSub4()
        {
            if ( _params.Count == 0 ) return false;
            var p = _params[_refParams.SelectedStagePropParamIndex];
            return p.Getter() < p.Max;
        }

        public override bool AcceptConfirm( InputContext context )
        {
            if ( !base.AcceptConfirm( context ) ) return false;

            _confirmed = true;
            int newGridIndex = _gridCursorCtrl.GetGridCursorX() + _gridCursorCtrl.GetGridCursorY() * _refParams.Col;
            _context.ExtraIntValues.Clear();
            _context.ExtraIntValues.Add( _refParams.EditingStagePropGridIndex );  // [0] 旧グリッドインデックス
            _context.ExtraIntValues.Add( newGridIndex );                           // [1] 新グリッドインデックス
            OwnCallback( _context );
            _boundProp = null;
            OnCompleted?.Invoke();
            return true;
        }

        public override bool AcceptSub1( InputContext context )
        {
            if ( !base.AcceptSub1( context ) ) return false;
            _refParams.SelectedStagePropParamIndex = Math.Max( 0, _refParams.SelectedStagePropParamIndex - 1 );
            return true;
        }

        public override bool AcceptSub2( InputContext context )
        {
            if ( !base.AcceptSub2( context ) ) return false;
            _refParams.SelectedStagePropParamIndex = Math.Min( _params.Count - 1, _refParams.SelectedStagePropParamIndex + 1 );
            return true;
        }

        public override bool AcceptSub3( InputContext context )
        {
            if ( !base.AcceptSub3( context ) ) return false;
            var p = _params[_refParams.SelectedStagePropParamIndex];
            p.Setter( Math.Clamp( p.Getter() - 1, p.Min, p.Max ) );
            if ( p.Name == "Size"   ) ApplySizeToBoundProp();
            if ( p.Name == "Prefab" ) RebuildBoundProp();
            return true;
        }

        public override bool AcceptSub4( InputContext context )
        {
            if ( !base.AcceptSub4( context ) ) return false;
            var p = _params[_refParams.SelectedStagePropParamIndex];
            p.Setter( Math.Clamp( p.Getter() + 1, p.Min, p.Max ) );
            if ( p.Name == "Size"   ) ApplySizeToBoundProp();
            if ( p.Name == "Prefab" ) RebuildBoundProp();
            return true;
        }

        /// <summary>
        /// 選択中プレハブに合わせて、編集中プロップのビジュアル（モデル）を作り直します。
        /// 旧モデルを破棄し、新プレハブから生成して共通マップ(GridIndexToStageProp)も差し替えます。
        /// </summary>
        private void RebuildBoundProp()
        {
            if ( _prefabReg?.StagePropPrefabs == null ) return;
            int idx = _refParams.StagePropPrefab;
            if ( idx < 0 || _prefabReg.StagePropPrefabs.Length <= idx ) return;
            if ( idx == _builtPrefab ) return;   // 変化なし

            int gridIndex = _refParams.EditingStagePropGridIndex;
            if ( _boundProp != null ) GameObject.Destroy( _boundProp.gameObject );

            _boundProp = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<StageProp>(
                _prefabReg.StagePropPrefabs[idx], true, true, "[StagePropEditing]" );
            _builtPrefab = idx;
            if ( _boundProp == null ) return;

            // 共通マップを新しいビジュアルへ差し替え（カーソル/削除/Confirm 後処理が参照する）
            _refParams.GridIndexToStageProp[gridIndex] = _boundProp;

            // 現在のサイズ・位置・向きを反映
            _boundProp.SetSize( _refParams.StagePropSize );
            var center = GridPositionUtility.CalcSizeAwareCenter(
                _refParams.StagePropTileIndex, _refParams.StagePropSize, _stageDataProvider );
            _boundProp.SetPosition( center );
            _boundProp.SetRotation( ( Direction ) _refParams.StagePropDirection );
        }

        private void ApplySizeToBoundProp()
        {
            if ( _boundProp == null ) return;
            _boundProp.SetSize( _refParams.StagePropSize );
            _refParams.SetGridCursorSize?.Invoke( _refParams.StagePropSize );
            var center = GridPositionUtility.CalcSizeAwareCenter(
                _refParams.StagePropTileIndex, _refParams.StagePropSize, _stageDataProvider );
            _boundProp.SetPosition( center );
        }
    }
}

#endif // UNITY_EDITOR
