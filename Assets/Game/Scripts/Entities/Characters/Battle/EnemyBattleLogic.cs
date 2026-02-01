using Frontier.Combat.Skill;
using Frontier.Entities.Ai;
using Frontier.Stage;
using System;

namespace Frontier.Entities
{
    public class EnemyBattleLogic : BattleLogicBase
    {
        /// <summary>
        /// 目的座標と標的キャラクターを決定する
        /// </summary>
        public (bool, bool) DetermineDestinationAndTargetWithAI()
        {
            return _baseAi.DetermineDestinationAndTarget( in _readOnlyOwner.Value.RefBattleParams, in _readOnlyOwner.Value.GetStatusRef, in _tileCostTable, _readOnlyOwner.Value.CharaKey );
        }

        /// <summary>
        /// 思考タイプを設定します
        /// </summary>
        /// <param name="type">設定する思考タイプ</param>
        public override void SetThinkType( ThinkingType type )
        {
            _thikType = type;

            // 思考タイプによってemAIに代入する派生クラスを変更する
            Func<BaseAi>[] emAiFactorys = new Func<BaseAi>[( int ) ThinkingType.NUM]
            {
                () => _hierarchyBld.InstantiateWithDiContainer<AiBase>(false),        // BASE
                () => _hierarchyBld.InstantiateWithDiContainer<AiAggressive>(false),  // AGGRESSIVE
                () => _hierarchyBld.InstantiateWithDiContainer<AiWaiting>(false),     // WAITING
            };

            _baseAi = emAiFactorys[( int ) _thikType]();
            _baseAi.Init( _readOnlyOwner.Value );
        }

        public override void ToggleDisplayDangerRange()
        {
            _actionRangeCtrl.ToggleDisplayDangerRange( in TileColors.Colors[( int ) MeshType.ENEMIES_ATTACKABLE] );
        }

        public override void SetDisplayDangerRange( bool isShow )
        {
            _actionRangeCtrl.SetDisplayDangerRange( isShow, in TileColors.Colors[( int ) MeshType.ENEMIES_ATTACKABLE] );
        }

        /// <summary>
        /// 使用スキルを選択します
        /// </summary>
        /// <param name="type">攻撃、防御、常駐などのスキルタイプ</param>
        public override void SelectUseSkills( SituationType type )
        {

        }
    }
}