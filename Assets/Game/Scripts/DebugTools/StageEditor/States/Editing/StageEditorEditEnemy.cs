using Frontier.Entities;
using Frontier.Registries;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Frontier.DebugTools.StageEditor.StageEditorController;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    /// <summary>
    /// 敵キャラクターのステータスパラメータをキー入力で編集するクラス
    /// Sub1/Sub2 で選択パラメータを前後に移動し、Sub3/Sub4 で値を増減します。
    /// Confirm でその時点のパラメータをコールバックに渡して登録します。
    /// Prefab フィールドが変更されると、グリッドカーソル位置にプレビューキャラクターが差し替わります。
    /// </summary>
    public class StageEditorEditEnemy : StageEditorEditBase
    {
        // ──── パラメータ記述子 ────────────────────────────────────────────

        private struct ParamDescriptor
        {
            public string        Name;
            public int           Min;
            public int           Max;
            public Func<int>     Getter;
            public Action<int>   Setter;
        }

        // ──── DI ─────────────────────────────────────────────────────────

        [Inject] private PrefabRegistry  _prefabReg        = null;
        [Inject] private CharacterFactory _characterFactory = null;

        // ──── 内部状態 ────────────────────────────────────────────────────

        private List<ParamDescriptor> _params           = new List<ParamDescriptor>();
        private Character             _previewCharacter  = null;
        private int                   _shownPrefabIndex  = -1;

        // ──── 初期化 ──────────────────────────────────────────────────────

        public override void Init( Action<EditActionContext> callback )
        {
            base.Init( callback );
            BuildParamDescriptors();
            ShowPreview();
        }

        public override void Exit()
        {
            HidePreview();
        }

        private void BuildParamDescriptors()
        {
            int maxPrefab = ( _prefabReg != null && _prefabReg.EnemyPrefabs != null )
                ? Mathf.Max( 0, _prefabReg.EnemyPrefabs.Length - 1 )
                : 0;

            _params.Clear();
            _params.Add( new ParamDescriptor { Name = "Level",       Min = 1,        Max = 99,       Getter = () => _refParams.EnemyLevel,       Setter = v => _refParams.EnemyLevel       = v } );
            _params.Add( new ParamDescriptor { Name = "MaxHP",       Min = 1,        Max = 9999,     Getter = () => _refParams.EnemyMaxHP,       Setter = v => _refParams.EnemyMaxHP       = v } );
            _params.Add( new ParamDescriptor { Name = "Atk",         Min = 0,        Max = 999,      Getter = () => _refParams.EnemyAtk,         Setter = v => _refParams.EnemyAtk         = v } );
            _params.Add( new ParamDescriptor { Name = "Def",         Min = 0,        Max = 999,      Getter = () => _refParams.EnemyDef,         Setter = v => _refParams.EnemyDef         = v } );
            _params.Add( new ParamDescriptor { Name = "MoveRange",   Min = 1,        Max = 10,       Getter = () => _refParams.EnemyMoveRange,   Setter = v => _refParams.EnemyMoveRange   = v } );
            _params.Add( new ParamDescriptor { Name = "JumpForce",   Min = 0,        Max = 10,       Getter = () => _refParams.EnemyJumpForce,   Setter = v => _refParams.EnemyJumpForce   = v } );
            _params.Add( new ParamDescriptor { Name = "AtkRange",    Min = 1,        Max = 10,       Getter = () => _refParams.EnemyAtkRange,    Setter = v => _refParams.EnemyAtkRange    = v } );
            _params.Add( new ParamDescriptor { Name = "ActGaugeMax", Min = 1,        Max = 9999,     Getter = () => _refParams.EnemyActGaugeMax, Setter = v => _refParams.EnemyActGaugeMax = v } );
            _params.Add( new ParamDescriptor { Name = "ActRecovery", Min = 0,        Max = 999,      Getter = () => _refParams.EnemyActRecovery, Setter = v => _refParams.EnemyActRecovery = v } );
            _params.Add( new ParamDescriptor { Name = "Prefab",      Min = 0,        Max = maxPrefab, Getter = () => _refParams.EnemyPrefab,     Setter = v => _refParams.EnemyPrefab      = v } );
            _params.Add( new ParamDescriptor { Name = "ThinkType",    Min = 0,        Max = 10,       Getter = () => _refParams.EnemyThinkType,    Setter = v => _refParams.EnemyThinkType    = v } );
            _params.Add( new ParamDescriptor { Name = "InitGridIndex", Min = 0,        Max = 624,      Getter = () => _refParams.EnemyInitGridIndex, Setter = v => _refParams.EnemyInitGridIndex = v } );
            _params.Add( new ParamDescriptor { Name = "InitDir",       Min = 0,        Max = 3,        Getter = () => _refParams.EnemyInitDir,       Setter = v => _refParams.EnemyInitDir       = v } );
        }

        // ──── 毎フレーム更新 ──────────────────────────────────────────────

        public override void Update()
        {
            base.Update();
            UpdatePreviewPosition();
        }

        private void UpdatePreviewPosition()
        {
            if ( _previewCharacter == null ) return;
            _previewCharacter.GetTransformHandler.SetPosition( _gridCursor.GetPosition() );
            _previewCharacter.GetTransformHandler.SetRotation( ( Direction ) _refParams.EnemyInitDir );
        }

        // ──── プレビュー管理 ──────────────────────────────────────────────

        private void ShowPreview()
        {
            RefreshPreview( force: true );
        }

        private void HidePreview()
        {
            if ( _previewCharacter != null )
            {
                GameObject.Destroy( _previewCharacter.gameObject );
                _previewCharacter  = null;
                _shownPrefabIndex  = -1;
            }
        }

        private void RefreshPreview( bool force = false )
        {
            int prefabIdx = _refParams.EnemyPrefab;

            if ( !force && prefabIdx == _shownPrefabIndex && _previewCharacter != null ) return;

            HidePreview();

            if ( _characterFactory == null ) return;
            if ( _prefabReg == null || _prefabReg.EnemyPrefabs == null ) return;
            if ( prefabIdx < 0 || _prefabReg.EnemyPrefabs.Length <= prefabIdx ) return;

            // CharacterFactory.CreateCharacter で生成することで Setup() が呼ばれ、
            // TransformHandler 等の内部コンポーネントが正しく初期化される
            _previewCharacter = _characterFactory.CreateCharacter( CHARACTER_TAG.ENEMY, prefabIdx );
            if ( _previewCharacter == null ) return;

            _previewCharacter.gameObject.name = "[EnemyPreview]";
            _shownPrefabIndex = prefabIdx;

            UpdatePreviewPosition();
        }

        // ──── 入力受付判定 ────────────────────────────────────────────────

        public override bool CanAcceptConfirm() { return true; }

        public override bool CanAcceptSub1()
        {
            return 0 < _refParams.SelectedEnemyParamIndex;
        }

        public override bool CanAcceptSub2()
        {
            return _refParams.SelectedEnemyParamIndex < _params.Count - 1;
        }

        public override bool CanAcceptSub3()
        {
            if ( _params.Count == 0 ) return false;
            var p = _params[_refParams.SelectedEnemyParamIndex];
            return p.Min < p.Getter();
        }

        public override bool CanAcceptSub4()
        {
            if ( _params.Count == 0 ) return false;
            var p = _params[_refParams.SelectedEnemyParamIndex];
            return p.Getter() < p.Max;
        }

        // ──── 入力処理 ────────────────────────────────────────────────────

        /// <summary>現在のテンプレートをコールバックに渡して登録します。</summary>
        public override bool AcceptConfirm( InputContext context )
        {
            if ( !base.AcceptConfirm( context ) ) return false;

            _context.X = _gridCursor.X();
            _context.Y = _gridCursor.Y();
            OwnCallback( _context );
            return true;
        }

        /// <summary>Sub1: 選択パラメータを前へ</summary>
        public override bool AcceptSub1( InputContext context )
        {
            if ( !base.AcceptSub1( context ) ) return false;

            _refParams.SelectedEnemyParamIndex = Math.Max( 0, _refParams.SelectedEnemyParamIndex - 1 );
            return true;
        }

        /// <summary>Sub2: 選択パラメータを次へ</summary>
        public override bool AcceptSub2( InputContext context )
        {
            if ( !base.AcceptSub2( context ) ) return false;

            _refParams.SelectedEnemyParamIndex = Math.Min( _params.Count - 1, _refParams.SelectedEnemyParamIndex + 1 );
            return true;
        }

        /// <summary>Sub3: 選択パラメータの値をデクリメント</summary>
        public override bool AcceptSub3( InputContext context )
        {
            if ( !base.AcceptSub3( context ) ) return false;

            var p = _params[_refParams.SelectedEnemyParamIndex];
            p.Setter( Math.Clamp( p.Getter() - 1, p.Min, p.Max ) );

            if ( p.Name == "Prefab" ) RefreshPreview();
            return true;
        }

        /// <summary>Sub4: 選択パラメータの値をインクリメント</summary>
        public override bool AcceptSub4( InputContext context )
        {
            if ( !base.AcceptSub4( context ) ) return false;

            var p = _params[_refParams.SelectedEnemyParamIndex];
            p.Setter( Math.Clamp( p.Getter() + 1, p.Min, p.Max ) );

            if ( p.Name == "Prefab" ) RefreshPreview();
            return true;
        }
    }
}

#endif // UNITY_EDITOR
