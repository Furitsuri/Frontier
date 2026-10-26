using Frontier.Battle;
using Frontier.UI;

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

        private BattleRoutinePresenter _presenter = null;

        // MEMO : AssignPresenter()の時点では子Presenterが未生成の場合があるため、参照の都度取得します
        protected PhaseAnimationPresenter PhaseAnimPresenter => _presenter.PhaseAnimationView;

        public override void AssignPresenter( PhasePresenterBase presenter )
        {
            _presenter = presenter as BattleRoutinePresenter;
        }

        /// <summary>
        /// フェーズアニメーションを再生します
        /// </summary>
        virtual protected void StartPhaseAnim()
        {
            PhaseAnimPresenter.StartPhaseAnim();
        }

        public override void Init( object context )
        {
            base.Init( context );   // _transitIndex を -1 にリセット

            StartPhaseAnim();   // フェーズアニメーションの開始
        }

        public override bool Update()
        {
            // フェーズアニメーション中は操作無効
            if( PhaseAnimPresenter.IsPlayingPhaseAnim() )
            {
                return false;
            }

            // フェーズアニメーションが終了すれば自動的にSelectTileへ遷移
            TransitStateWithExit( ( int ) TRANSIT_TAG.SELECT_TILE );

            return base.Update();
        }
    }
}
