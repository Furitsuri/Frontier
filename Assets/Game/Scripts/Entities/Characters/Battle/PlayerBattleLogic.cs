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
                RevertBeforeMoving,         // COMMAND_TAG.MOVE
                null,                       // COMMAND_TAG.ATTACK
                RevertExecutedSkills,       // COMMAND_TAG.SKILL
                null,                       // COMMAND_TAG.WAIT
            };

            _baseAi.Init( _readOnlyOwner.Value );
        }

        /// <summary>
        /// 現在の移動前情報を適応します
        /// </summary>
        public void HoldBeforeMoveInfo()
        {
            _prevMoveInfo.tmpParam  = _readOnlyOwner.Value.BattleParams.TmpParam.Clone();
            _prevMoveInfo.rotDir    = _readonlyTransform.Value.GetRotation();
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

            _stageCtrl.TileDataHdlr().UpdateTileDynamicDatas();
            _stageCtrl.ApplyCurrentGrid2CharacterTile( _readOnlyOwner.Value );
        }

        /// <summary>
        /// 実行済みのスキルを全て取り消します
        /// </summary>
        public void RevertExecutedSkills()
        {
            int totalCost = 0;

            for( int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                if( IsUsingEquipSkill( i ) )
                {
                    var skillData = SkillsData.data[( int ) _readOnlyOwner.Value.GetEquipSkillID( i )];
                    totalCost += skillData.Cost;

                    ToggleUseSkill( i );
                }
            }

            _readOnlyOwner.Value.BattleLogic.RemoveBuffEffect();
            _readOnlyOwner.Value.GetStatusRef.ResetConsumptionActionGauge();
            _readOnlyOwner.Value.GetStatusRef.CurActionGauge += totalCost;
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
        public override void ToggleUseSkill( int index )
        {
            bool IsToggledToUse = _readOnlyOwner.Value.BattleParams.TmpParam.IsSkillsToggledON[index] = !_readOnlyOwner.Value.BattleParams.TmpParam.IsSkillsToggledON[index];

            SkillID skillID = _readOnlyOwner.Value.GetEquipSkillID( index );
            if( !SkillsData.IsValidSkill( skillID ) ) { return; }
            var skillData = SkillsData.data[( int ) skillID];

            if( IsToggledToUse )
            {
                _readOnlyOwner.Value.GetStatusRef.ActGaugeConsumption                   += skillData.Cost;
                _readOnlyOwner.Value.BattleParams.SkillModifiedParam.AtkNum             += skillData.AddAtkNum;
                _readOnlyOwner.Value.BattleParams.SkillModifiedParam.AtkMagnification   += skillData.AddAtkMag;
                _readOnlyOwner.Value.BattleParams.SkillModifiedParam.DefMagnification   += skillData.AddDefMag;
            }
            else
            {
                _readOnlyOwner.Value.GetStatusRef.ActGaugeConsumption                   -= skillData.Cost;
                _readOnlyOwner.Value.BattleParams.SkillModifiedParam.AtkNum             -= skillData.AddAtkNum;
                _readOnlyOwner.Value.BattleParams.SkillModifiedParam.AtkMagnification   -= skillData.AddAtkMag;
                _readOnlyOwner.Value.BattleParams.SkillModifiedParam.DefMagnification   -= skillData.AddDefMag;
            }
        }
    }
}
