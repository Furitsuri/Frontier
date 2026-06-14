using Frontier.Combat;
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
    /// 新規敵キャラクターを配置するクラス。
    /// カーソル移動に合わせてプレビューを追従させ、InitGridIndex をリアルタイム更新します。
    /// Sub1/Sub2 で選択パラメータを前後に移動し、Sub3/Sub4 で値を増減します。
    /// Confirm で敵データを登録してプレビューをその場に固定し、新しいプレビューを生成します。
    /// Cancel (StageEditorEditEnemyCursor 経由) でカーソルモードに戻り、固定済みキャラクターをすべて破棄します。
    /// </summary>
    public class StageEditorEditEnemyNew : StageEditorEditBase
    {
        private struct ParamDescriptor
        {
            public string      Name;
            public int         Min;
            public int         Max;
            public Func<int>   Getter;
            public Action<int> Setter;
        }

        [Inject] private PrefabRegistry   _prefabReg        = null;
        [Inject] private CharacterFactory _characterFactory = null;

        private List<ParamDescriptor> _params           = new List<ParamDescriptor>();
        private Character             _previewCharacter  = null;
        private int                   _shownPrefabIndex  = -1;

        // Confirm で固定した配置済みキャラクター。Exit() で一括破棄。
        private List<Character>             _placedCharacters    = new List<Character>();
        // グリッドインデックス → キャラクター の逆引きマップ
        private Dictionary<int, Character>  _placedCharacterMap  = new Dictionary<int, Character>();

        public override string GetSub12Label() => "PREV/NEXT\nPARAM";
        public override string GetSub34Label() => "DEC/INC\nVALUE";

        // ──── 初期化 ──────────────────────────────────────────────────────

        public override void Init( Action<EditActionContext> callback )
        {
            base.Init( callback );
            BuildParamDescriptors();
            _refParams.SelectedEnemyParamIndex = 0;
            ShowPreview();
        }

        public override void Exit()
        {
            HidePreview();
            DestroyPlacedCharacters();
        }

        /// <summary>
        /// 敵エディット内の別サブモードへ遷移する際に呼びます。
        /// プレビューのみ非表示にし、配置済みキャラクターは保持します。
        /// </summary>
        public void ExitKeepPlaced()
        {
            HidePreview();
        }

        private void BuildParamDescriptors()
        {
            int maxPrefab = ( _prefabReg != null && _prefabReg.EnemyPrefabs != null )
                ? Mathf.Max( 0, _prefabReg.EnemyPrefabs.Length - 1 )
                : 0;

            _params.Clear();
            _params.Add( new ParamDescriptor { Name = "Prefab",      Min = 0,            Max = maxPrefab,    Getter = () => _refParams.EnemyPrefab,      Setter = v => _refParams.EnemyPrefab      = v } );
            _params.Add( new ParamDescriptor { Name = "Level",       Min = 1,            Max = 99,           Getter = () => _refParams.EnemyLevel,       Setter = v => _refParams.EnemyLevel       = v } );
            _params.Add( new ParamDescriptor { Name = "MaxHP",       Min = 1,            Max = 9999,         Getter = () => _refParams.EnemyMaxHP,       Setter = v => _refParams.EnemyMaxHP       = v } );
            _params.Add( new ParamDescriptor { Name = "Atk",         Min = 0,            Max = 999,          Getter = () => _refParams.EnemyAtk,         Setter = v => _refParams.EnemyAtk         = v } );
            _params.Add( new ParamDescriptor { Name = "Def",         Min = 0,            Max = 999,          Getter = () => _refParams.EnemyDef,         Setter = v => _refParams.EnemyDef         = v } );
            _params.Add( new ParamDescriptor { Name = "MoveRange",   Min = 1,            Max = 10,           Getter = () => _refParams.EnemyMoveRange,   Setter = v => _refParams.EnemyMoveRange   = v } );
            _params.Add( new ParamDescriptor { Name = "JumpForce",   Min = 0,            Max = 10,           Getter = () => _refParams.EnemyJumpForce,   Setter = v => _refParams.EnemyJumpForce   = v } );
            _params.Add( new ParamDescriptor { Name = "AtkRange",    Min = 1,            Max = 10,           Getter = () => _refParams.EnemyAtkRange,    Setter = v => _refParams.EnemyAtkRange    = v } );
            _params.Add( new ParamDescriptor { Name = "ActGaugeMax", Min = 1,            Max = 9999,         Getter = () => _refParams.EnemyActGaugeMax, Setter = v => _refParams.EnemyActGaugeMax = v } );
            _params.Add( new ParamDescriptor { Name = "ActRecovery", Min = 0,            Max = 999,          Getter = () => _refParams.EnemyActRecovery, Setter = v => _refParams.EnemyActRecovery = v } );
            _params.Add( new ParamDescriptor { Name = "ThinkType",   Min = 0,            Max = 10,           Getter = () => _refParams.EnemyThinkType,   Setter = v => _refParams.EnemyThinkType   = v } );
            // InitGridIndex はカーソル位置から UpdatePreviewPosition() で自動設定。Sub3/Sub4 は無効。
            _params.Add( new ParamDescriptor { Name = "InitGridIndex", Min = int.MaxValue, Max = int.MinValue, Getter = () => _refParams.EnemyInitGridIndex, Setter = v => { } } );
            _params.Add( new ParamDescriptor { Name = "InitDir",  Min = 0,                    Max = 3,                    Getter = () => _refParams.EnemyInitDir,  Setter = v => _refParams.EnemyInitDir  = v } );
            _params.Add( new ParamDescriptor { Name = "Skill1",   Min = ( int ) SkillID.NONE - 1, Max = ( int ) SkillID.NUM,  Getter = () => _refParams.EnemySkill1,  Setter = v => _refParams.EnemySkill1  = v } );
            _params.Add( new ParamDescriptor { Name = "Skill2",   Min = ( int ) SkillID.NONE - 1, Max = ( int ) SkillID.NUM,  Getter = () => _refParams.EnemySkill2,  Setter = v => _refParams.EnemySkill2  = v } );
            _params.Add( new ParamDescriptor { Name = "Skill3",   Min = ( int ) SkillID.NONE - 1, Max = ( int ) SkillID.NUM,  Getter = () => _refParams.EnemySkill3,  Setter = v => _refParams.EnemySkill3  = v } );
            _params.Add( new ParamDescriptor { Name = "Skill4",   Min = ( int ) SkillID.NONE - 1, Max = ( int ) SkillID.NUM,  Getter = () => _refParams.EnemySkill4,  Setter = v => _refParams.EnemySkill4  = v } );
        }

        // ──── 毎フレーム更新 ──────────────────────────────────────────────

        public override void Update()
        {
            base.Update();
            UpdatePreviewPosition();
        }

        private void UpdatePreviewPosition()
        {
            // カーソル位置から InitGridIndex をリアルタイム更新
            _refParams.EnemyInitGridIndex = _gridCursor.X() + _gridCursor.Y() * _refParams.Col;

            if ( _previewCharacter == null ) return;
            _previewCharacter.SetPosition( _gridCursor.GetPosition() );
            _previewCharacter.SetRotation( ( Direction ) _refParams.EnemyInitDir );
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
                _previewCharacter = null;
                _shownPrefabIndex = -1;
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

            _previewCharacter = _characterFactory.CreateCharacter( CHARACTER_TAG.ENEMY, prefabIdx );
            if ( _previewCharacter == null ) return;

            _previewCharacter.gameObject.name = "[EnemyPreview]";
            _shownPrefabIndex = prefabIdx;

            UpdatePreviewPosition();
        }

        /// <summary>
        /// 現在のプレビューキャラクターを配置位置で固定し、_placedCharacters に追加します。
        /// 新しいプレビューを生成して次の配置に備えます。
        /// </summary>
        private void FreezePreviewAndSpawnNext( int placedIndex )
        {
            if ( _previewCharacter != null )
            {
                _previewCharacter.gameObject.name = $"[EnemyPlaced_{placedIndex}]";
                _placedCharacters.Add( _previewCharacter );
                _placedCharacterMap[_refParams.EnemyInitGridIndex] = _previewCharacter;
                _refParams.GridIndexToCharacter[_refParams.EnemyInitGridIndex] = _previewCharacter;
                _previewCharacter = null;
                _shownPrefabIndex = -1;
            }

            ShowPreview();
        }

        private void DestroyPlacedCharacters()
        {
            foreach ( var c in _placedCharacters )
            {
                if ( c == null ) continue;

                // 共通マップからも除去
                foreach ( var kvp in new Dictionary<int, Character>( _refParams.GridIndexToCharacter ) )
                {
                    if ( kvp.Value == c )
                    {
                        _refParams.GridIndexToCharacter.Remove( kvp.Key );
                        break;
                    }
                }

                GameObject.Destroy( c.gameObject );
            }
            _placedCharacters.Clear();
            _placedCharacterMap.Clear();
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

        /// <summary>
        /// 現在位置に敵データを登録し、プレビューをその場に固定して次の配置用プレビューを生成します。
        /// </summary>
        public override bool AcceptConfirm( InputContext context )
        {
            if ( !base.AcceptConfirm( context ) ) return false;

            _context.X = _gridCursor.X();
            _context.Y = _gridCursor.Y();
            OwnCallback( _context );

            // 配置済みキャラクターとして現プレビューを固定し、次の配置用プレビューを生成
            FreezePreviewAndSpawnNext( _placedCharacters.Count );

            OnCompleted?.Invoke();
            return true;
        }

        public override bool AcceptSub1( InputContext context )
        {
            if ( !base.AcceptSub1( context ) ) return false;

            _refParams.SelectedEnemyParamIndex = Math.Max( 0, _refParams.SelectedEnemyParamIndex - 1 );
            return true;
        }

        public override bool AcceptSub2( InputContext context )
        {
            if ( !base.AcceptSub2( context ) ) return false;

            _refParams.SelectedEnemyParamIndex = Math.Min( _params.Count - 1, _refParams.SelectedEnemyParamIndex + 1 );
            return true;
        }

        public override bool AcceptSub3( InputContext context )
        {
            if ( !base.AcceptSub3( context ) ) return false;

            var p = _params[_refParams.SelectedEnemyParamIndex];
            p.Setter( Math.Clamp( p.Getter() - 1, p.Min, p.Max ) );

            if ( p.Name == "Prefab" ) RefreshPreview();
            return true;
        }

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
