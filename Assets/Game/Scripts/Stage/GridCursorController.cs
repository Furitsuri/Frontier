using Frontier.Entities;
using Frontier.Registries;
using System;
using System.Collections.ObjectModel;
using UnityEngine;
using Zenject;
using static Frontier.Stage.GridCursorController;

namespace Frontier.Stage
{
    public class GridCursorController
    {
        public enum CursorType : int
        {
            GRID_CURSOR = 0,
            TARGET_CURSOR,

            NUM,
        }

        private HierarchyBuilderBase _hierarchyBld      = null;
        private PrefabRegistry _prefabReg               = null;
        private IStageDataProvider _stageDataProvider   = null;
        private int _atkTargetIndex                     = 0;
        private GridCursor[] _gridCursors               = new GridCursor[( int ) CursorType.NUM];
        private ReadOnlyCollection<int> _refAttackableTileIndices;
        private Func<int, int>[] _directionMoveCallbacks;
        private Func<int, int>[] _directionAttackTargetCallbacks;

        [Inject] public void Construct( HierarchyBuilderBase hierarchyBld, PrefabRegistry prefabReg, IStageDataProvider stageDataProvider )
        {
            _hierarchyBld       = hierarchyBld;
            _prefabReg          = prefabReg;
            _stageDataProvider  = stageDataProvider;

            LazyInject.GetOrCreate( ref _gridCursors[(int)CursorType.GRID_CURSOR],   () => _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<GridCursor>( _prefabReg.GridCursorPrefab, new object[] { Color.yellow, true  }, true, true, "GridCursor"      ) );
            LazyInject.GetOrCreate( ref _gridCursors[(int)CursorType.TARGET_CURSOR], () => _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<GridCursor>( _prefabReg.GridCursorPrefab, new object[] { Color.red,    false }, true, true, "TargetGridCursor" ) );
        }

        public void Init( int initIndex )
        {
            InitCallbacks();

            _gridCursors[(int)CursorType.GRID_CURSOR].Init( initIndex, _directionMoveCallbacks );
            _gridCursors[(int)CursorType.TARGET_CURSOR].Init( initIndex, _directionAttackTargetCallbacks );
            _gridCursors[(int)CursorType.TARGET_CURSOR].SetActive( false );
        }

        public void ApplyGridCursor2CharacterTile( Character character, CursorType cursorType )
        {
            var tileIndex   = character.BattleParams.TmpParam.CurrentTileIndex;

            _gridCursors[( int )cursorType].SetTileIndex( tileIndex );
            SetActiveGridCursor( true, cursorType );
        }

        public void BindGridCursor( GridCursorState state, Character character )
        {
            _gridCursors[( int ) CursorType.GRID_CURSOR].Bind( state, character );

            // 攻撃ステートの場合は、キャラクターの攻撃対象となるタイルのインデックスを参照出来るように登録しておく
            if( GridCursorState.ATTACK == state )
            {
                _refAttackableTileIndices = character.BattleLogic.ActionRangeCtrl.ActionableTileData.RefAttackTargetTileIndicies;
            }
        }

        public void UnbindGridCursor()
        {
            _gridCursors[( int ) CursorType.GRID_CURSOR].Unbind();
        }

        public void SetActiveGridCursor( bool isActive, CursorType cursorType )
        {
            _gridCursors[( int ) cursorType].SetActive( isActive );
        }

        public void SetCameraFocusType( CursorType cursorType )
        {
            _gridCursors[( int ) cursorType].SetCameraFocus( true );
            _gridCursors[( ( int ) cursorType + 1 ) % ( int ) CursorType.NUM ].SetCameraFocus( false );
        }

        public void ApplyTargetCursor2CharacterTile( bool isToggleCamera, Character designatedTarget = null )
        {
            if( designatedTarget != null )
            {
                int targetTileIndex = designatedTarget.BattleParams.TmpParam.CurrentTileIndex;
                if( TrySetAttackTargetIndex( targetTileIndex, isToggleCamera ) ) { return; }
            }

            TrySetAttackTargetIndex( 0, isToggleCamera );
        }

        public bool OperateGridCursor( Direction direction )
        {
            if( direction == Direction.NONE ) { return false; }

            _gridCursors[(int)CursorType.GRID_CURSOR].Move( direction );

            return true;
        }

        public bool OperateTargetSelect( Direction direction )
        {
            if( Direction.NONE == direction ) { return false; }

            _gridCursors[(int)CursorType.TARGET_CURSOR].TransitAttackTarget( direction );

            _atkTargetIndex = _directionAttackTargetCallbacks[( int ) direction]( _atkTargetIndex );
            _gridCursors[( int ) CursorType.TARGET_CURSOR].SetTileIndex( _refAttackableTileIndices[_atkTargetIndex] );
            _gridCursors[( int ) CursorType.TARGET_CURSOR].SetActive( true );

            return true;
        }

        public int GetCurrentGridIndex()
        {
            return _gridCursors[(int)CursorType.GRID_CURSOR].CurrentTileIndex;
        }

        public int GetCurrentTargetIndex()
        {
            return _gridCursors[( int ) CursorType.TARGET_CURSOR].CurrentTileIndex;
        }

        public GridCursorState GetGridCursorState()
        {
            return _gridCursors[(int)CursorType.GRID_CURSOR].GridState;
        }

        public Character GetBindCharacterFromGridCursor()
        {
            return _gridCursors[(int)CursorType.GRID_CURSOR].BindCharacter;
        }

        /// <summary>
        /// 攻撃対象インデックス値を設定します
        /// </summary>
        /// <param name="index">攻撃対象インデックス値</param>
        public bool TrySetAttackTargetIndex( int targetTileIndex, bool isToggleCamera )
        {
            for( int i = 0; i < _refAttackableTileIndices.Count; i++ )
            {
                if( targetTileIndex == _refAttackableTileIndices[i] )
                {
                    _atkTargetIndex = i;
                    _gridCursors[( int ) CursorType.TARGET_CURSOR].SetTileIndex( _refAttackableTileIndices[i] );
                    _gridCursors[( int ) CursorType.TARGET_CURSOR].SyncPositionToTile();
                    _gridCursors[( int ) CursorType.TARGET_CURSOR].SetActive( true );

                    // ターゲットカーソルにカメラフォーカスを切り替える
                    if( isToggleCamera )
                    {
                        SetCameraFocusType( CursorType.TARGET_CURSOR );
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 前のターゲットインデックス値に遷移します
        /// </summary>
        public int TransitPrevTarget( int atkTargetIndex )
        {
            return ( atkTargetIndex - 1 ) < 0 ? _refAttackableTileIndices.Count - 1 : atkTargetIndex - 1;
        }

        /// <summary>
        /// 次のターゲットインデックス値に遷移します
        /// </summary>
        public int TransitNextTarget( int atkTargetIndex )
        {
            return ( atkTargetIndex + 1 ) % _refAttackableTileIndices.Count;
        }

        private void InitCallbacks()
        {
            _directionMoveCallbacks = new Func<int, int>[( int ) Direction.NUM]
            {
                // Direction.FORWARD
                ( tileIndex ) =>
                {
                    tileIndex += _stageDataProvider.CurrentData.TileColNum;
                    if( _stageDataProvider.CurrentData.GetTileTotalNum() <= tileIndex )
                    {
                        tileIndex = tileIndex % ( _stageDataProvider.CurrentData.GetTileTotalNum() );
                    }

                    return tileIndex;
                },
                // Direction.RIGHT
                ( tileIndex ) =>
                {
                    tileIndex++;
                    if( tileIndex % _stageDataProvider.CurrentData.TileColNum == 0 )
                    {
                        tileIndex -= _stageDataProvider.CurrentData.TileColNum;
                    }
                    return tileIndex;
                },
                // Direction.BACK
                ( tileIndex ) =>
                {
                    tileIndex -= _stageDataProvider.CurrentData.TileColNum;
                    if( tileIndex < 0 )
                    {
                        tileIndex += _stageDataProvider.CurrentData.GetTileTotalNum();
                    }
                    return tileIndex;
                },
                // Direction.LEFT
                ( tileIndex ) =>
                {
                    tileIndex--;
                    if( ( tileIndex + 1 ) % _stageDataProvider.CurrentData.TileColNum == 0 )
                    {
                        tileIndex += _stageDataProvider.CurrentData.TileColNum;
                    }
                    return tileIndex;
                }
            };

            _directionAttackTargetCallbacks = new Func<int, int>[( int ) Direction.NUM]
            {
                // Direction.FORWARD
                ( atkTargetIndex ) => TransitNextTarget( atkTargetIndex ),
                // Direction.RIGHT
                ( atkTargetIndex ) => TransitNextTarget( atkTargetIndex ),
                // Direction.BACK
                ( atkTargetIndex ) => TransitPrevTarget( atkTargetIndex ),
                // Direction.LEFT
                ( atkTargetIndex ) => TransitPrevTarget( atkTargetIndex ),
            };
        }
    }
}
