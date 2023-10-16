using UnityEngine;
using static Frontier.SkillsData;
using static UnityEngine.GraphicsBuffer;

namespace Frontier
{
    public class Player : Character
    {
        private bool _isPrevMoving = false;

        /// <summary>
        /// 直前フレームで移動を行っていたかのフラグを取得します
        /// </summary>
        /// <returns>直前フレームでの移動実行フラグ</returns>
        public bool IsPrevMoving() { return _isPrevMoving; }

        /// <summary>
        /// プレイヤーキャラクターの移動時の更新処理を行います
        /// </summary>
        /// <param name="destination">移動目的座標</param>
        public void UpdateMove(int gridIndex, in Vector3 destination)
        {
            bool toggleAnimation = false;

            Vector3 dir = (destination - transform.position).normalized;
            Vector3 afterPos = transform.position + dir * Constants.CHARACTER_MOVE_SPEED * Time.deltaTime;
            Vector3 afterDir = (destination - afterPos).normalized;
            if (Vector3.Dot(dir, afterDir) <= 0)
            {
                transform.position = destination;

                if (_isPrevMoving) toggleAnimation = true;
                _isPrevMoving = false;
                tmpParam.gridIndex = gridIndex;
            }
            else
            {
                transform.position = afterPos;
                transform.rotation = Quaternion.LookRotation(dir);

                if (!_isPrevMoving) toggleAnimation = true;
                _isPrevMoving = true;
            }

            if (toggleAnimation) setAnimator(Character.ANIME_TAG.MOVE, _isPrevMoving);
        }

        override public void setAnimator(ANIME_TAG animTag)
        {
            _animator.SetTrigger(_animNames[(int)animTag]);
        }
        override public void setAnimator(ANIME_TAG animTag, bool b)
        {
            _animator.SetBool(_animNames[(int)animTag], b);
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