using Froniter.Entities;
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

        private Character _owner                                = null;
        private ActionableTileMap _actionableTileMap            = null;
        private MovePathHandler _movePathHandler                = null;
        private AttackableRangeHandler _attackableRangeHandler  = null;

        public ActionableTileMap ActionableTileMap => _actionableTileMap;
        public MovePathHandler MovePathHdlr => _movePathHandler;

        public void Init( Character owner )
        {
            _owner = owner;

            if( null == _actionableTileMap )
            {
                _actionableTileMap = _hierarchyBld.InstantiateWithDiContainer<ActionableTileMap>( false );
                NullCheck.AssertNotNull( _actionableTileMap, nameof( _actionableTileMap ) );
            }
            if( null == _movePathHandler )
            {
                _movePathHandler = _hierarchyBld.InstantiateWithDiContainer<MovePathHandler>( false );
                NullCheck.AssertNotNull( _movePathHandler, nameof( _movePathHandler ) );
            }
            if( null == _attackableRangeHandler )
            {
                _attackableRangeHandler = _hierarchyBld.InstantiateWithDiContainer<AttackableRangeHandler>( false );
                NullCheck.AssertNotNull( _attackableRangeHandler, nameof( _attackableRangeHandler ) );
            }

            _actionableTileMap.Init();
            _movePathHandler.Init( owner );
            _attackableRangeHandler.Init( owner, _actionableTileMap );
        }

        public void SetupActionableRangeData( int dprtTileIdx, float dprtTileHeight )
        {
            _actionableTileMap.Init();

            var charaParam  = _owner.Params.CharacterParam;
            var mvRng       = charaParam.moveRange;
            var jmp         = charaParam.jumpForce;
            var atkRng      = !_owner.Params.TmpParam.IsEndCommand( COMMAND_TAG.ATTACK ) ? _owner.Params.CharacterParam.attackRange : 0;

            _stageCtrl.TileDataHdlr().ExtractActionableRangeData( dprtTileIdx, mvRng, jmp, atkRng, dprtTileHeight, _owner.TileCostTable, _owner.CharaKey, ref _actionableTileMap );
        }

        public void SetupAttackableRangeData( int dprtTileIdx )
        {
            _actionableTileMap.Init();

            var atkRng = !_owner.Params.TmpParam.IsEndCommand( COMMAND_TAG.ATTACK ) ? _owner.Params.CharacterParam.attackRange : 0;

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

        public void ClearAttackableRange()
        {
            _attackableRangeHandler.UnsetAttackableRangeDisplay();
        }

        public void ToggleAttackableRangeDisplay( in Color color )
        {
            // ActionableTileMapが空の場合はこのタイミングでセットアップを行う
            if( _actionableTileMap.IsEmpty() )
            {
                int dprtTileIndex   = _owner.Params.TmpParam.gridIndex;
                var data            =_stageCtrl.GetTileStaticData( dprtTileIndex );
                SetupActionableRangeData( dprtTileIndex, data.Height );
            }

            _attackableRangeHandler.ToggleAttackableRangeDisplay( color );
        }

        public void DrawAttackableRange()
        {
            _stageCtrl.DrawAttackableRange( _actionableTileMap );
        }

        public void DrawActionableRange()
        {
            _stageCtrl.DrawActionableRange( _actionableTileMap );
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