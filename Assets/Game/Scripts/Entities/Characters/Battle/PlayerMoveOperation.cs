using Frontier.Combat;
using Frontier.Stage;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier.Entities
{
    /// <summary>
    /// プレイヤーキャラクター1人分の移動操作(実体を目的地へ向けて実際に歩かせる・移動前の位置を表示する・移動を完了/取り消しする)を扱います。
    /// PlayerBattleLogicがキャラクター1人につき1つ保持し、単体移動(PlMoveState)とグループ移動(PlGroupMoveState)の双方が
    /// これを用いることで、移動の処理と、ユーザーからの見え方(実体が歩き、移動前のタイルに残像が残り、そこからの経路が矢印で示される)を揃えています。
    ///
    /// 通常の移動か、移動先の変更(暫定移動中の移動のやり直し)かといった状況による処理の違いは、このクラスの中で判断します。
    /// 呼び出し側(各ステート)は、状況を意識せずに Prepare → Begin → (SetDestination/UpdateWalking) → Complete または Cancel → End の順に呼び出してください。
    /// </summary>
    public class PlayerMoveOperation
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private StageController _stageCtrl         = null;

        private Player _owner                           = null;
        private PlayerBattleLogic _ownerLogic           = null;
        private bool _isActive                          = false;
        private int _originTileIndex                    = -1;
        private int _destinationTileIndex               = -1;
        // 移動先の変更(暫定移動中の移動のやり直し)として行っている移動操作か
        private bool _isRepositioning                   = false;
        // 移動先の変更を開始した時点の位置と向き。変更をキャンセルした際にここへ戻す。
        // 移動先の変更を開始する度に上書きされる(2度目の変更をキャンセルした場合は、2度目を開始する前の位置へ戻る)
        private int _repositionStartTileIndex           = -1;
        private Quaternion _repositionStartRot          = Quaternion.identity;
        private MoveOriginIndicator _originIndicator    = null;

        public Player Owner => _owner;
        /// <summary>移動操作の最中か(BeginからEndまでの間)</summary>
        public bool IsActive => _isActive;
        public int OriginTileIndex => _originTileIndex;
        public int DestinationTileIndex => _destinationTileIndex;

        /// <summary>実体が移動前のタイルから離れているか(1タイル以上移動しているか)</summary>
        public bool HasMoved => _owner.BattleParams.TmpParam.CurrentTileIndex != _originTileIndex;

        /// <summary>
        /// 対象のキャラクターを設定します。PlayerBattleLogicの初期化時に呼び出してください。
        /// </summary>
        public void Init( Player owner, PlayerBattleLogic ownerLogic )
        {
            _owner              = owner;
            _ownerLogic         = ownerLogic;
            _isActive           = false;
            _isRepositioning    = false;
        }

        /// <summary>
        /// 生成した表示物(残像・矢印)を破棄します
        /// </summary>
        public void Dispose()
        {
            _originIndicator?.Dispose();
            _originIndicator    = null;
            _isActive           = false;
        }

        /// <summary>
        /// 移動操作の準備として、取り消し・キャンセルの際に戻すための現時点の状態を保存します。
        /// 移動を行うことが決まった時点(コマンドメニューで移動を選んだ時、グループ移動の操作を開始する時)に1度だけ呼び出してください。
        /// ・通常の移動の場合: 現時点の状態を「移動前の状態」として保存します。
        /// ・移動先の変更の場合(既に暫定的に移動している場合): 移動前の状態は上書きせず(起点と移動範囲を最初の地点のままとするため)、
        ///   変更をキャンセルした際に戻すための現時点の位置のみを保存します。
        /// </summary>
        public void Prepare()
        {
            _isRepositioning = _owner.IsProvisionallyMoved();

            if( _isRepositioning )
            {
                _repositionStartTileIndex   = _owner.BattleParams.TmpParam.CurrentTileIndex;
                _repositionStartRot         = _owner.GetRotation();
            }
            else
            {
                _ownerLogic.HoldBeforeMoveInfo();
            }
        }

        /// <summary>
        /// 移動操作を開始します。移動前のタイルを起点とした移動可能範囲のデータを設定し、移動前の位置の表示を開始します。
        /// 事前にPrepareが呼び出されている必要があります(移動前のタイル・向きは、その保存内容を参照します)。
        /// 移動可能範囲の描画は用途によって異なるため、呼び出し側で行ってください。
        /// </summary>
        public void Begin()
        {
            LazyInject.GetOrCreate( ref _originIndicator, () => _hierarchyBld.InstantiateWithDiContainer<MoveOriginIndicator>( false ) );

            _isActive               = true;
            _originTileIndex        = _owner.PrevMoveInformaiton.tmpParam.CurrentTileIndex;
            _destinationTileIndex   = -1;

            // 以降の経路探索は、この時点の状況を基にした、移動前のタイルを起点とする移動可能範囲のデータを用いる
            float originTileHeight = _stageCtrl.GetTileStaticData( _originTileIndex ).Height;
            _ownerLogic.ActionRangeCtrl.SetupActionableRangeData( _originTileIndex, originTileHeight );

            // 残像は、実体が移動前のタイルに立っている間は表示しない
            _originIndicator.Show( _owner, HasMoved );
        }

        /// <summary>
        /// 指定タイルに留まることが出来るか(移動可能範囲内で、他キャラクターの存在や予約によって塞がれていないか)を取得します
        /// </summary>
        public bool CanStandOn( int tileIndex )
        {
            var actionRangeCtrl = _ownerLogic.ActionRangeCtrl;

            return actionRangeCtrl.MovePathHdlr.CanStandOnTile( actionRangeCtrl.ActionableTileData.GetMoveableTile( tileIndex ) );
        }

        /// <summary>
        /// 目的地を設定します。実体はUpdateWalkingによってこのタイルへ向けて歩き、
        /// 移動前のタイルからこのタイルまでの最短経路が矢印で表示されます。
        /// 留まることの出来ないタイル(移動範囲外など)が指定された場合、実体はそこへは向かわないため、
        /// 矢印は実体が実際に止まるタイルまでの経路を表示します。
        /// </summary>
        public void SetDestination( int tileIndex )
        {
            if( tileIndex == _destinationTileIndex ) { return; }

            _destinationTileIndex = tileIndex;

            int routeEndTileIndex = CanStandOn( tileIndex ) ? tileIndex : GetStoppingTileIndex();
            _originIndicator.SetPath( FindShortestPathFromOrigin( routeEndTileIndex ) );
        }

        /// <summary>
        /// 実体が現在の経路のまま歩いた場合に止まるタイルを取得します
        /// (歩いている途中であれば経路の終点、止まっていれば現在立っているタイル)
        /// </summary>
        public int GetStoppingTileIndex()
        {
            MovePathHandler pathHdlr = _ownerLogic.ActionRangeCtrl.MovePathHdlr;
            var movePath             = pathHdlr.ProposedMovePath;

            if( !pathHdlr.IsEndPathTrace() && 0 < movePath.Count )
            {
                return movePath[movePath.Count - 1].TileIndex;
            }

            return _owner.BattleParams.TmpParam.CurrentTileIndex;
        }

        /// <summary>
        /// 実体を目的地へ向けて歩かせます。毎フレーム呼び出してください。
        /// </summary>
        /// <param name="moveSpeedRate">移動速度の倍率</param>
        /// <param name="isRetargeting">現在の目的地へ向けて経路を引き直すか。目的地が変わり得る操作中はtrue、確定後に到着を待つだけの場合はfalseを指定します</param>
        /// <returns>目的地(経路の終点)に到着しているか</returns>
        public bool UpdateWalking( float moveSpeedRate, bool isRetargeting )
        {
            if( isRetargeting ) { SetupMovePath(); }

            bool isArrived = _ownerLogic.UpdateMovePath( moveSpeedRate );

            // 残像は、実体が移動前のタイルに立っている間は表示しない(他キャラクターの実体が立っている場合は表示する)
            _originIndicator.SetAfterimageVisible( HasMoved );

            return isArrived;
        }

        /// <summary>
        /// 歩いている実体を、次に到達するタイルで止めるようにします(それより先の経路を破棄します)。
        /// この後はUpdateWalkingをisRetargeting=falseで呼び出し、到着を待ってください。
        /// </summary>
        public void StopAtNextWaypoint()
        {
            _ownerLogic.ActionRangeCtrl.MovePathHdlr.TruncateAfterFocusedWaypoint();

            // 目的地が留まることの出来ないタイルの場合、矢印は実体が止まるタイルまでを表示しているため、止まる位置の変化に合わせて更新する
            if( !CanStandOn( _destinationTileIndex ) )
            {
                _originIndicator.SetPath( FindShortestPathFromOrigin( GetStoppingTileIndex() ) );
            }
        }

        /// <summary>
        /// 現在の位置で移動操作を終えます。状況に応じて以下のいずれかとして扱います。
        /// ・移動中に直接攻撃を行った後の場合: 既に行動が確定しているため、移動コマンドを使用済みにするのみとします。
        /// ・移動前のタイルに立っている場合: 移動しなかったものとして扱います。移動先の変更で最初の地点へ戻った場合は、
        ///   移動の取り消しとして扱い、暫定移動の状態を解除して移動コマンドを通常の移動として選択出来る状態へ戻します。
        /// ・それ以外の場合: 移動を完了させます。移動コマンドを使用済みにし、移動前へ戻せる暫定移動の状態として記録します。
        /// </summary>
        public void Complete()
        {
            bool isAttackEnded = _owner.BattleParams.TmpParam.IsEndCommand[( int ) COMMAND_TAG.ATTACK];

            if( !isAttackEnded && !HasMoved )
            {
                if( _isRepositioning ) { _owner.RevertLastCommand(); }

                return;
            }

            _owner.BattleParams.TmpParam.SetEndCommandStatus( COMMAND_TAG.MOVE, true );
            // 移動先の変更の場合など、既に移動が行動履歴へ積まれている場合は重ねて積まない
            // (重ねて積むと、移動を取り消しても履歴が残ってしまう)
            if( !_ownerLogic.IsContainsCommandHistory( COMMAND_TAG.MOVE ) ) { _owner.PushCommandHistory( COMMAND_TAG.MOVE ); }

            if( !isAttackEnded )
            {
                _owner.MarkMoveProvisional();
                // 移動前の位置を示す目印の経路表示用に、移動前の地点から移動後の地点までの最短経路を保持する。
                // 移動範囲内を自由に歩き回って移動先を決められるため、実際に通った経路ではなく最短経路を用いる
                // (ジグザグに歩いた場合などに、無駄の多い経路が描画されるのを避けるため)
                _owner.HoldMovedPath( FindShortestPathFromOrigin( _owner.BattleParams.TmpParam.CurrentTileIndex ) );
            }
        }

        /// <summary>
        /// 移動操作をキャンセルし、実体を即座に元の位置へ戻します。
        /// 通常の移動の場合は移動前の位置・状態へ、移動先の変更の場合は変更を開始する前の位置へ戻します(暫定移動の状態は維持されます)。
        /// </summary>
        public void Cancel()
        {
            if( _isRepositioning )
            {
                _ownerLogic.ForcedStopMoving();
                _ownerLogic.SetPositionOnStage( _repositionStartTileIndex, _repositionStartRot );
            }
            else
            {
                _owner.RevertBeforeMoving();
            }
        }

        /// <summary>
        /// 移動操作を終了し、移動前の位置の表示を消去します(Complete/Cancelのいずれの後にも呼び出してください)
        /// </summary>
        public void End()
        {
            _originIndicator?.Hide();
            _isActive = false;
        }

        /// <summary>
        /// 現在の目的地へ向けて、実体が歩く経路を引き直します
        /// </summary>
        private void SetupMovePath()
        {
            var actionRangeCtrl         = _ownerLogic.ActionRangeCtrl;
            MovePathHandler pathHdlr    = actionRangeCtrl.MovePathHdlr;
            int departingTileIndex      = _owner.BattleParams.TmpParam.CurrentTileIndex;
            bool isEndPathTrace         = pathHdlr.IsEndPathTrace();

            // 現在のパストレースが終了していない場合は、直近のwaypointを出発地点にする
            if( !isEndPathTrace )
            {
                departingTileIndex = pathHdlr.GetFocusedWaypointIndex();
            }

            actionRangeCtrl.FindActuallyMovePath( departingTileIndex, _destinationTileIndex, _owner.GetStatusRef.jumpForce, _ownerLogic.TileCostTable, isEndPathTrace );
        }

        /// <summary>
        /// 移動前の地点から指定タイルまでの最短経路を求めます。
        /// 移動可能範囲のデータが残っていない等で経路を求められない場合はnullを返します(その場合は経路の矢印を表示しません)。
        /// </summary>
        private List<WaypointInformation> FindShortestPathFromOrigin( int destinationTileIndex )
        {
            var moveableTileMap = _ownerLogic.ActionRangeCtrl.ActionableTileData.MoveableTileMap;
            if( moveableTileMap.Count <= 0 || destinationTileIndex == _originTileIndex ) { return null; }

            return _stageCtrl.ExtractShortestPath( _originTileIndex, destinationTileIndex, _owner.GetStatusRef.jumpForce, _ownerLogic.TileCostTable, moveableTileMap );
        }
    }
}
