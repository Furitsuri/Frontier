using Frontier.Combat;
using Frontier.Entities.Ai;
using Frontier.Stage;
using Frontier.UI;
using System;
using static Constants;

namespace Frontier.Entities
{
    public class EnemyBattleLogic : BattleLogicBase
    {
        /// <summary>
        /// 目的座標と標的キャラクターを決定する
        /// </summary>
        public (bool, bool) DetermineDestinationAndTargetWithAI()
        {
            return _baseAi.DetermineDestinationAndTarget( _readOnlyOwner.Value.BattleParams, in _readOnlyOwner.Value.GetStatusRef, in _tileCostTable, _readOnlyOwner.Value.GetCharacterKey() );
        }

        public override void Init()
        {
            base.Init();

            _paramWinType = ParameterWindowType.Right;
        }

        /// <summary>
        /// 思考タイプを設定します
        /// </summary>
        /// <param name="setting">設定する思考タイプ、及び思考タイプ毎の設定値</param>
        public override void SetThinkType( in ThinkingSetting setting )
        {
            _thikType = setting.Type;

            // 思考タイプによってemAIに代入する派生クラスを変更する
            Func<BaseAi>[] emAiFactorys = new Func<BaseAi>[( int ) ThinkingType.NUM]
            {
                () => _hierarchyBld.InstantiateWithDiContainer<AiBase>(false),        // BASE
                () => _hierarchyBld.InstantiateWithDiContainer<AiAggressive>(false),  // AGGRESSIVE
                () => _hierarchyBld.InstantiateWithDiContainer<AiIntercept>(false),   // INTERCEPT
            };

            _baseAi = emAiFactorys[( int ) _thikType]();
            _baseAi.ApplyThinkingSetting( in setting );   // MEMO : Init内で設定値を参照するAIがあるため、Initより前に呼ぶこと
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