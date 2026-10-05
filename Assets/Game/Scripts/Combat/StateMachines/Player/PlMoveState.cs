using Frontier.Combat;
using Frontier.Entities;
using Frontier.Stage;
using Frontier.UI;
using System.Collections.Generic;
using Zenject;
using static Constants;

namespace Frontier.Battle
{
    public class PlMoveState : PlPhaseStateBase
    {
        private enum PlMovePhase
        {
            PL_MOVE = 0,
            PL_MOVE_RESERVE_END,
            PL_MOVE_END,
        }

        /// <summary>
        /// 他のステートへ遷移する前の、実体の止め方です
        /// </summary>
        private enum StopMode
        {
            WALK_TO_DESTINATION = 0,    // 目的地(カーソル位置)まで歩かせる(移動先を確定させる遷移に使用する)
            FINISH_CURRENT_PATH,        // 現在歩いている経路の終点まで歩かせる(カーソルが留まれないタイルにあり、実体が向かっている先で行動させる遷移に使用する)
            STOP_AT_NEXT_TILE,          // 次に到達するタイルで止める(実体の位置が結果に関わらない、一時的な遷移に使用する)
        }

        private enum TransitTag
        {
            ATTACK_ON_MOVE = 0,
            CHARACTER_STATUS,
            CONFIRM_BLOCK_UNDO_MOVE,
        }

        [Inject] private ProvisionalMoveOriginDisplay _provisionalOriginDisplay = null;

        private PlMovePhase _phase          = PlMovePhase.PL_MOVE;
        private int _departTileIndex        = -1;
        private bool _isWaitingForBlockUndoConfirmResult = false;
        // 実体が止まるのを待ってから行う、他のステートへの遷移処理。待っていない間はnull。
        // 実体が歩いている途中で他のステートへ遷移すると、このステートの更新が止まっている間も実体が速度を持ったまま
        // 進み続けてしまう。そのため、このステートから他のステートへの遷移は必ずRequestTransitAfterStopを経由させること
        private System.Action _transitOnArrival = null;
        // 遷移待ちの間、目的地(カーソル位置)へ向けて経路を引き直し続けるか(falseの場合は現在の経路のまま歩かせる)
        private bool _isRetargetingBeforeTransit = false;
        // 操作対象のキャラクターが保持する移動操作(実体を歩かせる・移動前の位置を表示する・移動を完了させる)。
        // グループ移動(PlGroupMoveState)と同じ処理を用いることで、移動の処理とユーザーからの見え方を揃えている。
        // 通常の移動か移動先の変更かといった状況による処理の違いは移動操作側で判断されるため、このステートでは意識しない
        private PlayerMoveOperation _moveOperation = null;

        /// <summary>
        /// 移動中攻撃に遷移します
        /// </summary>
        private void TransitAttackOnMoveState()
        {
            TransitStateWithExit( ( int ) TransitTag.ATTACK_ON_MOVE );
        }

        /// <summary>
        /// 実体が止まるのを待ってから、他のステートへの遷移(またはこのステートの終了)を行うよう予約します。
        /// このステートから他のステートへ移る処理は、歩行中の実体が進み続けてしまうのを防ぐため、必ずこのメソッドを経由させてください
        /// (移動前の位置へ即座に戻すキャンセルのみ、実体を強制的に止めるため対象外です)。
        /// 待っている間は全ての入力を受け付けず、実体は高速で歩きます。既に止まっている場合は次の更新ですぐに遷移します。
        /// </summary>
        /// <param name="transit">実体が止まった後に行う遷移処理</param>
        /// <param name="stopMode">遷移する前の、実体の止め方</param>
        private void RequestTransitAfterStop( System.Action transit, StopMode stopMode )
        {
            _transitOnArrival           = transit;
            _isRetargetingBeforeTransit = ( StopMode.WALK_TO_DESTINATION == stopMode );

            if( StopMode.STOP_AT_NEXT_TILE == stopMode ) { _moveOperation.StopAtNextWaypoint(); }
        }

        /// <summary>
        /// 移動中攻撃に遷移しているかどうかを取得します
        /// </summary>
        /// <returns>遷移の有無</returns>
        private bool IsTransitAttackOnMoveState()
        {
            return ( ( int ) TransitTag.ATTACK_ON_MOVE == TransitIndex );
        }

        /// <summary>
        /// 移動中(現在のステート中)に攻撃へと直接遷移出来るか否かを取得します
        /// </summary>
        /// <param name="info">グリッド情報</param>
        /// <returns>直接遷移の可否</returns>
        private bool CanAttackOnMove( in TileDynamicData tileData )
        {
            if( null == tileData ) { return false; }

            if( !Methods.HasAnyFlag( tileData.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST ) ) { return false; }

            // 実体が最終的に止まるタイル(歩いている途中であれば経路の終点、止まっていれば現在立っているタイル)と、
            // 指定位置との差が攻撃レンジ以内であることが条件。
            // 歩いている途中に攻撃対象を指定した場合は、向かっている先まで歩いてからそこで攻撃するため、止まるタイルを基準とする
            (int, int) ranges = _stageCtrl.CalcurateRanges( _moveOperation.GetStoppingTileIndex(), _stageCtrl.GetCurrentGridIndex() );

            return ranges.Item1 + ranges.Item2 <= _plOwner.GetStatusRef.attackRange;
        }

        public override void Init( object context )
        {
            base.Init( context );

            _isWaitingForBlockUndoConfirmResult = false;
            _transitOnArrival                   = null;
            _moveOperation                      = _plOwner.MoveOperation;

            // 攻撃が終了している場合(移動遷移中に直接攻撃を行った場合)
            if( _plOwner.BattleParams.TmpParam.IsEndCommand[ ( int ) COMMAND_TAG.ATTACK ] )
            {
                _phase = PlMovePhase.PL_MOVE_END;
                return;
            }
            else { _phase = PlMovePhase.PL_MOVE; }

            _departTileIndex = _plOwner.PrevMoveInformaiton.tmpParam.CurrentTileIndex;
            _stageCtrl.BindGridCursor( GridCursorState.MOVE, _plOwner );

            // 移動操作を開始する(移動前のタイルを起点とした移動可能範囲のデータ設定と、移動前の位置の表示の開始)
            _moveOperation.Begin();
            // 移動可能範囲に加え、移動中に直接攻撃できる範囲も表示する
            _plOwner.BattleLogic.ActionRangeCtrl.DrawActionableRange();
            _moveOperation.SetDestination( _stageCtrl.GetCurrentGridIndex() );

            // 移動している間は、どこが他の暫定移動中のキャラクターの移動前のタイルなのかが分かるよう、操作中のキャラクター以外の全員分を表示する
            _provisionalOriginDisplay.ShowAllExcept( _plOwner );
        }

        public override bool Update()
        {
            if( base.Update() )
            {
                return true;
            }

            switch( _phase )
            {
                case PlMovePhase.PL_MOVE:
                    // 他のステートへの遷移待ちの場合は、実体が止まってから遷移する(待っている間は入力を受け付けず、高速で歩かせる)
                    if( null != _transitOnArrival )
                    {
                        if( _moveOperation.UpdateWalking( CHARACTER_MOVE_HIGH_SPEED_RATE, _isRetargetingBeforeTransit ) )
                        {
                            var transit         = _transitOnArrival;
                            _transitOnArrival   = null;
                            transit();
                        }
                        break;
                    }

                    // カーソル位置を目的地として、実体をそこへ向けて歩かせる
                    _moveOperation.SetDestination( _stageCtrl.GetCurrentGridIndex() );
                    _moveOperation.UpdateWalking( 1.0f, true );
                    break;

                case PlMovePhase.PL_MOVE_RESERVE_END:
                    // 移動完了後に終了へ移行
                    if( _moveOperation.UpdateWalking( CHARACTER_MOVE_HIGH_SPEED_RATE, false ) )
                    {
                        _phase = PlMovePhase.PL_MOVE_END;
                    }
                    break;

                case PlMovePhase.PL_MOVE_END:
                    // 現在の位置で移動操作を終える(移動の完了として扱うか等は、状況に応じて移動操作側で判断される)
                    _moveOperation.Complete();
                    Back();     // コマンド選択に戻る

                    return true;
            }

            return ( 0 <= TransitIndex );
        }

        public override object ExitState()
        {
            // 移動操作を終了し、移動前の位置の表示を消去する(移動完了・キャンセル・移動中攻撃への遷移のいずれの場合も)
            _moveOperation?.End();
            _provisionalOriginDisplay.Clear();

            // 選択グリッドを表示
            _stageCtrl.SetActiveGridCursor( true );
            // QUEUED以外のタイルメッシュ描画をすべてクリア
            _btlRtnCtrl.BtlCharaCdr.ClearTileMeshesByType( TileMapType.MOVEABLE | TileMapType.ATTACKABLE | TileMapType.TARGETABLE );

            // 攻撃に直接遷移しない場合のみに限定される処理
            if( !IsTransitAttackOnMoveState() )
            {
                _stageCtrl.UnbindGridCursor();           // 操作対象データをリセット
                _presenter.CharaParamView( ParameterWindowType.Right ).ClearCharacter();   // 右側のパラメータビューをクリア
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
                (GuideIcon.ALL_CURSOR,  "MOVE",     CanAcceptDirection, new AcceptContextInput( AcceptDirection ), GRID_DIRECTION_INPUT_INTERVAL, hashCode),
                (GuideIcon.CONFIRM,     "DECISION", CanAcceptConfirm, new AcceptContextInput( AcceptConfirm ), 0.0f, hashCode),
                (GuideIcon.CANCEL,      "BACK",     CanAcceptDefault, new AcceptContextInput( AcceptCancel ), 0.0f, hashCode),
                (GuideIcon.INFO,        "STATUS", CanAcceptInfo, new AcceptContextInput( AcceptInfo ), 0.0f, hashCode)
             );
        }

        /// <summary>
        /// 操作対象のプレイヤーを設定します
        /// </summary>
        protected override void AdaptSelectPlayer()
        {
            // グリッドカーソルで選択中のプレイヤーを取得
            var selectCharacter = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            _plOwner            = _btlRtnCtrl.BtlCharaCdr.GetPlayer( selectCharacter.GetCharacterKey() );
            NullCheck.AssertNotNull( _plOwner, nameof( _plOwner ) );
        }

        protected override void OnActivated()
        {
            base.OnActivated();

            // パラメータビューにキャラクターを割り当て
            var layerMaskIndex = BattleRoutinePresenter.GetLayerMaskIndexFromWinType( ParameterWindowType.Left );
            _presenter.CharaParamView( ParameterWindowType.Left ).AssignCharacter( _plOwner, layerMaskIndex );

            // 他キャラクターが移動前の位置へ戻せなくなる旨の確認から戻ってきた場合、YESであれば移動を確定する
            if( _isWaitingForBlockUndoConfirmResult )
            {
                _isWaitingForBlockUndoConfirmResult = false;
                var confirmState = GetChildren<PlConfirmBlockUndoMoveState>( ( int ) TransitTag.CONFIRM_BLOCK_UNDO_MOVE );
                if( confirmState != null && confirmState.Confirmed )
                {
                    _phase = PlMovePhase.PL_MOVE_RESERVE_END;
                }
            }
        }

        /// <summary>
        /// 他のステートへの遷移待ち(実体が止まるのを待っている間)は、全ての入力を受け付けません
        /// </summary>
        protected override bool CanAcceptDefault()
        {
            if( null != _transitOnArrival ) { return false; }

            return base.CanAcceptDefault();
        }

        /// <summary>
        /// 決定入力受付の可否を判定します
        /// </summary>
        /// <returns>決定入力受付の可否</returns>
        protected override bool CanAcceptConfirm()
        {
            if( !CanAcceptDefault() ) { return false; }

            if( PlMovePhase.PL_MOVE != _phase ) { return false; }     // 移動フェーズでない場合は終了

            // 移動不可の地点であっても、敵対勢力が存在しており自身の攻撃レンジ以内の場合にはtrueを返す
            int currentIndex = _stageCtrl.GetCurrentGridIndex();
            if( CanAttackOnMove( _plOwner.BattleLogic.ActionRangeCtrl.ActionableTileData.GetAttackableTile( currentIndex ) ) ) { return true; }
            // それ以外は留まることが可能かを確認
            else { return _plOwner.BattleLogic.ActionRangeCtrl.MovePathHdlr.CanStandOnTile( _plOwner.BattleLogic.ActionRangeCtrl.ActionableTileData.GetMoveableTile( currentIndex ) ); }
        }

        /// <summary>
        /// 方向入力受付の可否を判定します
        /// </summary>
        /// <returns>方向入力受付の可否</returns>
        protected override bool CanAcceptDirection()
        {
            if( !CanAcceptDefault() ) { return false; }
            // 移動フェーズでない場合、または移動入力受付が不可能である場合は不可
            if( PlMovePhase.PL_MOVE == _phase ) { return true; }

            return false;
        }

        /// <summary>
        /// グリッド上に移動中のキャラクター以外が選択されている場合は、ステータス画面への遷移を受け付けます
        /// </summary>
        /// <returns></returns>
        protected override bool CanAcceptInfo()
        {
            if( !CanAcceptDefault() ) { return false; }
            // 移動フェーズでない場合は不可
            if( PlMovePhase.PL_MOVE != _phase ) { return false; }
            // 自身以外のキャラクターが選択されていない場合は不可
            if( _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() == null ||
                _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() == _plOwner )
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 方向入力を受け取り、キャラクターを操作します
        /// </summary>
        /// <param name="dir">方向入力</param>
        /// <returns>入力によってキャラクター移動が行われたか</returns>
        protected override bool AcceptDirection( InputContext context )
        {
            bool isAcceptDirection = _stageCtrl.OperateGridCursorBasedOnCamera( ref context.Cursor );

            if( isAcceptDirection )
            {
                var gridSelectChara     = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
                bool isActiveRightParamUI   = ( gridSelectChara != null && gridSelectChara != _plOwner );
                if( isActiveRightParamUI )
                {
                    var layerMaskIndex = BattleRoutinePresenter.GetLayerMaskIndexFromWinType( ParameterWindowType.Right );
                    _presenter.CharaParamView( ParameterWindowType.Right ).AssignCharacter( gridSelectChara, layerMaskIndex );
                }
                _presenter.CharaParamView( ParameterWindowType.Right ).SetActive( isActiveRightParamUI );
            }

            return isAcceptDirection;
        }

        /// <summary>
        /// 決定入力を受けた際は選択した地点に移動するか、選択した場でそのまま攻撃へ遷移します
        /// </summary>
        /// <param name="isConfirm">決定入力</param>
        /// <returns>決定入力実行の有無</returns>
        protected override bool AcceptConfirm( InputContext context )
        {
			if( !base.AcceptConfirm( context ) ) { return false; }

			var currentIndex            = _stageCtrl.GetCurrentGridIndex();
            TileDynamicData tileData    = _plOwner.BattleLogic.ActionRangeCtrl.ActionableTileData.GetAttackableTile( currentIndex );

            // 出発地点と同一グリッドであれば戻る(実体が出発地点へ歩いて戻っている途中の場合は、到着を待ってから戻る)
            if( currentIndex == _departTileIndex )
            {
                RequestTransitAfterStop( () =>
                {
                    // 現在の位置(出発地点)で移動操作を終える(移動しなかったものとして扱うか、移動の取り消しとして扱うかは移動操作側で判断される)
                    _moveOperation.Complete();
                    Back();
                }, StopMode.WALK_TO_DESTINATION );

                return true;
            }
            // 攻撃可能なキャラクターが存在している場合は攻撃へ遷移
            else if( null != tileData && Methods.HasAnyFlag( tileData.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST ) )
            {
                // 実体が歩いている途中の場合は、向かっている先(現在の経路の終点)まで歩かせてから攻撃へ遷移する。
                // 攻撃が届くかどうかは、決定入力の受付時(CanAcceptConfirm)に同じく経路の終点を基準として判定済みだが、念のため到着後にも確認する
                RequestTransitAfterStop( () =>
                {
                    if( CanAttackOnMove( tileData ) ) { TransitAttackOnMoveState(); }
                }, StopMode.FINISH_CURRENT_PATH );

                return true;
            }

            // 移動先が、暫定移動中の他キャラクターの移動前の位置である場合は、そのキャラクターが戻せなくなる旨を確認する
            var blockedNames = CollectUndoBlockedCharacterNames( new int[] { currentIndex }, new Player[] { _plOwner } );
            if( 0 < blockedNames.Count )
            {
                // 実体が移動先へ到着するのを待ってから確認ダイアログを開く
                string[] blockedNameArray = blockedNames.ToArray();
                RequestTransitAfterStop( () =>
                {
                    _isWaitingForBlockUndoConfirmResult = true;
                    SetSendTransitionContext( blockedNameArray );
                    TransitState( ( int ) TransitTag.CONFIRM_BLOCK_UNDO_MOVE );
                }, StopMode.WALK_TO_DESTINATION );

                return true;
            }

            _phase = PlMovePhase.PL_MOVE_RESERVE_END;

            return true;
        }

        /// <summary>
        /// キャンセル入力を受けた際は巻き戻し処理を行います
        /// </summary>
        /// <param name="isCancel">キャンセル入力</param>
        /// <returns>キャンセル入力実行の有無</returns>
        protected override bool AcceptCancel( InputContext context )
        {
            if( !base.AcceptCancel( context ) ) { return false; }

            // 移動操作をキャンセルして実体を元の位置へ戻し、グリッドカーソルもその位置へ追従させる
            // (どの位置へ戻すかは、通常の移動か移動先の変更かに応じて移動操作側で判断される)
            _moveOperation.Cancel();
            _stageCtrl.SyncGridCursorAfterRevert( _plOwner );

            return true;
        }

        /// <summary>
        /// 選択しているキャラクターのステータス画面へ遷移します
        /// </summary>
        /// <param name="isInput"></param>
        /// <returns></returns>
        protected override bool AcceptInfo( InputContext context )
        {
            if( !base.AcceptInfo( context ) ) { return false; }

            // 実体を次に到達するタイルで止めてから、ステータス表示へ遷移する
            Character statusTarget = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            RequestTransitAfterStop( () =>
            {
                // ステータス表示ステートに対象キャラクターを渡す
                SetSendTransitionContext( statusTarget );
                TransitState( ( int ) TransitTag.CHARACTER_STATUS );
            }, StopMode.STOP_AT_NEXT_TILE );

            return true;
        }
    }
}