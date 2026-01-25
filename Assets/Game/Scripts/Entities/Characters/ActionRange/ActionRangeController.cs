using Frontier.Entities;
using Frontier.Combat;
using Frontier.Stage;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Unity.Burst.Intrinsics.X86.Avx;

namespace Frontier.Entities
{
    public class ActionRangeController
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private StageController _stageCtrl         = null;

        private Character _owner                            = null;
        private ActionableTileMap _actionableTileMap        = null;
        private MovePathHandler _movePathHandler            = null;
        private ActionableRangeRenderer _actionableRangeRdr = null;

        public ActionableTileMap ActionableTileMap => _actionableTileMap;
        public MovePathHandler MovePathHdlr => _movePathHandler;
        public ActionableRangeRenderer ActionableRangeRdr => _actionableRangeRdr;

        public void Init( Character owner )
        {
            _owner = owner;

            LazyInject.GetOrCreate( ref _actionableTileMap, () => _hierarchyBld.InstantiateWithDiContainer<ActionableTileMap>( false ) );
            LazyInject.GetOrCreate( ref _movePathHandler, () => _hierarchyBld.InstantiateWithDiContainer<MovePathHandler>( false ) );
            LazyInject.GetOrCreate( ref _actionableRangeRdr, () => _hierarchyBld.InstantiateWithDiContainer<ActionableRangeRenderer>( false ) );

            _actionableTileMap.Init();
            _movePathHandler.Init( owner );
            _actionableRangeRdr.Init( owner, _actionableTileMap );
        }

        public void SetupActionableRangeData( int dprtTileIdx, float dprtTileHeight )
        {
            _actionableTileMap.Init();

            var charaParam  = _owner.GetStatusRef;
            var mvRng       = charaParam.moveRange;
            var jmp         = charaParam.jumpForce;
            var atkRng      = !_owner.BattleLogic.BattleParams.TmpParam.IsEndCommand( COMMAND_TAG.ATTACK ) ? _owner.GetStatusRef.attackRange : 0;

            _stageCtrl.TileDataHdlr().ExtractActionableRangeData( dprtTileIdx, mvRng, jmp, atkRng, dprtTileHeight, _owner.BattleLogic.TileCostTable, _owner.CharaKey, ref _actionableTileMap );
        }

        public void SetupAttackableRangeData( int dprtTileIdx )
        {
            _actionableTileMap.Init();

            var atkRng = !_owner.BattleLogic.BattleParams.TmpParam.IsEndCommand( COMMAND_TAG.ATTACK ) ? _owner.GetStatusRef.attackRange : 0;

            _stageCtrl.TileDataHdlr().ExtractAttackableData( dprtTileIdx, atkRng, _owner.CharaKey, ref _actionableTileMap );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="setup"></param>
        /// <param name="condition"></param>
        /// <param name="args"></param>
        public void SetupMoveableRangeDataFilterByCondition( Func<TileDynamicData[]> setup, Func<TileDynamicData, object[], bool> condition, params object[] args )
        {
            _actionableTileMap.Init();

            _stageCtrl.TileDataHdlr().ExtractMoveableRangeDataFilterByCondition( ref _actionableTileMap, setup, condition, args );
        }

        public void ToggleDisplayDangerRange( in Color color )
        {
            // ActionableTileMapが空の場合はこのタイミングでセットアップを行う
            if( _actionableTileMap.IsEmpty() )
            {
                int dprtTileIndex   = _owner.BattleLogic.BattleParams.TmpParam.gridIndex;
                var data            =_stageCtrl.GetTileStaticData( dprtTileIndex );
                SetupActionableRangeData( dprtTileIndex, data.Height );
            }

            _actionableRangeRdr.SetDisplayDangerRange( !_actionableRangeRdr.IsShowingAttackableRange, color );
        }

        public void SetDisplayDangerRange( bool isShow, in Color color )
        {
            // ActionableTileMapが空の場合はこのタイミングでセットアップを行う
            if( _actionableTileMap.IsEmpty() )
            {
                int dprtTileIndex   = _owner.BattleLogic.BattleParams.TmpParam.gridIndex;
                var data            =_stageCtrl.GetTileStaticData( dprtTileIndex );
                SetupActionableRangeData( dprtTileIndex, data.Height );
            }

            _actionableRangeRdr.SetDisplayDangerRange( isShow, color );
        }

        public void DrawMoveableRange()
        {
            _actionableRangeRdr.DrawMoveableRange( tileData => new (MeshType, bool)[]
            {
                ( MeshType.REACHABLE_ATTACK,        Methods.CheckBitFlag(tileData.Flag, TileBitFlag.REACHABLE_ATTACK) ),
                ( MeshType.MOVE,                    0 <= tileData.EstimatedMoveRange ),
            } );
        }

        public void DrawAttackableRange()
        {
            // メッシュタイプとそれに対応する描画条件( MEMO : 描画優先度の高い順に並べること )
            _actionableRangeRdr.DrawAttackableRange( tileData => new (MeshType, bool)[]
            {
                ( MeshType.ATTACKABLE_TARGET_EXIST, Methods.CheckBitFlag(tileData.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST) && tileData.EstimatedMoveRange < 0 ),
                ( MeshType.ATTACKABLE,              Methods.CheckBitFlag(tileData.Flag, TileBitFlag.ATTACKABLE) && tileData.EstimatedMoveRange < 0 ),
            } );
        }

        public void DrawActionableRange()
        {
            DrawMoveableRange();
            DrawAttackableRange();
        }

        public bool FindMovePath( int dprtTileIndex, int destTileIndex, int ownerJumpForce, in int[] ownerTileCosts )
        {
            Debug.Assert( null != _actionableTileMap, "_actionableTileMapのセットアップが完了しているか確認してください。" );

            return _movePathHandler.FindMovePath( dprtTileIndex, destTileIndex, ownerJumpForce, ownerTileCosts, _actionableTileMap.MoveableTileMap );
        }

        public bool FindActuallyMovePath( int departingTileIndex, int destinationTileIndex, int ownerJumpForce, int[] ownerTileCosts, bool isEndPathTrace )
        {
            Debug.Assert( null != _actionableTileMap, "_actionableTileMapのセットアップが完了しているか確認してください。" );

            return _movePathHandler.FindActuallyMovePath( departingTileIndex, destinationTileIndex, ownerJumpForce, ownerTileCosts, isEndPathTrace, _actionableTileMap );
        }
    }
}