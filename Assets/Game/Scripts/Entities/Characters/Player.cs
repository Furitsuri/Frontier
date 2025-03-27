using Frontier.Combat;
using Frontier.Stage;
using UnityEngine;

namespace Frontier.Entities
{
    public class Player : Character
    {
        /// <summary>
        /// プレイヤーキャラクターが移動を開始する前の情報です
        /// 移動後に状態を巻き戻す際に使用します
        /// </summary>
        private struct PrevMoveInfo
        {
            public TmpParameter tmpParam;
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

        private bool _isPrevMoving = false;
        private Vector3 _movementDestination = Vector3.zero;
        private PrevMoveInfo _prevMoveInfo;

        /// <summary>
        /// プレイヤーキャラクターの移動時の更新処理を行います
        /// </summary>
        /// <param name="gridIndex">キャラクターの現在地となるグリッドのインデックス値</param>
        /// <param name="gridInfo">指定グリッドの情報</param>
        public void UpdateMove(int gridIndex, in GridInfo gridInfo)
        {
            bool toggleAnimation = false;

            // 移動可のグリッドに対してのみ目的地を更新(自身を除くキャラクターが存在するグリッドには移動させない)
            if ( 0 <= gridInfo.estimatedMoveRange && ( !gridInfo.IsExistCharacter() || gridInfo.IsMatchExistCharacter(this) ) )
            {
                _movementDestination    = gridInfo.charaStandPos;
                tmpParam.gridIndex      = gridIndex;
            }

            Vector3 dir         = (_movementDestination - transform.position).normalized;
            Vector3 afterPos    = transform.position + dir * Constants.CHARACTER_MOVE_SPEED * Time.deltaTime;
            Vector3 afterDir    = (_movementDestination - afterPos);
            afterDir.y          = 0f;
            afterDir            = afterDir.normalized;
            if ( Vector3.Dot(dir, afterDir) <= 0 )
            {
                transform.position = _movementDestination;

                if (_isPrevMoving) toggleAnimation = true;
                _isPrevMoving = false;
            }
            else
            {
                transform.position = afterPos;
                transform.rotation = Quaternion.LookRotation(dir);

                if (!_isPrevMoving) toggleAnimation = true;
                _isPrevMoving = true;
            }

            if (toggleAnimation) AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.MOVE, _isPrevMoving);
        }

        /// <summary>
        /// 現在の移動前情報を適応します
        /// </summary>
        public void AdaptPrevMoveInfo()
        {
            _prevMoveInfo.tmpParam  = tmpParam.Clone();
            _prevMoveInfo.rotDir    = transform.rotation;
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
            tmpParam = _prevMoveInfo.tmpParam;
            SetPosition( tmpParam.gridIndex, _prevMoveInfo.rotDir );
            // グリッド情報を更新
            _stageCtrl.UpdateGridInfo();
        }

        /// <summary>
        /// 移動入力受付の可否判定を行います
        /// TODO : 必要がなくなった可能性があるため、不必要と確信出来れば削除
        /// </summary>
        /// <returns>移動入力の受付可否</returns>
        public bool IsAcceptableMovementOperation(float gridSize)
        {
            if (_isPrevMoving)
            {
                var diff = _movementDestination - transform.position;
                diff.y = 0;
                if (diff.sqrMagnitude <= Mathf.Pow(gridSize * Constants.ACCEPTABLE_INPUT_GRID_SIZE_RATIO, 2f)) return true;

                return false;
            }

            return true;
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
            for( int i = 0; i < (int)Command.COMMAND_TAG.NUM; ++i )
            {
                if( i == (int)Command.COMMAND_TAG.MOVE )
                {
                    if (!IsEndCommand(Command.COMMAND_TAG.MOVE))
                    {
                        isPossible = false;
                        break;
                    }
                }
                else
                {
                    if (IsEndCommand((Command.COMMAND_TAG)i))
                    {
                        isPossible = false;
                        break;
                    }
                }
            }

            return isPossible;
        }

        /// <summary>
        /// 使用スキルを選択します
        /// </summary>
        /// <param name="type">攻撃、防御、常駐などのスキルタイプ</param>
        override public void SelectUseSkills(SkillsData.SituationType type)
        {
            KeyCode[] keyCodes = new KeyCode[Constants.EQUIPABLE_SKILL_MAX_NUM] 
            {
#if UNITY_EDITOR
                KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L
#elif UNITY_STANDALONE_WIN

#else
#endif
            };

            for (int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i)
            {
                if (!param.IsValidSkill(i)) continue;

                // 指定されたタイプ以外のスキルは無視する
                var skillType = SkillsData.data[(int)param.equipSkills[i]].Type;
                _uiSystem.BattleUi.GetPlayerParamSkillBox(i).SetUseable(skillType == type);
                if (skillType != type)
                {
                    continue;
                }

                // キーが押下されたら、キーに対応するスキルの使用状態を切り替える
                if (Input.GetKeyUp(keyCodes[i]))
                {
                    tmpParam.isUseSkills[i] = !tmpParam.isUseSkills[i];

                    int skillID = (int)param.equipSkills[i];
                    var skillData = SkillsData.data[skillID];

                    if (tmpParam.isUseSkills[i])
                    {
                        // コストが現在のアクションゲージ値を越えている場合は無視
                        if (param.curActionGauge < param.consumptionActionGauge + skillData.Cost)
                        {
                            tmpParam.isUseSkills[i] = false;
                            continue;
                        }

                        param.consumptionActionGauge += skillData.Cost;

                        skillModifiedParam.AtkNum += skillData.AddAtkNum;
                        skillModifiedParam.AtkMagnification += skillData.AddAtkMag;
                        skillModifiedParam.DefMagnification += skillData.AddDefMag;
                    }
                    else
                    {
                        param.consumptionActionGauge -= skillData.Cost;

                        skillModifiedParam.AtkNum -= skillData.AddAtkNum;
                        skillModifiedParam.AtkMagnification -= skillData.AddAtkMag;
                        skillModifiedParam.DefMagnification -= skillData.AddDefMag;
                    }

                    _uiSystem.BattleUi.GetPlayerParamSkillBox(i).SetFlickEnabled(tmpParam.isUseSkills[i]);
                }
            }
        }

        /// <summary>
        /// 指定のスキルの使用設定を切り替えます
        /// </summary>
        /// <param name="index">指定のスキルのインデックス番号</param>
        /// <returns>切替の有無</returns>
        override public bool ToggleUseSkillks(int index)
        {
            tmpParam.isUseSkills[index] = !tmpParam.isUseSkills[index];

            int skillID = (int)param.equipSkills[index];
            var skillData = SkillsData.data[skillID];

            if (tmpParam.isUseSkills[index])
            {
                param.consumptionActionGauge += skillData.Cost;
                skillModifiedParam.AtkNum += skillData.AddAtkNum;
                skillModifiedParam.AtkMagnification += skillData.AddAtkMag;
                skillModifiedParam.DefMagnification += skillData.AddDefMag;
            }
            else
            {
                param.consumptionActionGauge -= skillData.Cost;
                skillModifiedParam.AtkNum -= skillData.AddAtkNum;
                skillModifiedParam.AtkMagnification -= skillData.AddAtkMag;
                skillModifiedParam.DefMagnification -= skillData.AddDefMag;
            }

            _uiSystem.BattleUi.GetPlayerParamSkillBox(index).SetFlickEnabled(tmpParam.isUseSkills[index]);

            return true;
        }
    }
}