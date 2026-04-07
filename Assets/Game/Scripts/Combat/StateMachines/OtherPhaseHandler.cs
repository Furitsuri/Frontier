using Frontier.Entities;
using System.Linq;
using Zenject;

namespace Frontier.Battle
{
    public class OtherPhaseHandler : TroopPhaseHandler
    {
        [Inject]
        public OtherPhaseHandler( HierarchyBuilderBase hierarchyBld ) : base( hierarchyBld )
        {
        }

        public override void Init()
        {
            // 目標座標や攻撃対象をリセット
            foreach( Other other in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.OTHER ) )
            {
                other.BattleLogic.GetAi().ResetDestinationAndTarget();
            }
            // MEMO : 上記リセット後に初期化する必要があるためにこの位置であることに注意
            base.Init();
            // 選択グリッドを(1番目の)キャラクターのグリッド位置に合わせる
            if( 0 < _btlRtnCtrl.BtlCharaCdr.GetCharacterCount( CHARACTER_TAG.OTHER ) && _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.OTHER ) != null )
            {
                Character other = _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.OTHER ).First();
                _stgCtrl.ApplyCurrentGrid2CharacterTile( other );
            }
            // ターン開始時の自軍キャラへの処理
            _btlRtnCtrl.BtlCharaCdr.ApplyTurnStartProccessingForGroup( CHARACTER_TAG.OTHER );

            AssignPresenterToNodes( RootNode, _presenter );
        }

        public override void Update()
        {
            base.Update();

            _presenter.Update();
        }
    }
}