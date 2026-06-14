using Frontier.Combat;
using Frontier.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Frontier.DebugTools.StageEditor.StageEditorController;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    /// <summary>
    /// 配置済み敵キャラクターのパラメータを編集するクラス。
    /// Sub1/Sub2 で選択パラメータを前後に移動し、Sub3/Sub4 で値を増減します。
    /// Confirm で編集内容をコールバックに渡して反映し、カーソルモードへ戻ります。
    /// Cancel で編集前の状態に完全復元してカーソルモードへ戻ります。
    /// ExtraIntValues[0]=旧グリッドインデックス、[1]=新グリッドインデックス。
    /// </summary>
    public class StageEditorEditEnemyExisting : StageEditorEditBase
    {
        private struct ParamDescriptor
        {
            public string        Name;
            public int           Min;
            public int           Max;
            public Func<int>     Getter;
            public Action<int>   Setter;
        }

        private List<ParamDescriptor> _params         = new List<ParamDescriptor>();
        private Character             _boundCharacter = null;
        private bool                  _confirmed      = false;

        // Cancel 時に復元するためのスナップショット
        private Vector3 _snapCharacterPosition;
        private int     _snapCursorTileIndex;
        private int     _snapLevel, _snapMaxHP, _snapAtk, _snapDef;
        private int     _snapMoveRange, _snapJumpForce, _snapAtkRange;
        private int     _snapActGaugeMax, _snapActRecovery, _snapPrefab;
        private int     _snapThinkType, _snapInitGridIndex, _snapInitDir;
        private int     _snapSkill1, _snapSkill2, _snapSkill3, _snapSkill4;

        public override string GetSub12Label() => "PREV/NEXT\nPARAM";
        public override string GetSub34Label() => "DEC/INC\nVALUE";

        /// <summary>カーソルと連動させるキャラクターを設定します。Init() より前に呼んでください。</summary>
        public void SetBoundCharacter( Character character )
        {
            _boundCharacter = character;
        }

        public override void Init( Action<EditActionContext> callback )
        {
            base.Init( callback );
            BuildParamDescriptors();
            _refParams.SelectedEnemyParamIndex = 0;
            _confirmed = false;

            // 編集前の状態をスナップショット
            _snapLevel         = _refParams.EnemyLevel;
            _snapMaxHP         = _refParams.EnemyMaxHP;
            _snapAtk           = _refParams.EnemyAtk;
            _snapDef           = _refParams.EnemyDef;
            _snapMoveRange     = _refParams.EnemyMoveRange;
            _snapJumpForce     = _refParams.EnemyJumpForce;
            _snapAtkRange      = _refParams.EnemyAtkRange;
            _snapActGaugeMax   = _refParams.EnemyActGaugeMax;
            _snapActRecovery   = _refParams.EnemyActRecovery;
            _snapPrefab        = _refParams.EnemyPrefab;
            _snapThinkType     = _refParams.EnemyThinkType;
            _snapInitGridIndex = _refParams.EnemyInitGridIndex;
            _snapInitDir       = _refParams.EnemyInitDir;
            _snapSkill1            = _refParams.EnemySkill1;
            _snapSkill2            = _refParams.EnemySkill2;
            _snapSkill3            = _refParams.EnemySkill3;
            _snapSkill4            = _refParams.EnemySkill4;
            _snapCursorTileIndex   = _gridCursor != null ? _gridCursor.CurrentTileIndex : 0;
            _snapCharacterPosition = _boundCharacter != null
                ? _boundCharacter.transform.position
                : Vector3.zero;
        }

        public override void Exit()
        {
            if ( !_confirmed )
            {
                // Cancel による退出: パラメータとキャラクター位置を編集前に戻す
                _refParams.EnemyLevel        = _snapLevel;
                _refParams.EnemyMaxHP        = _snapMaxHP;
                _refParams.EnemyAtk          = _snapAtk;
                _refParams.EnemyDef          = _snapDef;
                _refParams.EnemyMoveRange    = _snapMoveRange;
                _refParams.EnemyJumpForce    = _snapJumpForce;
                _refParams.EnemyAtkRange     = _snapAtkRange;
                _refParams.EnemyActGaugeMax  = _snapActGaugeMax;
                _refParams.EnemyActRecovery  = _snapActRecovery;
                _refParams.EnemyPrefab       = _snapPrefab;
                _refParams.EnemyThinkType    = _snapThinkType;
                _refParams.EnemyInitGridIndex = _snapInitGridIndex;
                _refParams.EnemyInitDir       = _snapInitDir;
                _refParams.EnemySkill1        = _snapSkill1;
                _refParams.EnemySkill2        = _snapSkill2;
                _refParams.EnemySkill3        = _snapSkill3;
                _refParams.EnemySkill4        = _snapSkill4;

                if ( _boundCharacter != null )
                {
                    _boundCharacter.SetPosition( _snapCharacterPosition );
                    _boundCharacter.SetRotation( ( Direction ) _snapInitDir );
                }

                if ( _gridCursor != null )
                {
                    _gridCursor.SetTileIndex( _snapCursorTileIndex );
                    _gridCursor.SyncPositionToTile();
                }
            }
            _boundCharacter = null;
        }

        public override void Update()
        {
            base.Update();

            // カーソル位置から InitGridIndex をリアルタイム更新して表示に反映
            if ( _gridCursor != null && _refParams != null )
            {
                _refParams.EnemyInitGridIndex = _gridCursor.X() + _gridCursor.Y() * _refParams.Col;
            }

            if ( _boundCharacter == null || _gridCursor == null ) return;
            _boundCharacter.SetPosition( _gridCursor.GetPosition() );
            _boundCharacter.SetRotation( ( Direction ) _refParams.EnemyInitDir );
        }

        private void BuildParamDescriptors()
        {
            _params.Clear();
            // EnemyParamNames と同じ順・同じインデックスで 13 エントリを揃える
            // Prefab: 既存キャラクターのプレハブ変更は非対応のため読み取り専用
            _params.Add( new ParamDescriptor { Name = "Prefab",       Min = int.MaxValue, Max = int.MinValue, Getter = () => _refParams.EnemyPrefab,       Setter = v => { } } );
            _params.Add( new ParamDescriptor { Name = "Level",        Min = 1,            Max = 99,           Getter = () => _refParams.EnemyLevel,        Setter = v => _refParams.EnemyLevel        = v } );
            _params.Add( new ParamDescriptor { Name = "MaxHP",        Min = 1,            Max = 9999,         Getter = () => _refParams.EnemyMaxHP,        Setter = v => _refParams.EnemyMaxHP        = v } );
            _params.Add( new ParamDescriptor { Name = "Atk",          Min = 0,            Max = 999,          Getter = () => _refParams.EnemyAtk,          Setter = v => _refParams.EnemyAtk          = v } );
            _params.Add( new ParamDescriptor { Name = "Def",          Min = 0,            Max = 999,          Getter = () => _refParams.EnemyDef,          Setter = v => _refParams.EnemyDef          = v } );
            _params.Add( new ParamDescriptor { Name = "MoveRange",    Min = 1,            Max = 10,           Getter = () => _refParams.EnemyMoveRange,    Setter = v => _refParams.EnemyMoveRange    = v } );
            _params.Add( new ParamDescriptor { Name = "JumpForce",    Min = 0,            Max = 10,           Getter = () => _refParams.EnemyJumpForce,    Setter = v => _refParams.EnemyJumpForce    = v } );
            _params.Add( new ParamDescriptor { Name = "AtkRange",     Min = 1,            Max = 10,           Getter = () => _refParams.EnemyAtkRange,     Setter = v => _refParams.EnemyAtkRange     = v } );
            _params.Add( new ParamDescriptor { Name = "ActGaugeMax",  Min = 1,            Max = 9999,         Getter = () => _refParams.EnemyActGaugeMax,  Setter = v => _refParams.EnemyActGaugeMax  = v } );
            _params.Add( new ParamDescriptor { Name = "ActRecovery",  Min = 0,            Max = 999,          Getter = () => _refParams.EnemyActRecovery,  Setter = v => _refParams.EnemyActRecovery  = v } );
            _params.Add( new ParamDescriptor { Name = "ThinkType",    Min = 0,            Max = 10,           Getter = () => _refParams.EnemyThinkType,    Setter = v => _refParams.EnemyThinkType    = v } );
            // InitGridIndex: カーソル位置から自動設定されるため読み取り専用
            _params.Add( new ParamDescriptor { Name = "InitGridIndex", Min = int.MaxValue, Max = int.MinValue, Getter = () => _refParams.EnemyInitGridIndex, Setter = v => { } } );
            _params.Add( new ParamDescriptor { Name = "InitDir",  Min = 0,                    Max = 3,                    Getter = () => _refParams.EnemyInitDir,  Setter = v => _refParams.EnemyInitDir  = v } );
            _params.Add( new ParamDescriptor { Name = "Skill1",   Min = ( int ) SkillID.NONE - 1, Max = ( int ) SkillID.NUM,  Getter = () => _refParams.EnemySkill1,  Setter = v => _refParams.EnemySkill1  = v } );
            _params.Add( new ParamDescriptor { Name = "Skill2",   Min = ( int ) SkillID.NONE - 1, Max = ( int ) SkillID.NUM,  Getter = () => _refParams.EnemySkill2,  Setter = v => _refParams.EnemySkill2  = v } );
            _params.Add( new ParamDescriptor { Name = "Skill3",   Min = ( int ) SkillID.NONE - 1, Max = ( int ) SkillID.NUM,  Getter = () => _refParams.EnemySkill3,  Setter = v => _refParams.EnemySkill3  = v } );
            _params.Add( new ParamDescriptor { Name = "Skill4",   Min = ( int ) SkillID.NONE - 1, Max = ( int ) SkillID.NUM,  Getter = () => _refParams.EnemySkill4,  Setter = v => _refParams.EnemySkill4  = v } );
        }

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

        public override bool AcceptConfirm( InputContext context )
        {
            if ( !base.AcceptConfirm( context ) ) return false;

            _confirmed = true;
            int newGridIndex = _gridCursor.X() + _gridCursor.Y() * _refParams.Col;
            _context.ExtraIntValues.Clear();
            _context.ExtraIntValues.Add( _refParams.EditingEnemyGridIndex );  // [0] 旧グリッドインデックス
            _context.ExtraIntValues.Add( newGridIndex );                       // [1] 新グリッドインデックス
            OwnCallback( _context );
            _boundCharacter = null;
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
            return true;
        }

        public override bool AcceptSub4( InputContext context )
        {
            if ( !base.AcceptSub4( context ) ) return false;

            var p = _params[_refParams.SelectedEnemyParamIndex];
            p.Setter( Math.Clamp( p.Getter() + 1, p.Min, p.Max ) );
            return true;
        }
    }
}

#endif // UNITY_EDITOR
