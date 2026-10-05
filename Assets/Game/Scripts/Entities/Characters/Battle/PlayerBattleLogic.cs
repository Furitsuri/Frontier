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
        // 移動(単体移動・グループ移動)によって暫定的に移動している(移動が確定していない)状態か。
        // 移動中に直接攻撃した場合など、移動の履歴があっても暫定扱いにしないケースと区別するため、コマンド履歴とは別に明示的に保持する
        private bool _isMoveProvisional = false;
        // 移動先の変更(暫定移動中の移動のやり直し)を開始した時点の位置と向き。変更をキャンセルした際にここへ戻す。
        // 移動先の変更を開始する度に上書きされる(2度目の変更をキャンセルした場合は、2度目を開始する前の位置へ戻る)
        private int _repositionStartTileIndex       = -1;
        private UnityEngine.Quaternion _repositionStartRot = UnityEngine.Quaternion.identity;

        public ref PrevMoveInfo PrevMoveInformaiton => ref _prevMoveInfo;
        public bool IsMoveProvisional => _isMoveProvisional;

        public override void Init()
        {
            base.Init();

            _paramWinType           = ParameterWindowType.Left;
            _isMoveProvisional = false;

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
            _prevMoveInfo.movedPath = null;     // 経路を保持する場合は、呼び出し側で別途HoldMovedPathを呼び出す
        }

        /// <summary>
        /// 移動先の変更を開始する時点の位置と向きを保持します。
        /// 通常の移動開始時のHoldBeforeMoveInfoと異なり、移動前(最初に移動を開始した地点)の情報は上書きしません
        /// (移動先の変更では、起点と移動範囲を最初の地点のままとするため)。
        /// </summary>
        public void HoldRepositionStartInfo()
        {
            _repositionStartTileIndex   = _readOnlyOwner.Value.BattleParams.TmpParam.CurrentTileIndex;
            _repositionStartRot         = _readOnlyOwner.Value.GetRotation();
        }

        /// <summary>
        /// 移動先の変更を取り消し、変更を開始する前の位置へ即座に戻します(暫定移動の状態は維持されます)
        /// </summary>
        public void RevertToRepositionStart()
        {
            ForcedStopMoving();
            SetPositionOnStage( _repositionStartTileIndex, _repositionStartRot );
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
            // 履歴が無くなる=移動前へ巻き戻せなくなるため、暫定移動の状態も解除する
            _isMoveProvisional = false;
        }

        /// <summary>
        /// 移動(単体移動・グループ移動)によって暫定的に移動した状態として記録します。
        /// 移動コマンドをコマンド履歴へ積んだ後に呼び出してください。
        /// 解除は、移動の確定(ClearCommandHistory/FinalizeCommand)または巻き戻し(RevertBeforeMoving)の際に自動で行われます。
        /// </summary>
        public void MarkMoveProvisional()
        {
            _isMoveProvisional = true;
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
            _isMoveProvisional = false;    // 移動前へ戻るため、暫定移動の状態を解除する
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
