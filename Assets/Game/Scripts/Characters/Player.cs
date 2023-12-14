using Frontier.Stage;
using UnityEngine;
using static Frontier.SkillsData;
using static UnityEngine.GraphicsBuffer;

namespace Frontier
{
    public class Player : Character
    {
        private bool _isPrevMoving = false;
        private Vector3 _movementDestination = Vector3.zero;

        /// <summary>
        /// 移動入力受付の可否判定を行います
        /// </summary>
        /// <returns>移動入力の受付可否</returns>
        public bool IsAcceptableMovementOperation( float gridSize )
        {
            if( _isPrevMoving )
            {
                var diff = _movementDestination - transform.position;
                diff.y = 0;
                if(diff.sqrMagnitude <= Mathf.Pow( gridSize * Constants.ACCEPTABLE_INPUT_GRID_SIZE_RATIO, 2f ) ) return true;

                return false;
            }

            return true;
        }

        /// <summary>
        /// プレイヤーキャラクターの移動時の更新処理を行います
        /// </summary>
        /// <param name="gridIndex">キャラクターの現在地となるグリッドのインデックス値</param>
        /// <param name="gridInfo">指定グリッドの情報</param>
        public void UpdateMove(int gridIndex, in GridInfo gridInfo)
        {
            bool toggleAnimation = false;

            // 移動可のグリッドに対してのみ目的地を更新
            if (0 <= gridInfo.estimatedMoveRange)
            {
                _movementDestination = gridInfo.charaStandPos;
                tmpParam.gridIndex = gridIndex;
            }

            Vector3 dir = (_movementDestination - transform.position).normalized;
            Vector3 afterPos = transform.position + dir * Constants.CHARACTER_MOVE_SPEED * Time.deltaTime;
            Vector3 afterDir = (_movementDestination - afterPos);
            afterDir.y = 0f;
            afterDir = afterDir.normalized;
            if (Vector3.Dot(dir, afterDir) <= 0)
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

            if (toggleAnimation) AnimCtrl.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.MOVE, _isPrevMoving);
        }

        /// <summary>
        /// 死亡処理。管理リストから削除し、ゲームオブジェクトを破棄します
        /// モーションのイベントフラグから呼び出します
        /// </summary>
        override public void Die()
        {
            base.Die();

            _btlMgr.RemovePlayerFromList(this);
        }

        /// <summary>
        /// 使用スキルを選択します
        /// </summary>
        /// <param name="type">攻撃、防御、常駐などのスキルタイプ</param>
        override public void SelectUseSkills(SituationType type)
        {
            KeyCode[] keyCodes = new KeyCode[Constants.EQUIPABLE_SKILL_MAX_NUM] { KeyCode.H, KeyCode.J, KeyCode.K, KeyCode.L };

            for (int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i)
            {
                if (!param.IsValidSkill(i)) continue;

                var skillType = SkillsData.data[(int)param.equipSkills[i]].Type;
                BattleUISystem.Instance.GetPlayerParamSkillBox(i).SetUseable(skillType == type);
                if (skillType != type)
                {
                    continue;
                }

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

                    BattleUISystem.Instance.GetPlayerParamSkillBox(i).SetFlickEnabled(tmpParam.isUseSkills[i]);
                }
            }
        }
    }
}