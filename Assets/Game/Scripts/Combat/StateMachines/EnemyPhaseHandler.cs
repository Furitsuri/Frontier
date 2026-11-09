using Frontier.Entities;
using Frontier.Entities.Ai;
using Frontier.StateMachine;
using System.Linq;
using Zenject;

namespace Frontier.Battle
{
    public class EnemyPhaseHandler : TroopPhaseHandler
    {
        [Inject] private TroopActionCoordinator _coordinator = null;

        [Inject]
        public EnemyPhaseHandler( HierarchyBuilderBase hierarchyBld ) : base( hierarchyBld )
        {
        }

        public override void Init()
        {
            // 目標座標や攻撃対象をリセット
            foreach( Enemy enemy in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.ENEMY ) )
            {
                enemy.BattleLogic.GetAi().ResetDestinationAndTarget();
            }
            // 待ち構えている(INTERCEPT型の)敵のうち、行動範囲に攻撃対象が入ってきた集団に行動を開始させる
            // MEMO : 各敵の手番毎に判定すると、先に手番を終えた構成員がそのターン行動出来なくなるため、フェーズ開始時にまとめて判定する
            _btlRtnCtrl.BtlCharaCdr.InterceptGroupReg.ActivateGroupsDetectingOpponent( CHARACTER_TAG.ENEMY );
            // MEMO : 上記リセット後に初期化する必要があるためにこの位置であることに注意
            base.Init();
            // ターン開始時の自軍キャラへの処理
            _btlRtnCtrl.BtlCharaCdr.ApplyTurnStartProccessingForGroup( CHARACTER_TAG.ENEMY );

            // 選択グリッドを、このフェーズで最初に行動する敵(軍全体の調整役が最初に選ぶ敵。行動する敵がいない場合は1番目の敵)のグリッド位置に合わせ、
            // カメラの注視対象もグリッドカーソルへ切り替える(前のフェーズの最後の行動で、注視対象がターゲットカーソルのまま終わっている場合があるため)
            // MEMO : 最初に行動する敵を求める際にAIが行動を決定するため、アクションゲージの回復等(上記のターン開始時の処理)より後に行うこと
            if( 0 < _btlRtnCtrl.BtlCharaCdr.GetCharacterCount( CHARACTER_TAG.ENEMY ) )
            {
                var enemies = _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.ENEMY );
                Character focusEnemy = EmSelectState.FindFirstActingEnemy( _coordinator, enemies ) ?? enemies.First();
                _stgCtrl.ApplyGridCursor2CharacterTileWithFocusCamera( focusEnemy );
                // フェーズアニメーション中はグリッドカーソルを表示しない(アニメーション後にEmSelectStateが行動する敵へ合わせて表示する)
                _stgCtrl.SetVisibleGridCursor( false );
            }

            AssignPresenterToNodes( RootNode, _presenter );
        }

        /// <summary>
        /// 遷移の木構造を作成します
        /// </summary>
        protected override void CreateTree()
        {
            // 遷移木の作成
            // MEMO : 別のファイル(XMLなど)から読み込んで作成出来るようにするのもアリ

            /*
             *  親子図 ( [n]は子ステートとしての登録順。EmSelectStateの子はCOMMAND_TAGの値と一致させること )
             *
             *      EmPhaseStateAnimation
             *          ｜
             *          └─ [0] EmSelectState
             *                  ｜
             *                  ├─ [0] EmMoveState   (COMMAND_TAG.MOVE)
             *                  ├─ [1] EmAttackState (COMMAND_TAG.ATTACK)
             *                  │       └─ [0] EmDefenceSkillSelectState (攻撃対象のプレイヤーが防御スキルを選択)
             *                  ├─ [2] EmSkillState  (COMMAND_TAG.SKILL)
             *                  └─ [3] EmWaitState   (COMMAND_TAG.WAIT)
             *
             */

            var selectState = _hierarchyBld.InstantiateWithDiContainer<EmSelectState>( false );

            RootNode = _hierarchyBld.InstantiateWithDiContainer<EmPhaseStateAnimation>( false );
            RootNode.AddChild( selectState );

            // 行動選択
            selectState.AddChild( _hierarchyBld.InstantiateWithDiContainer<EmMoveState>( false ) );
            var attackState = _hierarchyBld.InstantiateWithDiContainer<EmAttackState>( false );
            attackState.AddChild( _hierarchyBld.InstantiateWithDiContainer<EmDefenceSkillSelectState>( false ) );
            selectState.AddChild( attackState );
            selectState.AddChild( _hierarchyBld.InstantiateWithDiContainer<EmSkillState>( false ) );
            selectState.AddChild( _hierarchyBld.InstantiateWithDiContainer<EmWaitState>( false ) );
        }
    }
}