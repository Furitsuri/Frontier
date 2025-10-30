using Frontier.Entities;
using Frontier.Battle;
using System.Linq;

namespace Frontier.StateMachine
{
    public class OtherPhaseHandler : PhaseHandlerBase
    {
        /// <summary>
        /// 初期化を行います
        /// </summary>
        override public void Init()
        {
            // 目標座標や攻撃対象をリセット
            foreach( Other other in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.OTHER ) )
            {
                other.GetAi().ResetDestinationAndTarget();
            }
            // MEMO : 上記リセット後に初期化する必要があるためにこの位置であることに注意
            base.Init();
            // 選択グリッドを(1番目の)キャラクターのグリッド位置に合わせる
            if( 0 < _btlRtnCtrl.BtlCharaCdr.GetCharacterCount( CHARACTER_TAG.OTHER ) && _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.OTHER ) != null )
            {
                Character other = _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.OTHER ).First();
                _stgCtrl.ApplyCurrentGrid2CharacterTile( other );
            }
            // アクションゲージの回復
            _btlRtnCtrl.BtlCharaCdr.RecoveryActionGaugeForGroup( CHARACTER_TAG.OTHER );

            // フェーズアニメーションの開始
            StartPhaseAnim();
        }

        /// <summary>
        /// フェーズアニメーションを再生します
        /// </summary>
        override protected void StartPhaseAnim()
        {
            _btlUi.TogglePhaseUI( true, TurnType.OTHER_TURN );
            _btlUi.StartAnimPhaseUI();
        }
    }
}