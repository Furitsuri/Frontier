using Frontier.Combat;
using Frontier.Entities;
using Frontier.Stage;
using System.Collections.Generic;
using Zenject;

namespace Frontier.Battle
{
    /// <summary>
    /// プレイヤーキャラクター1人分の移動操作(実体を目的地へ向けて実際に歩かせる・移動前の位置を表示する・移動を完了/取り消しする)を扱います。
    /// 単体移動(PlMoveState)とグループ移動(PlGroupMoveState)の双方がこのクラスを用いることで、
    /// 移動の処理と、ユーザーからの見え方(実体が歩き、移動前のタイルに残像が残り、そこからの経路が矢印で示される)を揃えています。
    /// 生成はHierarchyBuilderBase.InstantiateWithDiContainerで行ってください。
    /// </summary>
    public class PlayerMoveOperation
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private StageController _stageCtrl         = null;

        private Player _owner                       = null;
        private int _originTileIndex                = -1;
        private int _destinationTileIndex           = -1;
        // 移動先の変更(暫定移動中の移動のやり直し)として行っている移動操作か
        private bool _isRepositioning               = false;
        private MoveOriginIndicator _originIndicator = null;

        public Player Owner => _owner;
        public bool IsActive => ( null != _owner );
        public int OriginTileIndex => _originTileIndex;
        public int DestinationTileIndex => _destinationTileIndex;
        /// <summary>移動先の変更(暫定移動中の移動のやり直し)として行っている移動操作か</summary>
        public bool IsRepositioning => _isRepositioning;

        /// <summary>実体が移動前のタイルから離れているか(1タイル以上移動しているか)</summary>
        public bool HasMoved => _owner.BattleParams.TmpParam.CurrentTileIndex != _originTileIndex;

        /// <summary>
        /// 移動操作を開始します。移動前のタイルを起点とした移動可能範囲のデータを設定し、移動前の位置の表示を開始します。
        /// 事前にowner.HoldBeforeMoveInfo()で移動前の状態が保存されている必要があります
        /// (移動前のタイル・向きは、その保存内容を参照します)。
        /// 移動可能範囲の描画は用途によって異なるため、呼び出し側で行ってください。
        /// </summary>
        public void Begin( Player owner )
        {
            LazyInject.GetOrCreate( ref _originIndicator, () => _hierarchyBld.InstantiateWithDiContainer<MoveOriginIndicator>( false ) );

            _owner                  = owner;
            _originTileIndex        = owner.PrevMoveInformaiton.tmpParam.CurrentTileIndex;
            _destinationTileIndex   = -1;
            // 既に暫定的に移動している場合は、移動先の変更として扱う。
            // 起点・移動可能範囲は最初に移動を開始した地点(保存されている移動前の位置)のままとなる
            _isRepositioning        = owner.IsProvisionallyMoved();

            // 以降の経路探索は、この時点(移動前)の状況を基にした移動可能範囲のデータを用いる
            float originTileHeight = _stageCtrl.GetTileStaticData( _originTileIndex ).Height;
            owner.BattleLogic.ActionRangeCtrl.SetupActionableRangeData( _originTileIndex, originTileHeight );

            // 残像は、実体が移動前のタイルに立っている間は表示しない
            _originIndicator.Show( owner, HasMoved );
        }

        /// <summary>
        /// 指定タイルに留まることが出来るか(移動可能範囲内で、他キャラクターの存在や予約によって塞がれていないか)を取得します
        /// </summary>
        public bool CanStandOn( int tileIndex )
        {
            var actionRangeCtrl = _owner.BattleLogic.ActionRangeCtrl;

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
            MovePathHandler pathHdlr = _owner.BattleLogic.ActionRangeCtrl.MovePathHdlr;
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

            bool isArrived = _owner.BattleLogic.UpdateMovePath( moveSpeedRate );

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
            _owner.BattleLogic.ActionRangeCtrl.MovePathHdlr.TruncateAfterFocusedWaypoint();

            // 目的地が留まることの出来ないタイルの場合、矢印は実体が止まるタイルまでを表示しているため、止まる位置の変化に合わせて更新する
            if( !CanStandOn( _destinationTileIndex ) )
            {
                _originIndicator.SetPath( FindShortestPathFromOrigin( GetStoppingTileIndex() ) );
            }
        }

        /// <summary>
        /// 現在の位置で移動を完了させます。移動コマンドを使用済みにして行動履歴へ積み、移動前へ戻せる暫定移動の状態として記録します。
        /// ただし移動中に直接攻撃を行った場合は、既に行動が確定しているため暫定移動の状態にはしません。
        /// </summary>
        public void Commit()
        {
            _owner.BattleParams.TmpParam.SetEndCommandStatus( COMMAND_TAG.MOVE, true );
            // 移動先の変更の場合は、最初の移動の時点で既に行動履歴へ積まれているため、重ねて積まない
            // (重ねて積むと、移動を取り消しても履歴が残ってしまう)
            if( !_isRepositioning ) { _owner.PushCommandHistory( COMMAND_TAG.MOVE ); }

            if( !_owner.BattleParams.TmpParam.IsEndCommand[( int ) COMMAND_TAG.ATTACK] )
            {
                _owner.MarkMoveProvisional();
                // 移動前の位置を示す目印の経路表示用に、移動前の地点から移動後の地点までの最短経路を保持する。
                // 移動範囲内を自由に歩き回って移動先を決められるため、実際に通った経路ではなく最短経路を用いる
                // (ジグザグに歩いた場合などに、無駄の多い経路が描画されるのを避けるため)
                _owner.HoldMovedPath( FindShortestPathFromOrigin( _owner.BattleParams.TmpParam.CurrentTileIndex ) );
            }
        }

        /// <summary>
        /// 移動操作を取り消し、実体を即座に元の位置へ戻します。
        /// 通常の移動の場合は移動前の位置・状態へ、移動先の変更の場合は変更を開始する前の位置へ戻します(暫定移動の状態は維持されます)。
        /// </summary>
        public void Revert()
        {
            if( _isRepositioning ) { _owner.RevertToRepositionStart(); }
            else { _owner.RevertBeforeMoving(); }
        }

        /// <summary>
        /// 移動操作を終了し、移動前の位置の表示を消去します(Commit/Revertのいずれの後にも呼び出してください)
        /// </summary>
        public void End()
        {
            _originIndicator?.Hide();
            _owner = null;
        }

        /// <summary>
        /// 現在の目的地へ向けて、実体が歩く経路を引き直します
        /// </summary>
        private void SetupMovePath()
        {
            var actionRangeCtrl         = _owner.BattleLogic.ActionRangeCtrl;
            MovePathHandler pathHdlr    = actionRangeCtrl.MovePathHdlr;
            int departingTileIndex      = _owner.BattleParams.TmpParam.CurrentTileIndex;
            bool isEndPathTrace         = pathHdlr.IsEndPathTrace();

            // 現在のパストレースが終了していない場合は、直近のwaypointを出発地点にする
            if( !isEndPathTrace )
            {
                departingTileIndex = pathHdlr.GetFocusedWaypointIndex();
            }

            actionRangeCtrl.FindActuallyMovePath( departingTileIndex, _destinationTileIndex, _owner.GetStatusRef.jumpForce, _owner.BattleLogic.TileCostTable, isEndPathTrace );
        }

        /// <summary>
        /// 移動前の地点から指定タイルまでの最短経路を求めます。
        /// 移動可能範囲のデータが残っていない等で経路を求められない場合はnullを返します(その場合は経路の矢印を表示しません)。
        /// </summary>
        private List<WaypointInformation> FindShortestPathFromOrigin( int destinationTileIndex )
        {
            var moveableTileMap = _owner.BattleLogic.ActionRangeCtrl.ActionableTileData.MoveableTileMap;
            if( moveableTileMap.Count <= 0 || destinationTileIndex == _originTileIndex ) { return null; }

            return _stageCtrl.ExtractShortestPath( _originTileIndex, destinationTileIndex, _owner.GetStatusRef.jumpForce, _owner.BattleLogic.TileCostTable, moveableTileMap );
        }
    }
}
