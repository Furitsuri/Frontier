using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Entities
{
    public class Enemy : Character
    {
        /// <summary>
        /// 初期化します
        /// </summary>
        public override void Init()
        {
            base.Init();
        }

        /// <summary>
        /// 死亡処理。管理リストから削除し、ゲームオブジェクトを破棄します
        /// MEMO : モーションのイベントフラグから呼び出します
        /// </summary>
        override public void Die()
        {
            base.Die();

            _btlRtnCtrl.BtlCharaCdr.RemoveCharacterFromList(this);
        }

        /// <summary>
        /// 思考タイプを設定します
        /// </summary>
        /// <param name="type">設定する思考タイプ</param>
        override public void SetThinkType(ThinkingType type)
        {
            _thikType = type;

            // 思考タイプによってemAIに代入する派生クラスを変更する
            switch (_thikType)
            {
                case ThinkingType.AGGERESSIVE:
                    _baseAI = _hierarchyBld.InstantiateWithDiContainer<EmAiAggressive>();
                    break;
                case ThinkingType.WAITING:
                    _baseAI = _hierarchyBld.InstantiateWithDiContainer<EmAiWaitting>();
                    break;
                default:
                    _baseAI = _hierarchyBld.InstantiateWithDiContainer<EmAiBase>();
                    break;
            }

            _baseAI.Init();
        }

        /// <summary>
        /// 目的座標と標的キャラクターを決定する
        /// </summary>
        public (bool, bool) DetermineDestinationAndTargetWithAI()
        {
            return _baseAI.DetermineDestinationAndTarget(param, tmpParam);
        }

        /// <summary>
        /// 目標座標と標的キャラクターを取得します
        /// </summary>
        public void FetchDestinationAndTarget(out int destinationIndex, out Character targetCharacter)
        {
            destinationIndex    = _baseAI.GetDestinationGridIndex();
            targetCharacter     = _baseAI.GetTargetCharacter();
        }
    }
}