using Frontier.Battle;
using Frontier.Entities;
using Frontier.Stage;
using System.Linq;
using Zenject;

namespace Frontier.StateMachine
{
    public class OtherPhaseHandler : PhaseHandlerBase
    {
        [Inject] protected BattleRoutineController _btlRtnCtrl = null;
        [Inject] protected StageController _stgCtrl = null;

        /// <summary>
        /// 初期化を行います
        /// </summary>
        public override void Init()
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
        }
    }
}