using Frontier.Combat;
using Frontier.Entities;
using Frontier.Stage;
using System.Collections.Generic;
using System.Linq;
using Zenject;
using static Constants;

namespace Frontier.Battle
{
    /// <summary>
    /// PlSelectGroupMembersStateでのCONFIRM入力、またはPlSelectTileStateでの一括登録(SUB3)入力により遷移する、
    /// グループ移動の操作・実行ステートです。
    /// PlSelectTileStateを継承し、グリッドカーソル移動のたびに、登録された各キャラクターを貪欲法によって
    /// 目的地(現在のカーソル位置)周辺の到達可能な空きタイルへ割り当て直します。
    /// 各キャラクターの移動は単体移動(PlMoveState)と同じ処理(PlayerMoveOperation)で行うため、実体は割り当て先へ向けて
    /// 実際に歩き、移動前のタイルには残像が、そこからの経路には矢印が表示されます。
    /// このステートでは新たなキャラクターの登録・解除は行えません。
    /// CONFIRM入力を受けると、全キャラクターが割り当て先に到着するのを待ってから移動を完了させます。
    /// キャンセル時は全キャラクターを移動前の位置へ戻した上で、PlSelectGroupMembersStateから遷移した場合は登録を維持したまま戻り、
    /// PlSelectTileStateから直接遷移した場合は登録を全て解除して戻ります。
    /// </summary>
    public class PlGroupMoveState : PlSelectTileState
    {
        /// <summary>
        /// 遷移元を示すコンテキストです。遷移時にSetSendTransitionContextで渡してください(未指定時はFromMemberSelection扱い)
        /// </summary>
        public enum EntryType
        {
            FromMemberSelection = 0,    // PlSelectGroupMembersStateから遷移(戻った後も登録を維持する)
            Direct,                     // PlSelectTileStateから直接遷移(戻る際に登録を全て解除する)
        }

        private enum Phase
        {
            OPERATING = 0,      // カーソルで目的地を操作中(実体は割り当て先へ向けて歩き続ける)
            EXECUTE_MOVE,       // 決定後、全キャラクターの到着待ち
            END,
        }

        private enum TransitTag
        {
            CONFIRM_BLOCK_UNDO_MOVE = 0,
        }

        // 移動中の各キャラクターが保持する移動操作
        private readonly List<PlayerMoveOperation> _moveOperations = new List<PlayerMoveOperation>();

        private Phase _phase;
        private EntryType _entryType;
        private bool _isMoveOperationsBegun              = false;
        private bool _isWaitingForBlockUndoConfirmResult = false;
        // 他のキャラクターが移動している間は、どこが暫定移動中のキャラクターの移動前のタイルなのかが分かるよう、全員分を表示する
        // (移動するメンバー自身は暫定移動の状態ではないため対象にならず、各自の移動前の位置は移動操作側が表示する)
        protected override ProvisionalOriginDisplayMode OriginDisplayMode => ProvisionalOriginDisplayMode.ALL;

        public override void Init( object context )
        {
            base.Init( context );  // PlSelectTileStateの初期化を再利用(各種文言設定・RefreshUseableSkillFlags等)

            _phase      = Phase.OPERATING;
            _entryType  = EntryType.FromMemberSelection;
            ReceiveContext( ref _entryType, context );
            _moveOperations.Clear();
            _isMoveOperationsBegun              = false;
            _isWaitingForBlockUndoConfirmResult = false;
        }

        protected override void OnActivated()
        {
            // 基底でホバー範囲表示の消去が行われる(対象キャラクターの範囲データ・描画も全て消える)ため、移動操作の開始はその後に行う
            base.OnActivated();

            // ステートへ入った初回のみ、登録済みキャラクターの移動操作を開始する(確認ダイアログから戻った際は開始済み)
            if( !_isMoveOperationsBegun )
            {
                BeginMoveOperations();
                _isMoveOperationsBegun = true;
            }

            // 現在のカーソル位置(=登録操作を行った位置)を目的地として、各キャラクターの移動先を割り当てる
            AssignGroupMoveDestinations( _stageCtrl.GetCurrentGridIndex() );

            // 他キャラクターが移動前の位置へ戻せなくなる旨の確認から戻ってきた場合、YESであれば移動を確定する
            if( _isWaitingForBlockUndoConfirmResult )
            {
                _isWaitingForBlockUndoConfirmResult = false;
                var confirmState = GetChildren<PlConfirmBlockUndoMoveState>( ( int ) TransitTag.CONFIRM_BLOCK_UNDO_MOVE );
                if( confirmState != null && confirmState.Confirmed )
                {
                    StartExecuteMove();
                }
            }
        }

        /// <summary>
        /// このステートでは登録キャラクターの移動可能範囲(青色)のみを表示するため、カーソル上のキャラクターの
        /// 移動・攻撃範囲表示(ホバー範囲表示)は行わず、遷移元から引き継いだ表示も消去します
        /// </summary>
        protected override void RefreshHoveredRangeDisplay()
        {
            _hoveredRangeDisplay.Clear();
        }

        public override bool Update()
        {
            switch( _phase )
            {
                case Phase.OPERATING:
                    // カーソル移動・文言更新・登録者の失格判定はPlSelectTileStateの実装をそのまま再利用する
                    if( base.Update() ) { return true; }

                    // PruneIneligibleRegistrationsによって登録者が0人になった場合は自動的に戻る
                    if( _groupMoveRegistrationList.IsEmpty )
                    {
                        Back();
                        return true;
                    }

                    // 各キャラクターの実体は、戦闘ロジックの更新によって割り当て先へ向けて歩く(単体移動の操作中と同じ)
                    return ( 0 <= TransitIndex );

                case Phase.EXECUTE_MOVE:
                    // 全キャラクターが割り当て先に到着するまで待つ(単体移動の決定後と同じく高速で移動させる)
                    if( AreAllArrived() ) { _phase = Phase.END; }
                    break;

                case Phase.END:
                    foreach( var moveOperation in _moveOperations )
                    {
                        Player character = moveOperation.Owner;

                        // 現在の位置で移動操作を終える。実際に移動したキャラクターのみ移動の完了として扱われ、
                        // 留まったキャラクターは個別に移動可能なままとなる(移動操作側で判断される)
                        moveOperation.Complete();

                        character.BattleLogic.ActionRangeCtrl.ActionableRangeRdr.ClearTileMeshesByType( TileMapType.MOVEABLE );
                        _groupMoveRegistrationList.Remove( character );
                    }
                    _moveOperations.Clear();

                    Back();
                    return true;
            }

            return ( 0 <= TransitIndex );
        }

        public override object ExitState()
        {
            // キャンセル等、操作中のまま終了する場合は、全キャラクターを移動前の位置へ即座に戻して後始末する
            // (実行フェーズへ進んだ場合はEND側で完了・後始末・登録解除が既に済んでいるため対象外)
            if( Phase.OPERATING == _phase )
            {
                CancelMoveOperations();
            }

            // タイル選択から直接遷移した場合は、戻り先で登録が残留しないよう全て解放する
            if( EntryType.Direct == _entryType )
            {
                ClearAllRegistrations();
            }

            return base.ExitState();
        }

        /// <summary>
        /// 入力コードを登録します
        /// </summary>
        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
                (GuideIcon.ALL_CURSOR, "MOVE", CanAcceptDefault, new AcceptContextInput( AcceptDirection ), GRID_DIRECTION_INPUT_INTERVAL, hashCode),
                (GuideIcon.CONFIRM,    "MOVE", CanAcceptConfirm, new AcceptContextInput( AcceptConfirm ),   0.0f, hashCode),
                (GuideIcon.CANCEL,     "BACK", CanAcceptDefault, new AcceptContextInput( AcceptCancel ),    0.0f, hashCode)
            );
        }

        /// <summary>
        /// 操作中のみ入力を受け付けます
        /// </summary>
        protected override bool CanAcceptDefault()
        {
            if( Phase.OPERATING != _phase ) { return false; }
            return base.CanAcceptDefault();
        }

        /// <summary>
        /// 方向入力を受けてカーソルを移動させた際、その位置を目的地として各キャラクターの移動先を割り当て直します
        /// </summary>
        protected override bool AcceptDirection( InputContext context )
        {
            bool isAccepted = base.AcceptDirection( context );

            if( isAccepted )
            {
                AssignGroupMoveDestinations( _stageCtrl.GetCurrentGridIndex() );
            }

            return isAccepted;
        }

        /// <summary>
        /// 操作中、移動するキャラクターが1人以上いる場合のみCONFIRM(確定)を受け付けます
        /// </summary>
        protected override bool CanAcceptConfirm()
        {
            if( Phase.OPERATING != _phase ) { return false; }
            return 0 < _moveOperations.Count;
        }

        /// <summary>
        /// 決定入力を受けた際、その時点の割り当て通りに移動を確定します
        /// </summary>
        protected override bool AcceptConfirm( InputContext context )
        {
            if( !AcceptConfirmCore( context ) ) { return false; }
            if( Phase.OPERATING != _phase || _moveOperations.Count <= 0 ) { return false; }

            // 移動先のいずれかが、暫定移動中の他キャラクターの移動前の位置である場合は、そのキャラクターが戻せなくなる旨を確認する
            var destinationTileIndices = new HashSet<int>();
            var movers                 = new HashSet<Player>();
            foreach( var moveOperation in _moveOperations )
            {
                if( moveOperation.DestinationTileIndex == moveOperation.OriginTileIndex ) { continue; }

                destinationTileIndices.Add( moveOperation.DestinationTileIndex );
                movers.Add( moveOperation.Owner );
            }

            var blockedNames = CollectUndoBlockedCharacterNames( destinationTileIndices, movers );
            if( 0 < blockedNames.Count )
            {
                // 確認ダイアログはこのステートを中断するだけの遷移のため、実体が歩いている途中でもそのまま開く
                // (中断している間も、実体は戦闘ロジックの更新によって歩き続け、割り当て先のタイルで止まる)
                _isWaitingForBlockUndoConfirmResult = true;
                SetSendTransitionContext( blockedNames.ToArray() );
                TransitState( ( int ) TransitTag.CONFIRM_BLOCK_UNDO_MOVE );

                return true;
            }

            StartExecuteMove();

            return true;
        }

        /// <summary>
        /// その時点の割り当てで移動を確定し、全キャラクターの到着待ちへ移行します
        /// </summary>
        private void StartExecuteMove()
        {
            // 単体移動の決定後と同じく、割り当て先まで高速で歩かせる
            foreach( var moveOperation in _moveOperations )
            {
                moveOperation.SetWalk( CHARACTER_MOVE_HIGH_SPEED_RATE, true );
            }

            _phase = Phase.EXECUTE_MOVE;
        }

        /// <summary>
        /// 全キャラクターの実体が、割り当て先に到着しているかを取得します
        /// </summary>
        private bool AreAllArrived()
        {
            foreach( var moveOperation in _moveOperations )
            {
                if( !moveOperation.IsArrived ) { return false; }
            }

            return true;
        }

        /// <summary>
        /// 登録済みキャラクターそれぞれの移動操作を開始します。
        /// 単体移動(PlSelectTileStateからPlMoveStateへの遷移時)と同様に、移動操作の準備(移動前の状態の保存)を行った上で、
        /// 移動前のタイルを起点とした移動可能範囲を設定・表示します。
        /// 移動可能範囲は、全キャラクターが移動前の位置にいるこの時点の状況を基に求め、以降の操作中は求め直しません
        /// (実体が歩き始めると各タイルの状況が変わってしまうため)。
        /// </summary>
        private void BeginMoveOperations()
        {
            foreach( var key in _groupMoveRegistrationList.GetAll() )
            {
                Player character = _btlRtnCtrl.BtlCharaCdr.GetPlayer( key );
                if( null == character || !Command.IsExecutableMoveCommand( character, _stageCtrl ) ) { continue; }

                // 登録中を示す半透明表示は解除し、歩く実体を通常の見た目にする(移動前の位置に残る残像と区別するため)
                character.RestoreMaterialsOriginalColor();

                PlayerMoveOperation moveOperation = character.MoveOperation;
                moveOperation.Prepare();
                moveOperation.Begin();
                // 登録キャラクターごとに移動可能範囲を描画する。タイル毎にオーナーキー別のメッシュとして
                // Y軸方向にずらして描画されるため、他キャラクターの範囲と重なっても埋もれず個別に視認できる。
                // 複数キャラクターの範囲が重なるため、攻撃関連の色は混ぜずに移動可能タイルのみを描画する
                character.BattleLogic.ActionRangeCtrl.DrawMoveOnlyRange();

                _moveOperations.Add( moveOperation );
            }
        }

        /// <summary>
        /// 全キャラクターの移動操作を取り消し、移動前の位置へ即座に戻します
        /// </summary>
        private void CancelMoveOperations()
        {
            foreach( var moveOperation in _moveOperations )
            {
                Player character = moveOperation.Owner;

                moveOperation.Cancel();
                character.BattleLogic.ActionRangeCtrl.ActionableRangeRdr.ClearTileMeshesByType( TileMapType.MOVEABLE );

                // メンバー選択へ戻る場合は登録が維持されるため、登録中を示す半透明表示へ戻す
                if( EntryType.FromMemberSelection == _entryType && _groupMoveRegistrationList.Contains( character ) )
                {
                    character.SetMaterialsSemiTransparent();
                }
            }
            _moveOperations.Clear();

            // 実体の位置を戻したことを、タイルの情報へ反映する
            _stageCtrl.TileDataHdlr().UpdateTileDynamicDatas();
        }

        /// <summary>
        /// 登録済みキャラクターを、移動前の位置から目的地までの距離が近い順に並べ、貪欲法で移動先タイルを割り当てます。
        /// 目的地周辺に到達可能な空きタイルが1つもない場合は、移動前の位置がフォールバックとして選ばれます。
        /// </summary>
        private void AssignGroupMoveDestinations( int targetTileIndex )
        {
            // 目的地までの距離が近いキャラクター順に割り当てる(貪欲法)。OrderByは安定ソートのため、同値の場合は登録順が維持される
            var sortedOperations = _moveOperations.OrderBy( op => _stageCtrl.CalculateTotalRange( op.OriginTileIndex, targetTileIndex ) );

            // 先に割り当てたキャラクターの移動先(重複して割り当てないようにする)
            var assignedTileIndices = new HashSet<int>();

            foreach( var moveOperation in sortedOperations )
            {
                Player character = moveOperation.Owner;

                int bestIdx   = moveOperation.OriginTileIndex;
                int bestRange = int.MaxValue;
                foreach( var tile in character.BattleLogic.ActionRangeCtrl.ActionableTileData.MoveableTileMap )
                {
                    // 立てないタイル(生存キャラクターが存在する、または他キャラクターが着地予約(RESERVED)している)、
                    // 及び他のキャラクターへ割り当て済みのタイルは候補から除外する
                    if( !tile.Value.IsStandableBy( character.GetCharacterKey() ) ) { continue; }
                    if( assignedTileIndices.Contains( tile.Key ) ) { continue; }

                    int range = _stageCtrl.CalculateTotalRange( tile.Key, targetTileIndex );
                    if( range < bestRange )
                    {
                        bestRange = range;
                        bestIdx   = tile.Key;
                    }
                }

                assignedTileIndices.Add( bestIdx );
                moveOperation.SetDestination( bestIdx );
            }
        }
    }
}
