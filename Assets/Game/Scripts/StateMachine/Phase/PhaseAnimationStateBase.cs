using Frontier.Battle;
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

        [Inject] protected BattleUISystem _btlUi = null;

        /// <summary>
        /// フェーズアニメーションを再生します
        /// </summary>
        virtual protected void StartPhaseAnim()
        {
            _btlUi.StartAnimPhaseUI();
        }

        override public void Init()
        {
            StartPhaseAnim();   // フェーズアニメーションの開始
        }

        override public bool Update()
        {
            // フェーズアニメーション中は操作無効
            if( _btlUi.IsPlayingPhaseUI() )
            {
                return false;
            }

            // フェーズアニメーションが終了すれば自動的にPlSelectTileへ遷移
            TransitIndex = ( int ) TRANSIT_TAG.SELECT_TILE;

            return base.Update();
        }
    }
}
