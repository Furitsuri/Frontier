using Frontier.Combat;
using Frontier.Entities.Ai;
using Frontier.UI;
using System;
using System.Collections.Generic;
using static Frontier.Entities.Player;
using static Constants;

namespace Frontier.Entities
{
    public class PlayerBattleLogic : BattleLogicBase
    {
        private PrevMoveInfo _prevMoveInfo;
        private Stack<COMMAND_TAG> _commandHistory = new Stack<COMMAND_TAG>();    // コマンド履歴(攻撃シーケンスにおいて使用)
        private Action[] _revertCommandStateFuncs;

        public ref PrevMoveInfo PrevMoveInformaiton => ref _prevMoveInfo;

        public override void Init()
        {
            base.Init();

            _paramWinType = ParameterWindowType.Left;

            LazyInject.GetOrCreate( ref _baseAi, () => _hierarchyBld.InstantiateWithDiContainer<AiBase>( false ) );

            _revertCommandStateFuncs = new Action[( int ) COMMAND_TAG.NUM]
            {
                RevertBeforeMoving, // COMMAND_TAG.MOVE
                null,               // COMMAND_TAG.ATTACK
                RevertUsedSkills,   // COMMAND_TAG.SKILL
                null,               // COMMAND_TAG.WAIT
            };

            _baseAi.Init( _readOnlyOwner.Value );
        }

        /// <summary>
        /// 現在の移動前情報を適応します
        /// </summary>
        public void HoldBeforeMoveInfo()
        {
            _prevMoveInfo.tmpParam  = _readOnlyOwner.Value.BattleParams.TmpParam.Clone();
            _prevMoveInfo.rotDir    = _readOnlyOwner.Value.GetRotation();
        }

        public void PushCommandHistory( COMMAND_TAG commandTag )
        {
            _commandHistory.Push( commandTag );
        }

        public COMMAND_TAG PopCommandHistory()
        {
            if( 0 < _commandHistory.Count )
            {
                return _commandHistory.Pop();
            }

            return COMMAND_TAG.NONE;
        }

        public void ClearCommandHistory()
        {
            _commandHistory.Clear();
        }

        /// <summary>
        /// 指定コマンドを実行完了として確定します。以前の状態には戻せなくなるため、
        /// 攻撃・スキルなど実行し終えたコマンドに対して呼び出してください。
        /// </summary>
        public void FinalizeCommand( COMMAND_TAG commandTag )
        {
            _readOnlyOwner.Value.BattleParams.TmpParam.SetEndCommandStatus( commandTag, true );
            ClearCommandHistory();
        }

        public void RevertToPreviousExecCommand( COMMAND_TAG commandTag )
        {
            if( _revertCommandStateFuncs[( int ) commandTag] == null ) { return; }

            _revertCommandStateFuncs[( int ) commandTag]();
        }

        /// <summary>
        /// 移動前の状態に巻き戻します
        /// </summary>
        public void RevertBeforeMoving()
        {
            ForcedStopMoving();
            _readOnlyOwner.Value.BattleParams.TmpParam = _prevMoveInfo.tmpParam;
            SetPositionOnStage( _readOnlyOwner.Value.BattleParams.TmpParam.CurrentTileIndex, _prevMoveInfo.rotDir );
        }

        /// <summary>
        /// 実行済みのスキルを全て取り消します
        /// </summary>
        public void RevertUsedSkills()
        {
            int totalCost = 0;

            for( int i = 0; i < EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                if( IsUsingEquipSkill( i ) )
                {
                    var skillID     = _readOnlyOwner.Value.GetEquipSkillID( i );
                    var skillData   = SkillsData.data[( int ) skillID];
                    totalCost       += skillData.Cost;
                    _readOnlyOwner.Value.BattleParams.RemoveSkill( skillID, _readOnlyOwner.Value.GetStatusRef );
                }
            }

            _readOnlyOwner.Value.BattleLogic.RemoveBuffEffect();
            _readOnlyOwner.Value.GetStatusRef.CurActionGauge += totalCost;
        }

        /// <summary>
        /// プレイヤーは移動の有無を問わず、攻撃またはスキルを実行した時点で行動終了とみなします。
        /// (コマンド選択で移動をせずに攻撃・スキルを選んだ場合でも、その場で行動を終了させるため)
        /// </summary>
        protected override void UpdateActionEndState()
        {
            var endCommand = _readOnlyOwner.Value.BattleParams.TmpParam.IsEndCommand;
            if( endCommand[( int ) COMMAND_TAG.ATTACK] || endCommand[( int ) COMMAND_TAG.SKILL] )
            {
                // 攻撃またはスキル実行済み(移動の有無を問わない) → グレー化して行動不可
                BeImpossibleAction();
            }
            else if( _readOnlyOwner.Value.BattleParams.TmpParam.IsSkillQueued )
            {
                // スキルをキュー積み済み(移動の有無を問わない) → グレー化せず待機フラグのみ立てて行動不可
                _readOnlyOwner.Value.BattleParams.TmpParam.SetEndCommandStatus( COMMAND_TAG.WAIT, true );
            }
        }

        public bool IsContainsCommandHistory( COMMAND_TAG commandTag )
        {
            return _commandHistory.Contains( commandTag );
        }

        public int GetCommandHistoryCount()
        {
            return _commandHistory.Count;
        }

        /// <summary>
        /// 指定のスキルの使用設定を切り替えます
        /// </summary>
        /// <param name="index">指定のスキルのインデックス番号</param>
        public override void ToggleEquipSkill( int index )
        {
            var owner = _readOnlyOwner.Value;
            bool IsToggledToUse = owner.BattleParams.TmpParam.IsSkillsToggledON[index] = !owner.BattleParams.TmpParam.IsSkillsToggledON[index];

            SkillID skillID = owner.GetEquipSkillID( index );
            if( !SkillsData.IsValidSkill( skillID ) ) { return; }
            var skillData = SkillsData.data[( int ) skillID];

            if( IsToggledToUse )
            {
                owner.BattleParams.ApplySkill( skillID, owner.GetStatusRef );
            }
            else
            {
                owner.BattleParams.RemoveSkill( skillID, owner.GetStatusRef );
            }
        }
    }
}
