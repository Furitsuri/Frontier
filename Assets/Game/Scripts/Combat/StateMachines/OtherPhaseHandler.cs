using Frontier.Entities;
using System.Linq;

namespace Frontier.Battle
{
    public class OtherPhaseHandler : TroopPhaseHandler
    {
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

        public override void Update()
        {
            base.Update();

            _presenter.Update();
        }
    }
}