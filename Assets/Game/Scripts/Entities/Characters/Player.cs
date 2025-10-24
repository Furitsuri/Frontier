using Froniter.Entities;
using Frontier.Combat;
using Frontier.Combat.Skill;
using Frontier.Entities.Ai;
using Frontier.Stage;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Constants;
using static UnityEngine.UI.GridLayoutGroup;

namespace Frontier.Entities
{
    public class Player : Character
    {
        /// <summary>
        /// プレイヤーキャラクターが移動を開始する前の情報です
        /// 移動後に状態を巻き戻す際に使用します
        /// </summary>
        public struct PrevMoveInfo
        {
            public TemporaryParameter tmpParam;
            public Quaternion rotDir;

            /// <summary>
            /// 情報をリセットします
            /// </summary>
            public void Reset()
            {
                tmpParam.Reset();
                rotDir = Quaternion.identity;
            }
        }

        // private bool _isPrevMoving = false;
        private PrevMoveInfo _prevMoveInfo;
        public ref PrevMoveInfo PrevMoveInformaiton => ref _prevMoveInfo;

        /// <summary>
        /// 現在の移動前情報を適応します
        /// </summary>
        public void HoldBeforeMoveInfo()
        {
            _prevMoveInfo.tmpParam  = _params.TmpParam.Clone();
            _prevMoveInfo.rotDir    = _transformHdlr.GetRotation();
        }

        /// <summary>
        /// 移動前情報をリセットします
        /// </summary>
        public void ResetPrevMoveInfo()
        {
            _prevMoveInfo.Reset();
        }

        /// <summary>
        /// コマンドの可否や位置を以前の状態に巻き戻します
        /// </summary>
        public void RewindToPreviousState()
        {
            _params.TmpParam = _prevMoveInfo.tmpParam;
            SetPosition( _params.TmpParam.gridIndex, _prevMoveInfo.rotDir );
        }

        /// <summary>
        /// 移動後などに直前のコマンド状態に戻れるかどうかを取得します
        /// </summary>
        /// <returns>直前のコマンドに戻れるか否か</returns>
        public bool IsRewindStatePossible()
        {
            // 移動コマンドだけが終了している場合のみ直前の状態に戻れるように
            // MEMO : コマンドが今後増えても問題ないようにfor文で判定しています
            bool isPossible = true;
            for( int i = 0; i < (int)COMMAND_TAG.NUM; ++i )
            {
                if( i == (int)COMMAND_TAG.MOVE )
                {
                    if (!_params.TmpParam.IsEndCommand(COMMAND_TAG.MOVE))
                    {
                        isPossible = false;
                        break;
                    }
                }
                else
                {
                    if (_params.TmpParam.IsEndCommand((COMMAND_TAG)i))
                    {
                        isPossible = false;
                        break;
                    }
                }
            }

            return isPossible;
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        override public void Init()
        {
            base.Init();
            _baseAi = _hierarchyBld.InstantiateWithDiContainer<AiBase>( false );
            NullCheck.AssertNotNull( _baseAi , nameof( _baseAi ) );
            _baseAi.Init( this );
        }

        /// <summary>
        /// 指定のスキルの使用設定を切り替えます
        /// </summary>
        /// <param name="index">指定のスキルのインデックス番号</param>
        /// <returns>切替の有無</returns>
        override public bool ToggleUseSkillks(int index)
        {
            _params.TmpParam.isUseSkills[index] = !_params.TmpParam.isUseSkills[index];

            int skillID = (int)_params.CharacterParam.equipSkills[index];
            var skillData = SkillsData.data[skillID];

            if (_params.TmpParam.isUseSkills[index])
            {
                _params.CharacterParam.consumptionActionGauge += skillData.Cost;
                Params.SkillModifiedParam.AtkNum += skillData.AddAtkNum;
                Params.SkillModifiedParam.AtkMagnification += skillData.AddAtkMag;
                Params.SkillModifiedParam.DefMagnification += skillData.AddDefMag;
            }
            else
            {
                _params.CharacterParam.consumptionActionGauge -= skillData.Cost;
                Params.SkillModifiedParam.AtkNum -= skillData.AddAtkNum;
                Params.SkillModifiedParam.AtkMagnification -= skillData.AddAtkMag;
                Params.SkillModifiedParam.DefMagnification -= skillData.AddDefMag;
            }

            _btlRtnCtrl.BtlUi.GetPlayerParamSkillBox(index).SetFlickEnabled(_params.TmpParam.isUseSkills[index]);

            return true;
        }
    }
}