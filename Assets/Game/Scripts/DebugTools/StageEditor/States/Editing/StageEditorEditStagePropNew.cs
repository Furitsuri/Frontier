using Frontier.Entities;
using Frontier.Registries;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    /// <summary>
    /// 新規 StageProp を配置するクラス。
    /// カーソル移動に合わせてプレビューを追従させ、TileIndex をリアルタイム更新します。
    /// Sub1/Sub2 で選択パラメータを前後に移動し、Sub3/Sub4 で値を増減します。
    /// Confirm でデータを登録してプレビューを固定し、次のプレビューを生成します。
    /// Cancel (StageEditorEditStagePropCursor 経由) でカーソルモードに戻り、配置済みプロップをすべて破棄します。
    /// </summary>
    public class StageEditorEditStagePropNew : StageEditorEditBase
    {
        private struct ParamDescriptor
        {
            public string      Name;
            public int         Min;
            public int         Max;
            public Func<int>   Getter;
            public Action<int> Setter;
        }

        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private PrefabRegistry       _prefabReg    = null;

        private List<ParamDescriptor>  _params          = new List<ParamDescriptor>();
        private StageProp              _previewProp     = null;
        private int                    _shownPrefabIndex = -1;

        private List<StageProp>             _placedProps    = new List<StageProp>();
        private Dictionary<int, StageProp>  _placedPropMap  = new Dictionary<int, StageProp>();

        public override string GetSub12Label() => "PREV/NEXT\nPARAM";
        public override string GetSub34Label() => "DEC/INC\nVALUE";

        // ──── 初期化 ──────────────────────────────────────────────────────

        public override void Init( Action<EditActionContext> callback )
        {
            base.Init( callback );
            BuildParamDescriptors();
            _refParams.SelectedStagePropParamIndex = 0;
            ShowPreview();
        }

        public override void Exit()
        {
            HidePreview();
            DestroyPlacedProps();
        }

        /// <summary>
        /// 別サブモードへ遷移する際に呼びます。プレビューのみ非表示にし、配置済みプロップを保持します。
        /// </summary>
        public void ExitKeepPlaced()
        {
            HidePreview();
        }

        private void BuildParamDescriptors()
        {
            int maxPrefab = (int)STAGE_PROPS.NUM - 1;

            _params.Clear();
            _params.Add( new ParamDescriptor { Name = "Prefab",    Min = 0,            Max = maxPrefab,    Getter = () => _refParams.StagePropPrefab,    Setter = v => _refParams.StagePropPrefab    = v } );
            // TileIndex: カーソル位置から自動設定されるため読み取り専用
            _params.Add( new ParamDescriptor { Name = "TileIndex", Min = int.MaxValue, Max = int.MinValue, Getter = () => _refParams.StagePropTileIndex, Setter = v => { } } );
            _params.Add( new ParamDescriptor { Name = "Direction", Min = 0,            Max = 3,            Getter = () => _refParams.StagePropDirection, Setter = v => _refParams.StagePropDirection = v } );
        }

        // ──── 毎フレーム更新 ──────────────────────────────────────────────

        public override void Update()
        {
            base.Update();
            UpdatePreviewPosition();
        }

        private void UpdatePreviewPosition()
        {
            _refParams.StagePropTileIndex = _gridCursor.X() + _gridCursor.Y() * _refParams.Col;

            if ( _previewProp == null ) return;
            _previewProp.SetPosition( _gridCursor.GetPosition() );
            _previewProp.SetRotation( ( Direction ) _refParams.StagePropDirection );
        }

        // ──── プレビュー管理 ──────────────────────────────────────────────

        private void ShowPreview() { RefreshPreview( force: true ); }

        private void HidePreview()
        {
            if ( _previewProp != null )
            {
                GameObject.Destroy( _previewProp.gameObject );
                _previewProp      = null;
                _shownPrefabIndex = -1;
            }
        }

        private void RefreshPreview( bool force = false )
        {
            int prefabIdx = _refParams.StagePropPrefab;
            if ( !force && prefabIdx == _shownPrefabIndex && _previewProp != null ) return;

            HidePreview();

            if ( _prefabReg?.StagePropPrefabs == null ) return;
            if ( prefabIdx < 0 || _prefabReg.StagePropPrefabs.Length <= prefabIdx ) return;

            _previewProp = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<StageProp>(
                _prefabReg.StagePropPrefabs[prefabIdx], true, true, "[StagePropPreview]" );
            if ( _previewProp == null ) return;

            _shownPrefabIndex = prefabIdx;
            UpdatePreviewPosition();
        }

        private void FreezePreviewAndSpawnNext( int placedIndex )
        {
            if ( _previewProp != null )
            {
                _previewProp.gameObject.name = $"[StagePropPlaced_{placedIndex}]";
                _placedProps.Add( _previewProp );
                _placedPropMap[_refParams.StagePropTileIndex] = _previewProp;
                _refParams.GridIndexToStageProp[_refParams.StagePropTileIndex] = _previewProp;
                _previewProp      = null;
                _shownPrefabIndex = -1;
            }
            ShowPreview();
        }

        private void DestroyPlacedProps()
        {
            foreach ( var p in _placedProps )
            {
                if ( p == null ) continue;
                foreach ( var kvp in new Dictionary<int, StageProp>( _refParams.GridIndexToStageProp ) )
                {
                    if ( kvp.Value == p ) { _refParams.GridIndexToStageProp.Remove( kvp.Key ); break; }
                }
                GameObject.Destroy( p.gameObject );
            }
            _placedProps.Clear();
            _placedPropMap.Clear();
        }

        // ──── 入力受付判定 ────────────────────────────────────────────────

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

        // ──── 入力処理 ────────────────────────────────────────────────────

        public override bool AcceptConfirm( InputContext context )
        {
            if ( !base.AcceptConfirm( context ) ) return false;

            _context.X = _gridCursor.X();
            _context.Y = _gridCursor.Y();
            OwnCallback( _context );

            FreezePreviewAndSpawnNext( _placedProps.Count );
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
            if ( p.Name == "Prefab" ) RefreshPreview();
            return true;
        }

        public override bool AcceptSub4( InputContext context )
        {
            if ( !base.AcceptSub4( context ) ) return false;
            var p = _params[_refParams.SelectedStagePropParamIndex];
            p.Setter( Math.Clamp( p.Getter() + 1, p.Min, p.Max ) );
            if ( p.Name == "Prefab" ) RefreshPreview();
            return true;
        }
    }
}

#endif // UNITY_EDITOR
