using Frontier.Entities;
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

        [Inject] private IStageDataProvider _stageDataProvider = null;

        private List<ParamDescriptor> _params    = new List<ParamDescriptor>();
        private StageProp             _boundProp = null;
        private bool                  _confirmed = false;

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
            _confirmed = false;

            _refParams.SetGridCursorSize?.Invoke( _refParams.StagePropSize );

            // 編集前の状態をスナップショット
            _snapPrefab          = _refParams.StagePropPrefab;
            _snapSize            = _refParams.StagePropSize;
            _snapTileIndex       = _refParams.StagePropTileIndex;
            _snapDirection       = _refParams.StagePropDirection;
            _snapCursorTileIndex = _gridCursor != null ? _gridCursor.CurrentTileIndex : 0;
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

                if ( _boundProp != null )
                {
                    _boundProp.SetSize( _snapSize );
                    _boundProp.SetPosition( _snapPropPosition );
                    _boundProp.SetRotation( ( Direction ) _snapDirection );
                }

                if ( _gridCursor != null )
                {
                    _gridCursor.SetTileIndex( _snapCursorTileIndex );
                    _gridCursor.SyncPositionToTile();
                }
            }
            _boundProp = null;
        }

        public override void Update()
        {
            base.Update();

            // カーソル位置から TileIndex をリアルタイム更新
            if ( _gridCursor != null && _refParams != null )
            {
                _refParams.StagePropTileIndex = _gridCursor.X() + _gridCursor.Y() * _refParams.Col;
            }

            if ( _boundProp == null || _gridCursor == null ) return;
            var center = GridPositionUtility.CalcSizeAwareCenter(
                _refParams.StagePropTileIndex, _refParams.StagePropSize, _stageDataProvider );
            _boundProp.SetPosition( center );
            _boundProp.SetRotation( ( Direction ) _refParams.StagePropDirection );
        }

        private void BuildParamDescriptors()
        {
            _params.Clear();
            // Prefab: 既存プロップのプレハブ変更は非対応のため読み取り専用
            _params.Add( new ParamDescriptor { Name = "Prefab",    Min = int.MaxValue,             Max = int.MinValue,            Getter = () => _refParams.StagePropPrefab,    Setter = v => { } } );
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
            int newGridIndex = _gridCursor.X() + _gridCursor.Y() * _refParams.Col;
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
            if ( p.Name == "Size" ) ApplySizeToBoundProp();
            return true;
        }

        public override bool AcceptSub4( InputContext context )
        {
            if ( !base.AcceptSub4( context ) ) return false;
            var p = _params[_refParams.SelectedStagePropParamIndex];
            p.Setter( Math.Clamp( p.Getter() + 1, p.Min, p.Max ) );
            if ( p.Name == "Size" ) ApplySizeToBoundProp();
            return true;
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
