using Frontier.Entities.Ai;
using Frontier.Stage;
using System;
using System.Collections.Generic;

namespace Frontier.Entities
{
    public class Enemy : Npc
    {
        /// <summary>
        /// 目標座標と標的キャラクターを取得します
        /// </summary>
        public void FetchDestinationAndTarget(out int destinationIndex, out Character targetCharacter)
        {
            destinationIndex    = _baseAi.GetDestinationGridIndex();
            targetCharacter     = _baseAi.GetTargetCharacter();
        }

        /// <summary>
        /// 目的座標と標的キャラクターを決定する
        /// </summary>
        public (bool, bool) DetermineDestinationAndTargetWithAI()
        {
            return _baseAi.DetermineDestinationAndTarget( in _params, in _tileCostTable, CharaKey );
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        override public void Init()
        {
            base.Init();
        }

        /// <summary>
        /// 思考タイプを設定します
        /// </summary>
        /// <param name="type">設定する思考タイプ</param>
        override public void SetThinkType( ThinkingType type )
        {
            _thikType = type;

            // 思考タイプによってemAIに代入する派生クラスを変更する
            Func<BaseAi>[] emAiFactorys = new Func<BaseAi>[(int)ThinkingType.NUM]
            {
                () => _hierarchyBld.InstantiateWithDiContainer<AiBase>(false),        // BASE
                () => _hierarchyBld.InstantiateWithDiContainer<AiAggressive>(false),  // AGGRESSIVE
                () => _hierarchyBld.InstantiateWithDiContainer<AiWaiting>(false),     // WAITING
            };

            _baseAi = emAiFactorys[( int )_thikType]();
            _baseAi.Init( this );
        }

        override public void ToggleAttackableRangeDisplay()
        {
            _actionRangeCtrl.ToggleAttackableRangeDisplay( in TileColors.Colors[( int ) MeshType.ENEMIES_ATTACKABLE] );
        }
    }
}