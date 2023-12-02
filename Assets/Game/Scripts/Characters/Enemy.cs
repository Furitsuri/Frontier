using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class Enemy : Character
    {
        /// <summary>
        /// 思考タイプ
        /// </summary>
        public enum ThinkingType
        {
            AGGERESSIVE = 0,    // 積極的に移動し、攻撃後の結果の評価値が高い対象を狙う
            WAITING,            // 自身の行動範囲に対象が入ってこない限り動かない。動き始めた後はAGGRESSIVEと同じ動作

            NUM
        }

        private ThinkingType _thikType;
        public EMAIBase EmAI { get; private set; }

        public void SetThinkType(ThinkingType type)
        {
            _thikType = type;

            // 思考タイプによってemAIに代入する派生クラスを変更する
            switch (_thikType)
            {
                case ThinkingType.AGGERESSIVE:
                    EmAI = new EMAIAggressive();
                    break;
                case ThinkingType.WAITING:
                    // TODO : Waitタイプを作成次第追加
                    break;
                default:
                    EmAI = new EMAIBase();
                    break;
            }

            EmAI.Init(this, _btlMgr, _stageCtrl);
        }

        override public void setAnimator(AnimDatas.ANIME_CONDITIONS_TAG animTag)
        {
            _animator.SetTrigger(AnimDatas.ANIME_CONDITIONS_NAMES[(int)animTag]);
        }

        override public void setAnimator(AnimDatas.ANIME_CONDITIONS_TAG animTag, bool b)
        {
            _animator.SetBool(AnimDatas.ANIME_CONDITIONS_NAMES[(int)animTag], b);
        }

        /// <summary>
        /// 死亡処理。管理リストから削除し、ゲームオブジェクトを破棄します
        /// MEMO : モーションのイベントフラグから呼び出します
        /// </summary>
        public override void Die()
        {
            base.Die();

            _btlMgr.RemoveEnemyFromList(this);
        }

        /// <summary>
        /// 目的座標と標的キャラクターを決定する
        /// </summary>
        public (bool, bool) DetermineDestinationAndTargetWithAI()
        {
            return EmAI.DetermineDestinationAndTarget(param, tmpParam);
        }

        /// <summary>
        /// 目標座標と標的キャラクターを取得します
        /// </summary>
        public void FetchDestinationAndTarget(out int destinationIndex, out Character targetCharacter)
        {
            destinationIndex = EmAI.GetDestinationGridIndex();
            targetCharacter = EmAI.GetTargetCharacter();
        }
    }
}