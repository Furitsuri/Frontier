using Frontier.UI;
using Zenject;

namespace Frontier.StateMachine
{
    public class PhaseAnimationStateBase : PhaseStateBase
    {
        private enum TRANSIT_TAG
        {
            NONE = -1,
            SELECT_TILE,

            NUM,
        }

        protected BattleUISystem _btlUi = null;

        /// <summary>
        /// フェーズアニメーションを再生します
        /// </summary>
        virtual protected void StartPhaseAnim()
        {
            _btlUi.StartAnimPhaseUI();
        }

        public override void Init()
        {
            _btlUi = _uiSystem.BattleUi;

            StartPhaseAnim();   // フェーズアニメーションの開始
        }

        public override bool Update()
        {
            // フェーズアニメーション中は操作無効
            if( _btlUi.IsPlayingPhaseUI() )
            {
                return false;
            }

            // フェーズアニメーションが終了すれば自動的にSelectTileへ遷移
            TransitStateWithExit( ( int ) TRANSIT_TAG.SELECT_TILE );

            return base.Update();
        }
    }
}
