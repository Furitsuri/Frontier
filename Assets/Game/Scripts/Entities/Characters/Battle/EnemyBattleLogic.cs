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
        // AIが決定した目的地・攻撃対象がそれぞれ有効か(AIが決定済み(IsDetermined)の間のみ有効な値)
        private (bool isValidDestination, bool isValidTarget) _determinedResult = (false, false);

        /// <summary>
        /// 目的座標と標的キャラクターを決定する
        /// </summary>
        public (bool, bool) DetermineDestinationAndTargetWithAI()
        {
            return _baseAi.DetermineDestinationAndTarget( _readOnlyOwner.Value.BattleParams, in _readOnlyOwner.Value.GetStatusRef, in _tileCostTable, _readOnlyOwner.Value.GetCharacterKey() );
        }

        /// <summary>
        /// このフェーズでの行動(目的地・攻撃対象)をAIに決定させ、その結果を返します。
        /// AIが既に決定済みの場合は決定し直さず、決定した時点の結果を返します
        /// (フェーズ開始時に最初に行動する敵を求める際と、各敵の手番の際の双方から呼ばれても、結果が一致するようにするため)。
        /// 移動先が現在地(移動経路が空)の場合は、移動の必要が無いため移動済み状態にします
        /// (移動コマンドに遷移させず、移動・攻撃の両コマンドの終了状態による行動終了判定が成り立つようにするため)。
        /// </summary>
        /// <returns>目的地・攻撃対象がそれぞれ有効か</returns>
        public (bool isValidDestination, bool isValidTarget) PrepareActionWithAI()
        {
            if( !_baseAi.IsDetermined() )
            {
                _determinedResult = DetermineDestinationAndTargetWithAI();

                if( _actionRangeCtrl.MovePathHdlr.ProposedMovePath.Count <= 0 )
                {
                    _readOnlyOwner.Value.BattleParams.TmpParam.SetEndCommandStatus( COMMAND_TAG.MOVE, true );
                }
            }

            return _determinedResult;
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