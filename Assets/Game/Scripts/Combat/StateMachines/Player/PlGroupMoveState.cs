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
    /// PlSelectTileStateでOPT1入力によりグループ移動の登録者が0人から1人になった際に自動遷移する、
    /// グループ移動のプレビュー・実行ステートです。PlSelectTileStateを継承し、グリッドカーソル移動や
    /// OPT1による登録操作のたびに、登録された各キャラクターを貪欲法によって目的地(現在のカーソル位置)
    /// 周辺の到達可能な空きタイルへ割り当て直し、ゴースト表示・移動経路矢印によるプレビューを更新します。
    /// CONFIRM入力を受けると、その時点のプレビュー通りに全キャラクターを同時に移動させます。
    /// </summary>
    public class PlGroupMoveState : PlSelectTileState
    {
        private enum Phase
        {
            PREVIEW = 0,
            EXECUTE_MOVE,
            END,
        }

        private class GroupMoveAssignment
        {
            public readonly Player Character;
            public readonly int DepartureTileIndex;
            public readonly int DestinationTileIndex;

            public bool IsMoving => DestinationTileIndex != DepartureTileIndex;

            public GroupMoveAssignment( Player character, int departureTileIndex, int destinationTileIndex )
            {
                Character             = character;
                DepartureTileIndex    = departureTileIndex;
                DestinationTileIndex  = destinationTileIndex;
            }
        }

        private readonly List<GroupMoveAssignment> _assignments = new List<GroupMoveAssignment>();
        private Phase _phase;

        public override void Init( object context )
        {
            base.Init( context );  // PlSelectTileStateの初期化を再利用(各種文言設定・RefreshUseableSkillFlags等)

            _phase = Phase.PREVIEW;
            _assignments.Clear();

            RefreshGroupMovePreview();  // 現在のカーソル位置(=登録操作を行った位置)を目的地としてプレビューを計算
        }

        public override bool Update()
        {
            if( Phase.PREVIEW == _phase )
            {
                // カーソル移動・文言更新・登録者の失格判定はPlSelectTileStateの実装をそのまま再利用する
                if( base.Update() ) { return true; }

                // OPT1による登録解除やPruneIneligibleRegistrationsによって登録者が0人になった場合は自動的に戻る
                if( _groupMoveRegistrationList.IsEmpty )
                {
                    Back();
                    return true;
                }

                return false;
            }

            switch( _phase )
            {
                case Phase.EXECUTE_MOVE:
                    bool isAllArrived = true;
                    foreach( var assignment in _assignments )
                    {
                        if( !assignment.IsMoving ) { continue; }
                        if( !assignment.Character.BattleLogic.UpdateMovePath( CHARACTER_MOVE_HIGH_SPEED_RATE ) )
                        {
                            isAllArrived = false;
                        }
                    }

                    if( isAllArrived ) { _phase = Phase.END; }
                    break;

                case Phase.END:
                    foreach( var assignment in _assignments )
                    {
                        assignment.Character.SetGhostActive( false );
                        assignment.Character.BattleLogic.ActionRangeCtrl.ClearMoveDirectionArrows();

                        // 実際に移動したキャラクターのみ移動コマンドを消費する(留まったキャラクターは個別に移動可能なままにする)
                        if( assignment.IsMoving )
                        {
                            assignment.Character.BattleParams.TmpParam.SetEndCommandStatus( COMMAND_TAG.MOVE, true );
                            assignment.Character.PushCommandHistory( COMMAND_TAG.MOVE );
                        }

                        _groupMoveRegistrationList.Remove( assignment.Character );
                        assignment.Character.RestoreMaterialsOriginalColor();
                    }

                    Back();
                    return true;
            }

            return ( 0 <= TransitIndex );
        }

        public override object ExitState()
        {
            // キャンセル等、プレビューフェーズのまま終了する場合はゴースト・矢印・予約タイルを後始末し、
            // 登録していたキャラクターも全て解放する(実行フェーズへ進んだ場合はEXECUTE_MOVE/END側で
            // 予約解放・後始末・登録解除が既に完了しているため対象外)
            if( Phase.PREVIEW == _phase )
            {
                ClearPreview();
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
                (GuideIcon.CANCEL,     "BACK", CanAcceptDefault, new AcceptContextInput( AcceptCancel ),    0.0f, hashCode),
                (GuideIcon.OPT1, _inputOpt1StrWrapper, CanAcceptOpt1, new AcceptContextInput( AcceptOpt1 ), 0.0f, hashCode)
            );
        }

        /// <summary>
        /// プレビューフェーズ中のみ入力を受け付けます
        /// </summary>
        protected override bool CanAcceptDefault()
        {
            if( Phase.PREVIEW != _phase ) { return false; }
            return base.CanAcceptDefault();
        }

        /// <summary>
        /// プレビューフェーズ中のみOPT1(登録・解除)を受け付けます
        /// </summary>
        protected override bool CanAcceptOpt1()
        {
            if( Phase.PREVIEW != _phase ) { return false; }
            return base.CanAcceptOpt1();
        }

        /// <summary>
        /// 方向入力を受けてカーソルを移動させた際、その位置を目的地としてプレビューを再計算します
        /// </summary>
        protected override bool AcceptDirection( InputContext context )
        {
            bool isAccepted = base.AcceptDirection( context );

            if( isAccepted )
            {
                RefreshGroupMovePreview();
            }

            return isAccepted;
        }

        /// <summary>
        /// OPT1入力を受けた際、カーソル上のキャラクターの登録・解除を切り替え、プレビューを再計算します。
        /// 登録者が0人になった場合は元のタイル選択ステートへ自動的に戻ります。
        /// </summary>
        protected override bool AcceptOpt1( InputContext context )
        {
            if( !AcceptOpt1Core( context ) ) { return false; }

            Character character = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            if( null == character ) { return false; }

            ToggleGroupMoveRegistration( character, out _ );

            if( _groupMoveRegistrationList.IsEmpty )
            {
                Back();
            }
            else
            {
                RefreshGroupMovePreview();
            }

            return true;
        }

        /// <summary>
        /// プレビューフェーズ中、割り当てが1件以上ある場合のみCONFIRM(実行)を受け付けます
        /// </summary>
        protected override bool CanAcceptConfirm()
        {
            if( Phase.PREVIEW != _phase ) { return false; }
            return 0 < _assignments.Count;
        }

        /// <summary>
        /// 決定入力を受けた際、その時点のプレビュー通りに移動実行フェーズへ移行します
        /// </summary>
        protected override bool AcceptConfirm( InputContext context )
        {
            if( !AcceptConfirmCore( context ) ) { return false; }
            if( Phase.PREVIEW != _phase || _assignments.Count <= 0 ) { return false; }

            ClearMoveRangeDisplay();
            ReleaseCurrentReservations();
            _phase = Phase.EXECUTE_MOVE;

            return true;
        }

        /// <summary>
        /// 現在のカーソル位置を目的地として、登録済みキャラクターのゴースト・移動経路プレビューを再計算します
        /// </summary>
        private void RefreshGroupMovePreview()
        {
            ClearPreview();
            AssignGroupMoveDestinations( _stageCtrl.GetCurrentGridIndex() );
        }

        /// <summary>
        /// 登録済みキャラクターを目的地までの距離が近い順に並べ、貪欲法で移動先タイルを割り当てます。
        /// 目的地周辺に到達可能な空きタイルが1つもない場合は、自身の現在地(=行けるところまで)がフォールバックとして選ばれます。
        /// </summary>
        private void AssignGroupMoveDestinations( int targetTileIndex )
        {
            var eligible = new List<Player>();
            foreach( var key in _groupMoveRegistrationList.GetAll() )
            {
                Player character = _btlRtnCtrl.BtlCharaCdr.GetPlayer( key );
                if( null == character || !Command.IsExecutableMoveCommand( character, _stageCtrl ) ) { continue; }

                eligible.Add( character );
            }

            // 目的地までの距離が近いキャラクター順に割り当てる(貪欲法)。OrderByは安定ソートのため、同値の場合は登録順が維持される
            var sortedCharacters = eligible.OrderBy( c => _stageCtrl.CalculateTotalRange( c.BattleParams.TmpParam.CurrentTileIndex, targetTileIndex ) );

            foreach( var character in sortedCharacters )
            {
                int dprtIdx          = character.BattleParams.TmpParam.CurrentTileIndex;
                float dprtHeight     = _stageCtrl.GetTileStaticData( dprtIdx ).Height;
                var actionRangeCtrl  = character.BattleLogic.ActionRangeCtrl;

                actionRangeCtrl.SetupActionableRangeData( dprtIdx, dprtHeight );
                // 登録キャラクターごとに移動可能範囲を描画する。タイル毎にオーナーキー別のメッシュとして
                // Y軸方向にずらして描画されるため、他キャラクターの範囲と重なっても埋もれず個別に視認できる
                actionRangeCtrl.DrawMoveableRange();

                int bestIdx   = dprtIdx;
                int bestRange = int.MaxValue;
                foreach( var tile in actionRangeCtrl.ActionableTileData.MoveableTileMap )
                {
                    // 立てないタイル(生存キャラクターが存在する、または他キャラクターが着地予約(RESERVED)している)は候補から除外する
                    if( !tile.Value.IsStandableBy( character.GetCharacterKey() ) ) { continue; }

                    int range = _stageCtrl.CalculateTotalRange( tile.Key, targetTileIndex );
                    if( range < bestRange )
                    {
                        bestRange = range;
                        bestIdx   = tile.Key;
                    }
                }

                if( bestIdx != dprtIdx )
                {
                    actionRangeCtrl.FindMovePath( dprtIdx, bestIdx, character.GetStatusRef.jumpForce, character.BattleLogic.TileCostTable );
                    actionRangeCtrl.PlaceMoveDirectionArrows( dprtIdx, actionRangeCtrl.MovePathHdlr.ProposedMovePath );

                    var ghostObject = character.GetGhostObject();
                    var destTile    = _stageCtrl.GetTileStaticData( bestIdx );
                    ghostObject.TileIndex = bestIdx;
                    ghostObject.transform.SetPositionAndRotation( destTile.CharaStandPos, character.transform.rotation );
                    character.SetGhostActive( true );
                }

                // ★重要 : 次のキャラクターのSetupActionableRangeDataにこの予約を反映させるため、ループ内で都度UpdateTileDynamicDatasを呼ぶ
                //          (ExtractActionableRangeDataは毎回タイルデータをクローンするため、都度反映しないと重複割り当てが起こり得る)
                _stageCtrl.TileDataHdlr().ReserveTile( bestIdx );
                _stageCtrl.TileDataHdlr().UpdateTileDynamicDatas();

                _assignments.Add( new GroupMoveAssignment( character, dprtIdx, bestIdx ) );
            }
        }

        /// <summary>
        /// 現在の割り当てのゴースト・移動経路矢印を消去し、予約タイルを解放した上で割り当てをクリアします
        /// </summary>
        private void ClearPreview()
        {
            foreach( var assignment in _assignments )
            {
                assignment.Character.SetGhostActive( false );
                assignment.Character.BattleLogic.ActionRangeCtrl.ClearMoveDirectionArrows();
            }

            ClearMoveRangeDisplay();
            ReleaseCurrentReservations();

            _assignments.Clear();
        }

        /// <summary>
        /// グループ移動の登録キャラクターを全て解放します(マテリアルを元に戻した上で登録リストをクリアします)
        /// </summary>
        private void ClearAllRegistrations()
        {
            foreach( var key in _groupMoveRegistrationList.GetAll() )
            {
                _btlRtnCtrl.BtlCharaCdr.GetPlayer( key )?.RestoreMaterialsOriginalColor();
            }

            _groupMoveRegistrationList.Clear();
        }

        /// <summary>
        /// 現在の割り当てを持つ各キャラクターの移動可能範囲表示を消去します
        /// </summary>
        private void ClearMoveRangeDisplay()
        {
            foreach( var assignment in _assignments )
            {
                assignment.Character.BattleLogic.ActionRangeCtrl.ActionableRangeRdr.ClearTileMeshesByType( TileMapType.MOVEABLE );
            }
        }

        /// <summary>
        /// 現在の割り当てが保持する予約タイルのみを解放します(ゴースト・矢印・割り当てリストはそのまま維持します)
        /// </summary>
        private void ReleaseCurrentReservations()
        {
            foreach( var assignment in _assignments )
            {
                _stageCtrl.TileDataHdlr().ReleaseTile( assignment.DestinationTileIndex );
            }
            _stageCtrl.TileDataHdlr().UpdateTileDynamicDatas();
        }
    }
}
