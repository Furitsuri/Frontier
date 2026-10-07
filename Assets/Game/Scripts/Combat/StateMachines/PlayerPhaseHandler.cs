using Frontier.Entities;
using Frontier.StateMachine;
using System.Linq;
using Zenject;

namespace Frontier.Battle
{
    public class PlayerPhaseHandler : TroopPhaseHandler
    {
        [Inject] private SkillActionReservationQueue _reservationQueue = null;
        [Inject] private GroupMoveRegistrationList _groupMoveRegistrationList = null;
        [Inject] private ProvisionalMoveOriginDisplay _provisionalOriginDisplay = null;

        private PlConfirmReservedActionsState _confirmReservedActionsState = null;

        [Inject]
        public PlayerPhaseHandler( HierarchyBuilderBase hierarchyBld ) : base( hierarchyBld )
        {
        }

        public override void Init()
        {
            base.Init();

            if( 0 < _btlRtnCtrl.BtlCharaCdr.GetCharacterCount( CHARACTER_TAG.PLAYER ) )
            {
                // 選択グリッドを(1番目の)プレイヤーのグリッド位置に合わせる
                Character player = _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.PLAYER ).First();
                _stgCtrl.ApplyGridCursor2CharacterTile( player );
                // ターン開始時の自軍キャラへの処理
                _btlRtnCtrl.BtlCharaCdr.ApplyTurnStartProccessingForGroup( CHARACTER_TAG.PLAYER );
            }

            AssignPresenterToNodes( RootNode, _presenter );
        }

        /// <summary>
        /// 更新を行います
        /// </summary>
        public override void Update()
        {
            base.Update();

            _presenter.Update();
        }

        /// <summary>
        /// 後更新を行います。
        /// フェーズ終了時にキューが残っていれば確認ステートへ遷移します。
        /// </summary>
        public override bool LateUpdate()
        {
            bool phaseEnded = base.LateUpdate();

            if( phaseEnded && !_reservationQueue.IsEmpty && _confirmReservedActionsState != null )
            {
                CurrentNode = _confirmReservedActionsState;
                CurrentNode.OnEnter( null );
                return false;
            }

            return phaseEnded;
        }

        public override void Exit()
        {
            // プレイヤー以外の攻撃範囲表示をすべてクリア
            foreach( var npc in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.ENEMY, CHARACTER_TAG.OTHER ) )
            {
                npc.BattleLogic.ActionRangeCtrl.ActionableRangeRdr.ClearTileMeshesAllType();
            }

            // グループ移動の登録が残っていた場合、フェーズをまたいで半透明のまま残留しないよう後始末する
            if( !_groupMoveRegistrationList.IsEmpty )
            {
                foreach( var key in _groupMoveRegistrationList.GetAll() )
                {
                    _btlRtnCtrl.BtlCharaCdr.GetPlayer( key )?.RestoreMaterialsOriginalColor();
                }
                _groupMoveRegistrationList.Clear();
            }

            // ターン終了によって、暫定的に移動していたキャラクターの移動を確定させる
            // (コマンド履歴が残ったままだと、次のターンに前ターンの位置へ巻き戻せてしまうため)
            foreach( Player player in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.PLAYER ) )
            {
                player.ClearCommandHistory();
            }
            _provisionalOriginDisplay.Clear();

            base.Exit();
        }

        /// <summary>
        /// 遷移の木構造を作成します
        /// </summary>
        protected override void CreateTree()
        {
            // 遷移木の作成
            // MEMO : 別のファイル(XMLなど)から読み込んで作成出来るようにするのもアリ

            /*
             *  親子図 ( [n]は子ステートとしての登録順。各ステートのTransitTagの値と一致させること )
             *
             *      PlPhaseStateAnimation
             *          ｜
             *          ├─ [0] PlSelectTileState
             *          ｜       ｜
             *          ｜       ├─ [0] PlMoveState (プレイヤーキャラクターを決定すると直接遷移する、移動の操作)
             *          ｜       ｜       ｜
             *          ｜       ｜       ├─ [0] PlAttackOnMoveState (移動中に直接、攻撃へ遷移する)
             *          ｜       ｜       ｜       ├─ [0] CharacterStatusViewState
             *          ｜       ｜       ｜       └─ [1] PlConfirmKillReservedTargetState
             *          ｜       ｜       ├─ [1] CharacterStatusViewState
             *          ｜       ｜       ├─ [2] PlConfirmBlockUndoMoveState (移動先が、暫定移動中の他キャラクターの移動前の位置である場合の確認)
             *          ｜       ｜       └─ [3] PlSelectCommandState (移動先を決定すると開かれるコマンド選択。キャンセルすると移動の操作へ戻る)
             *          ｜       ｜               ｜
             *          ｜       ｜               ├─ [0] PlAttackState
             *          ｜       ｜               ｜       ├─ [0] CharacterStatusViewState
             *          ｜       ｜               ｜       └─ [1] PlConfirmKillReservedTargetState
             *          ｜       ｜               ├─ [1] PlSelectSkillState
             *          ｜       ｜               ｜       └─ [0] PlSkillActionToTargetState
             *          ｜       ｜               ｜               ├─ [0] CharacterStatusViewState
             *          ｜       ｜               ｜               ├─ [1] PlSkillUseOptionState
             *          ｜       ｜               ｜               ├─ [2] PlConfirmKillReservedTargetState
             *          ｜       ｜               ｜               └─ [3] PlConfirmBlockUndoMoveState (移動を伴うスキルの着地先に対する確認)
             *          ｜       ｜               └─ [2] PlWaitState
             *          ｜       ｜
             *          ｜       ├─ [1] CharacterStatusViewState
             *          ｜       ├─ [2] PlConfirmTurnEnd
             *          ｜       ├─ [3] PlSelectReservedActionState (予約に対する操作選択、実行まで行う)
             *          ｜       ├─ [4] PlSelectMenuState (OPT2から遷移するOption/Turn Endメニュー)
             *          ｜       ├─ [5] PlSelectGroupMembersState (OPT1でのキャラクター登録時に自動遷移するグループ移動のメンバー選択。登録・解除はここでのみ可能)
             *          ｜       ｜       └─ [0] PlGroupMoveState (CONFIRMで遷移するグループ移動プレビュー・実行。カーソル移動のたびにプレビューを更新する)
             *          ｜       ｜               └─ [0] PlConfirmBlockUndoMoveState
             *          ｜       ├─ [6] PlGroupMoveState (SUB3で全員を一括登録した際に直接遷移。上記と同一インスタンス)
             *          ｜       └─ [7] PlSelectCommandState (スキルの使用等によって移動が確定しているキャラクターを決定した場合に直接遷移。上記と同一インスタンス)
             *          ｜
             *          └─ [1] PlConfirmReservedActionsState (キュー実行確認)
             *
             */

            // MEMO : StackStateBaseはAddChildで戻り先(Parent)を上書きしないため、複数の箇所から遷移するステート
            //        (キャラクターステータス表示、グループ移動、コマンド選択、移動前の位置へ戻せなくなる旨の確認)は、単一インスタンスを全ての箇所で共有しています。

            var selectTileState             = _hierarchyBld.InstantiateWithDiContainer<PlSelectTileState>( false );
            var confirmReservedActionsState = _hierarchyBld.InstantiateWithDiContainer<PlConfirmReservedActionsState>( false );
            var characterStatusViewState    = _hierarchyBld.InstantiateWithDiContainer<CharacterStatusViewState>( false );
            var moveState                   = _hierarchyBld.InstantiateWithDiContainer<PlMoveState>( false );
            var attackOnMoveState           = _hierarchyBld.InstantiateWithDiContainer<PlAttackOnMoveState>( false );
            var selectCommandState          = _hierarchyBld.InstantiateWithDiContainer<PlSelectCommandState>( false );
            var attackState                 = _hierarchyBld.InstantiateWithDiContainer<PlAttackState>( false );
            var selectSkillState            = _hierarchyBld.InstantiateWithDiContainer<PlSelectSkillState>( false );
            var skillActionToTargetState    = _hierarchyBld.InstantiateWithDiContainer<PlSkillActionToTargetState>( false );
            var selectGroupMembersState     = _hierarchyBld.InstantiateWithDiContainer<PlSelectGroupMembersState>( false );
            var groupMoveState              = _hierarchyBld.InstantiateWithDiContainer<PlGroupMoveState>( false );
            var confirmBlockUndoMoveState   = _hierarchyBld.InstantiateWithDiContainer<PlConfirmBlockUndoMoveState>( false );

            RootNode = _hierarchyBld.InstantiateWithDiContainer<PlPhaseStateAnimation>( false );
            RootNode.AddChild( selectTileState );
            RootNode.AddChild( confirmReservedActionsState );
            _confirmReservedActionsState = confirmReservedActionsState;

            // タイル選択
            selectTileState.AddChild( moveState );
            selectTileState.AddChild( characterStatusViewState );
            selectTileState.AddChild( _hierarchyBld.InstantiateWithDiContainer<PlConfirmTurnEnd>( false ) );
            selectTileState.AddChild( _hierarchyBld.InstantiateWithDiContainer<PlSelectReservedActionState>( false ) );
            selectTileState.AddChild( _hierarchyBld.InstantiateWithDiContainer<PlSelectMenuState>( false ) );
            selectTileState.AddChild( selectGroupMembersState );
            selectTileState.AddChild( groupMoveState );
            selectTileState.AddChild( selectCommandState );

            // 移動の操作
            moveState.AddChild( attackOnMoveState );
            moveState.AddChild( characterStatusViewState );
            moveState.AddChild( confirmBlockUndoMoveState );
            moveState.AddChild( selectCommandState );

            // 移動中の直接攻撃
            // (TransitTag.CONFIRM_KILL_RESERVED_TARGET(=1、PlAttackState側と共通)とインデックスを揃えるため、CharacterStatusViewStateを先に追加する)
            attackOnMoveState.AddChild( characterStatusViewState );
            attackOnMoveState.AddChild( _hierarchyBld.InstantiateWithDiContainer<PlConfirmKillReservedTargetState>( false ) );

            // コマンド選択
            selectCommandState.AddChild( attackState );
            selectCommandState.AddChild( selectSkillState );
            selectCommandState.AddChild( _hierarchyBld.InstantiateWithDiContainer<PlWaitState>( false ) );

            // 攻撃
            attackState.AddChild( characterStatusViewState );
            attackState.AddChild( _hierarchyBld.InstantiateWithDiContainer<PlConfirmKillReservedTargetState>( false ) );

            // スキル
            selectSkillState.AddChild( skillActionToTargetState );
            skillActionToTargetState.AddChild( characterStatusViewState );
            skillActionToTargetState.AddChild( _hierarchyBld.InstantiateWithDiContainer<PlSkillUseOptionState>( false ) );
            skillActionToTargetState.AddChild( _hierarchyBld.InstantiateWithDiContainer<PlConfirmKillReservedTargetState>( false ) );
            skillActionToTargetState.AddChild( confirmBlockUndoMoveState );

            // グループ移動
            selectGroupMembersState.AddChild( groupMoveState );
            groupMoveState.AddChild( confirmBlockUndoMoveState );
        }
    }
}