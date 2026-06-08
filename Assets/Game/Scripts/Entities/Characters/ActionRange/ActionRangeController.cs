using Frontier.Battle;
using Frontier.Combat;
using Frontier.Stage;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier.Entities
{
    public class ActionRangeController
    {
        private BattleRoutineController  _btlRtnCtrl              = null;
        private StageController          _stageCtrl               = null;
        private Character                _owner                   = null;
        private MovePathHandler          _movePathHandler         = null;
        private ActionableTileData       _actionableTileData      = null;
        private ActionableRangeRenderer  _actionableRangeRdr     = null;
        private MoveDirectionArrowPlacer _moveDirectionArrowPlacer = null;
        private TileDataHandler.GhostTileResolver _ghostTileResolver = null;
        private Action<TargetingRangeContext, bool, bool, int, int, ActionableTileData>[] RefreshTargetableRangeCallbacks;

        public MovePathHandler MovePathHdlr => _movePathHandler;
        public ActionableTileData ActionableTileData => _actionableTileData;
        public ActionableRangeRenderer ActionableRangeRdr => _actionableRangeRdr;

        [Inject] public ActionRangeController( HierarchyBuilderBase hierarchyBld, BattleRoutineController btlRtnCtrl, StageController stageCtrl )
        {
            _btlRtnCtrl = btlRtnCtrl;
            _stageCtrl  = stageCtrl;

            LazyInject.GetOrCreate( ref _actionableTileData,       () => hierarchyBld.InstantiateWithDiContainer<ActionableTileData>( false ) );
            LazyInject.GetOrCreate( ref _movePathHandler,          () => hierarchyBld.InstantiateWithDiContainer<MovePathHandler>( false ) );
            LazyInject.GetOrCreate( ref _actionableRangeRdr,       () => hierarchyBld.InstantiateWithDiContainer<ActionableRangeRenderer>( false ) );
            LazyInject.GetOrCreate( ref _moveDirectionArrowPlacer, () => hierarchyBld.InstantiateWithDiContainer<MoveDirectionArrowPlacer>( false ) );

            RefreshTargetableRangeCallbacks = new Action<TargetingRangeContext, bool, bool, int, int, ActionableTileData>[( int ) TargetingMode.NUM]
            {
                NormalAttackTargetingRange.RefreshTargetableRange,    // TargetingMode.NORMAL_ATTACK
                PartOfRangeTargetingRange.RefreshTargetableRange,     // TargetingMode.PART_OF_RANGE
                DirectionalTargetingRange.RefreshTargetableRange,     // TargetingMode.DIRECTIONAL
                AllTargetingRange.RefreshTargetableRange,             // TargetingMode.ALL
            };
        }

        public void Init( Character owner )
        {
            _owner = owner;

            _actionableTileData.Init();
            _movePathHandler.Init( owner );
            _actionableRangeRdr.Init( owner, _actionableTileData );
            _moveDirectionArrowPlacer.Init( owner.GetCharacterKey() );
        }

        public void Dispose()
        {
            _actionableRangeRdr.Dispose();
            _actionableTileData.Dispose();
            _movePathHandler.Dispose();
            _moveDirectionArrowPlacer.ClearArrows();

            _owner                    = null;
            _actionableRangeRdr       = null;
            _actionableTileData       = null;
            _movePathHandler          = null;
            _moveDirectionArrowPlacer = null;
        }
        public void SetupActionableRangeData( int dprtTileIdx, float dprtTileHeight )
        {
            _actionableTileData.Init();

            var charaParam  = _owner.GetStatusRef;
            var mvRng       = charaParam.moveRange;
            var jmp         = charaParam.jumpForce;
            var atkRng      = !_owner.BattleParams.TmpParam.IsEndCommand[( int ) COMMAND_TAG.ATTACK] ? _owner.GetStatusRef.attackRange : 0;

            _stageCtrl.TileDataHdlr().ExtractActionableRangeData( dprtTileIdx, mvRng, jmp, atkRng, dprtTileHeight, _owner.BattleLogic.TileCostTable, _owner.GetCharacterKey(), ref _actionableTileData );
        }

        /// <summary>
        /// 通常攻撃用に攻撃可能な範囲を設定します
        /// </summary>
        /// <param name="dprtTileIdx"></param>
        public void SetupAttackableRangeData( int dprtTileIdx )
        {
            _actionableTileData.Init();

            var atkRng = !_owner.BattleParams.TmpParam.IsEndCommand[( int ) COMMAND_TAG.ATTACK] ? _owner.GetStatusRef.attackRange : 0;

            _stageCtrl.TileDataHdlr().ExtractAttackableData( dprtTileIdx, atkRng, RangeShape.FROM_MYSELF, _owner.GetCharacterKey(), ref _actionableTileData );
        }

        /// <summary>
        /// スキルによる攻撃用に攻撃可能な範囲を設定します
        /// </summary>
        /// <param name="dprtTileIdx"></param>
        /// <param name="useSkillID"></param>
        public void SetupAttackableRangeData( int dprtTileIdx, SkillID useSkillID )
        {
            SkillsData.Data skillData = SkillsData.data[( int ) useSkillID];
            if( !SkillsData.IsTransitionSkillActionType( skillData.ActionType ) )
            {
                Debug.LogError( "攻撃用のスキル以外のスキルが指定されています。" );

                return;
            }

            // スキルの効果範囲に負の値が設定されている場合は、キャラクターの攻撃レンジをそのまま用いる
            var atkRng = ( skillData.RangeValue < 0 ) ? _owner.GetStatusRef.attackRange : skillData.RangeValue;

            _actionableTileData.Init();
            var postProcessor = ( useSkillID == SkillID.DASH_SLASH ) ? DashSlashSA.FilterAttackTargets : null;
            _ghostTileResolver = null;
            _stageCtrl.TileDataHdlr().ExtractAttackableData( dprtTileIdx, atkRng, skillData.RangeShape, _owner.GetCharacterKey(), ref _actionableTileData, postProcessor );
        }

        public void ClearActionableRangeData()
        {
            _actionableTileData.Init();
        }

        public void ClearActionableRangeDataWithRender()
        {
            _actionableRangeRdr.ClearTileMeshesAllType();

            ClearActionableRangeData();
        }

        public void RefreshTargetableRange( TargetingMode targetingMode, bool isFirstRefresh, bool isWithMove, int tileIndex, int currentRange )
        {
            if( _actionableTileData.IsEmpty() )
            {
                Debug.LogError("アクション可能範囲が設定されていない状態で対象範囲指定の処理が呼び出されています。");
                return;
            }

            // 描画されている攻撃範囲のメッシュを一度全てクリアする
            _actionableRangeRdr.ClearTileMeshesAllType();
            // ターゲット対象タイルのインデックスを一度クリアする
            _actionableTileData.ClearAttackTargetTileIndicies();
            // ターゲット可能タイルのインデックスを一度クリアする
            _actionableTileData.ClearTargetableTile();
            // ターゲットモードに合ったコールバックを呼び出す
            var context = new TargetingRangeContext { BtlRtnCtrl = _btlRtnCtrl, Owner = _owner, StageCtrl = _stageCtrl, GhostResolver = _ghostTileResolver };
            RefreshTargetableRangeCallbacks[( int ) targetingMode]( context, isFirstRefresh, isWithMove, tileIndex, currentRange, _actionableTileData );
            // 攻撃範囲再描画も併せて行う
            DrawAttackableRange();
        }

        /// <summary>
        /// 条件を指定して移動可能範囲を設定します
        /// </summary>
        /// <param name="setup"></param>
        /// <param name="condition"></param>
        /// <param name="args"></param>
        public void SetupMoveableRangeDataFilterByCondition( Func<TileDynamicData[]> setup, Func<TileDynamicData, object[], bool> condition, params object[] args )
        {
            _actionableTileData.Init();

            _stageCtrl.TileDataHdlr().ExtractMoveableRangeDataFilterByCondition( ref _actionableTileData, setup, condition, args );
        }

        public void ToggleDisplayDangerRange( in Color color )
        {
            // ActionableTileDataが空の場合はこのタイミングでセットアップを行う
            if( _actionableTileData.IsEmpty() )
            {
                int dprtTileIndex   = _owner.BattleParams.TmpParam.CurrentTileIndex;
                var data            =_stageCtrl.GetTileStaticData( dprtTileIndex );
                SetupActionableRangeData( dprtTileIndex, data.Height );
            }

            _actionableRangeRdr.SetDisplayDangerRange( !_actionableRangeRdr.IsShowingAttackableRange, color );
        }

        public void SetDisplayDangerRange( bool isShow, in Color color )
        {
            // ActionableTileDataが空の場合はこのタイミングでセットアップを行う
            if( _actionableTileData.IsEmpty() )
            {
                int dprtTileIndex   = _owner.BattleParams.TmpParam.CurrentTileIndex;
                var data            =_stageCtrl.GetTileStaticData( dprtTileIndex );
                SetupActionableRangeData( dprtTileIndex, data.Height );
            }

            _actionableRangeRdr.SetDisplayDangerRange( isShow, color );
        }

        public void DrawMoveableRange()
        {
            _actionableRangeRdr.DrawRange( TileMapType.MOVEABLE, tileData => new (MeshType, bool)[]
            {
                ( MeshType.REACHABLE_ATTACK,        Methods.HasAnyFlag(tileData.Flag, TileBitFlag.REACHABLE_ATTACK) ),
                ( MeshType.MOVE,                    0 <= tileData.EstimatedMoveRange ),
            } );
        }

        public void DrawAttackableRange()
        {
            // メッシュタイプとそれに対応する描画条件( MEMO : 描画優先度の高い順に並べること )
            _actionableRangeRdr.DrawRange( TileMapType.ATTACKABLE, tileData => new (MeshType, bool)[]
            {
                ( MeshType.ATTACKABLE_TARGET_EXIST, Methods.HasAnyFlag(tileData.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST) && tileData.EstimatedMoveRange < 0 ),
                ( MeshType.ATTACKABLE,              Methods.HasAnyFlag(tileData.Flag, TileBitFlag.ATTACKABLE) && tileData.EstimatedMoveRange < 0 )
            } );

            _actionableRangeRdr.DrawRange( TileMapType.TARGETABLE, _ => new (MeshType, bool)[]
            {
                ( MeshType.TARGETABLE, true ),
            } );
        }

        public void ReDrawAttackableRange()
        {
            _actionableRangeRdr.ClearTileMeshesAllType();
            DrawAttackableRange();
        }

        /// <summary>
        /// スキルキュー時の QUEUED 範囲を描画し、直線移動スキルの場合は進行方向矢印を配置します。
        /// skillDestinationTileIndex に -1 を渡すと矢印はクリアされます（移動なしスキル）。
        /// </summary>
        public void DrawTargetableRangeAsQueued( int skillDestinationTileIndex = -1 )
        {
            _actionableTileData.ClearTileMap( TileMapType.QUEUED );
            _actionableTileData.AddQueuedTile( _owner.BattleParams.TmpParam.CurrentTileIndex );

            foreach( var data in _actionableTileData.GetTileMap( TileMapType.TARGETABLE ) )
            {
                _actionableTileData.AddQueuedTile( data.Key );
            }

            _actionableRangeRdr.DrawRange( TileMapType.QUEUED, _ => new (MeshType, bool)[]
            {
                ( MeshType.QUEUED, true ),
            } );

            PlaceMoveDirectionArrows( _owner.BattleParams.TmpParam.CurrentTileIndex, skillDestinationTileIndex );
        }

        /// <summary>
        /// 直線移動スキル用。出発タイルから目的タイルまでの直線経路に進行方向矢印を配置します。
        /// destinationTileIndex が負の場合は矢印をクリアします。
        /// </summary>
        public void PlaceMoveDirectionArrows( int departureTileIndex, int destinationTileIndex )
        {
            if ( destinationTileIndex < 0 || destinationTileIndex == departureTileIndex )
            {
                _moveDirectionArrowPlacer.ClearArrows();
                return;
            }

            int colNum = _stageCtrl.GetGridNumsXZ().Item2;
            int delta  = destinationTileIndex - departureTileIndex;

            int step, dx, dz;
            if ( delta % colNum == 0 )
            {
                int sign = Math.Sign( delta / colNum );
                step = colNum * sign;
                dx = 0; dz = sign;
            }
            else
            {
                int sign = Math.Sign( delta );
                step = sign;
                dx = sign; dz = 0;
            }

            int tileCount = Math.Abs( delta / step );
            var rotation  = Quaternion.LookRotation( new Vector3( dx, 0f, dz ) );
            var entries   = new List<MoveDirectionArrowPlacer.Entry>( tileCount );

            for ( int i = 1; i < tileCount; ++i )
            {
                int  tileIndex = departureTileIndex + step * i;
                bool isLast    = ( i == tileCount - 1 );
                var  dirType   = isLast ? MoveDirectionType.ARROW_STRAIGHT : MoveDirectionType.ARROW_BODY;
                entries.Add( new MoveDirectionArrowPlacer.Entry( tileIndex, dirType, rotation ) );
            }

            _moveDirectionArrowPlacer.PlaceArrows( entries );
        }

        /// <summary>
        /// NPC移動用。ProposedMovePath に沿って各タイルに進行方向矢印を配置します。
        /// </summary>
        public void PlaceMoveDirectionArrows( int currentTileIndex, List<WaypointInformation> proposedMovePath )
        {
            if ( proposedMovePath == null || proposedMovePath.Count == 0 )
            {
                _moveDirectionArrowPlacer.ClearArrows();
                return;
            }

            int colNum    = _stageCtrl.GetGridNumsXZ().Item2;
            int pathCount = proposedMovePath.Count;
            var entries   = new List<MoveDirectionArrowPlacer.Entry>( pathCount );

            for ( int i = 0; i < pathCount; ++i )
            {
                int prevTileIndex = ( i == 0 ) ? currentTileIndex : proposedMovePath[i - 1].TileIndex;
                int thisTileIndex = proposedMovePath[i].TileIndex;

                ( int inDx, int inDz ) = IndexDeltaToXZ( thisTileIndex - prevTileIndex, colNum );

                MoveDirectionType dirType;
                Quaternion        rotation;

                if ( i == pathCount - 1 )
                {
                    if ( i == 0 )
                    {
                        dirType = MoveDirectionType.ARROW_STRAIGHT;
                    }
                    else
                    {
                        int prevPrevIndex = ( i == 1 ) ? currentTileIndex : proposedMovePath[i - 2].TileIndex;
                        ( int ppDx, int ppDz ) = IndexDeltaToXZ( prevTileIndex - prevPrevIndex, colNum );
                        int cross = ppDx * inDz - ppDz * inDx;
                        dirType   = cross == 0 ? MoveDirectionType.ARROW_STRAIGHT
                                  : cross  > 0 ? MoveDirectionType.ARROW_TURN_LEFT
                                               : MoveDirectionType.ARROW_TURN_RIGHT;
                    }
                    rotation = Quaternion.LookRotation( new Vector3( inDx, 0f, inDz ) );
                }
                else
                {
                    int nextTileIndex         = proposedMovePath[i + 1].TileIndex;
                    ( int outDx, int outDz )  = IndexDeltaToXZ( nextTileIndex - thisTileIndex, colNum );
                    int cross                 = inDx * outDz - inDz * outDx;
                    if ( cross == 0 )
                    {
                        dirType  = MoveDirectionType.ARROW_BODY;
                        rotation = Quaternion.LookRotation( new Vector3( outDx, 0f, outDz ) );
                    }
                    else if ( cross > 0 )
                    {
                        dirType  = MoveDirectionType.ARROW_BODY_TURN_LEFT;
                        rotation = Quaternion.LookRotation( new Vector3( inDx, 0f, inDz ) );
                    }
                    else
                    {
                        dirType  = MoveDirectionType.ARROW_BODY_TURN_RIGHT;
                        rotation = Quaternion.LookRotation( new Vector3( inDx, 0f, inDz ) );
                    }
                }

                entries.Add( new MoveDirectionArrowPlacer.Entry( thisTileIndex, dirType, rotation ) );
            }

            _moveDirectionArrowPlacer.PlaceArrows( entries );
        }

        /// <summary>
        /// このキャラクターの移動方向矢印をすべて破棄します。
        /// </summary>
        public void ClearMoveDirectionArrows()
        {
            _moveDirectionArrowPlacer.ClearArrows();
        }

        /// <summary>
        /// ターゲット可能範囲(黄色)のみをクリアし、攻撃可能範囲(赤色)のみを再描画します。
        /// DIRECTIONALスキルのキャンセル時など、攻撃範囲を維持したままターゲット表示だけ消したい場合に使います。
        /// </summary>
        public void ClearTargetableAndReDrawAttackableRange()
        {
            _actionableTileData.ClearTargetableTile();
            _actionableTileData.ClearAttackTargetTileIndicies();
            DrawAttackableRange();
        }

        public void DrawActionableRange()
        {
            DrawMoveableRange();
            DrawAttackableRange();
        }

        public bool FindMovePath( int dprtTileIndex, int destTileIndex, int ownerJumpForce, in int[] ownerTileCosts )
        {
            Debug.Assert( null != _actionableTileData, "_actionableTileDataのセットアップが完了しているか確認してください。" );

            return _movePathHandler.FindMovePath( dprtTileIndex, destTileIndex, ownerJumpForce, ownerTileCosts, _actionableTileData.MoveableTileMap );
        }

        public bool FindActuallyMovePath( int departingTileIndex, int destinationTileIndex, int ownerJumpForce, int[] ownerTileCosts, bool isEndPathTrace )
        {
            Debug.Assert( null != _actionableTileData, "_actionableTileDataのセットアップが完了しているか確認してください。" );

            return _movePathHandler.FindActuallyMovePath( departingTileIndex, destinationTileIndex, ownerJumpForce, ownerTileCosts, isEndPathTrace, _actionableTileData );
        }

        public List<CharacterKey> GetAttackTargetCharacterKeys()
        {
            List<CharacterKey> targetKeys = new List<CharacterKey>( _actionableTileData.RefAttackTargetTileIndicies.Count );
            for( int i = 0; i < _actionableTileData.RefAttackTargetTileIndicies.Count; ++i )
            {
                var tileData = _stageCtrl.GetTileDynamicData( _actionableTileData.RefAttackTargetTileIndicies[i] );
                targetKeys.Add( tileData.CharaKey );
            }
            return targetKeys;
        }

        private static ( int dx, int dz ) IndexDeltaToXZ( int delta, int colNum )
        {
            if ( delta ==  colNum ) return ( 0,  1 );
            if ( delta == -colNum ) return ( 0, -1 );
            if ( delta ==  1      ) return ( 1,  0 );
            if ( delta == -1      ) return (-1,  0 );
            return ( 0, 0 );
        }

    }
}