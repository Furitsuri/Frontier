using Frontier.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using UniRx;
using UnityEngine;
using Zenject;
using static Constants;

#pragma warning disable 0618

namespace Frontier.Stage
{
    public class GridCursorController : MonoBehaviour
    {
        [Header( "移動補間時間" )]
        [SerializeField]
        private float MoveInterpolationTime = 1f;

        [Inject] private IStageDataProvider _stageDataProvider  = null;
        [Inject] private BattleCameraController _btlCamCtrl     = null;

        private int _tileIndex              = 0;
        private int _atkTargetIndex         = 0;
        private float _totalTime            = 0;
        private GridCursorState _gridState  = GridCursorState.NONE;
        private Vector3 _beginPos           = Vector3.zero;
        private Vector3 _endPos             = Vector3.zero;
        private Vector3 _currentPos         = Vector3.zero;
        private Character _bindCharacter    = null;
        private ReadOnlyCollection<int> _refAttackableTileIndices;
        private LineRenderer _lineRenderer;
        private Func<int, int>[] _directionMoveCallbacks;
        private Func<int, int>[] _directionAttackTargetCallbacks;

        public int CurrentTileIndex => _tileIndex;
        public GridCursorState GridState => _gridState;
        public Character BindCharacter => _bindCharacter;

        private void Start()
        {
            _lineRenderer = gameObject.GetComponent<LineRenderer>();
        }

        public void Init( int initIndex )
        {
            _atkTargetIndex = 0;
            _bindCharacter  = null;
            _gridState      = GridCursorState.NONE;

            SetTileIndex( initIndex );

            InitCallbacks();
        }

        public void AssignAttackTargetTileIndices( ReadOnlyCollection<int> attackTargetTileIndices )
        {
            _refAttackableTileIndices = attackTargetTileIndices;
        }

        public void Bind( GridCursorState gridState, Character bindCharacter )
        {
            _gridState      = gridState;
            _bindCharacter  = bindCharacter;

            if( null == _bindCharacter ) { return; }

            // 攻撃ステートの場合は、キャラクターの攻撃対象となるタイルのインデックスを参照出来るように登録しておく
            if( GridCursorState.ATTACK == _gridState )
            {
                _refAttackableTileIndices = _bindCharacter.BattleLogic.ActionRangeCtrl.ActionableTileData.RefAttackTargetTileIndicies;
            }
        }

        public void Unbind()
        {
            if( _bindCharacter != null )
            {
                _bindCharacter.gameObject.SetLayerRecursively( LAYER_MASK_INDEX_CHARACTER );
            }

            _gridState      = GridCursorState.NONE;
            _bindCharacter  = null;
        }

        public void Move( Direction direction )
        {
            StartLerpMove();

            _tileIndex = _directionMoveCallbacks[ ( int )direction ]( _tileIndex );
        }

        public void SetActive( bool isActive )
        {
            gameObject.SetActive( isActive );
        }

        public void SetTileIndex( int index )
        {
            _tileIndex = index;
        }

        public void TransitAttackTarget( Direction direction )
        {
            StartLerpMove();

            _atkTargetIndex = _directionAttackTargetCallbacks[( int ) direction]( _atkTargetIndex );
            _tileIndex      = _refAttackableTileIndices[_atkTargetIndex];
        }

        /// <summary>
        /// 攻撃対象インデックス値を設定します
        /// </summary>
        /// <param name="index">攻撃対象インデックス値</param>
        public bool TrySetAttackTargetIndex( int targetTileIndex )
        {
            for( int i = 0; i < _refAttackableTileIndices.Count; i++ )
            {
                if( targetTileIndex == _refAttackableTileIndices[i] )
                {
                    _atkTargetIndex = i;
                    _tileIndex = _refAttackableTileIndices[_atkTargetIndex];

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

        public int X()
        {
            return _tileIndex % _stageDataProvider.CurrentData.TileColNum;
        }

        public int Y()
        {
            return _tileIndex / _stageDataProvider.CurrentData.TileColNum;
        }

        public Vector3 GetPosition()
        {
            return _currentPos;
        }

        private void InitCallbacks()
        {
            _directionMoveCallbacks = new Func<int, int>[( int )Direction.NUM]
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

            _directionAttackTargetCallbacks = new Func<int, int>[( int )Direction.NUM]
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

        private void Update()
        {
            UpdateUI( DeltaTimeProvider.DeltaTime );
        }

        /// <summary>
        /// 選択しているカーソル位置を更新します
        /// </summary>
        /// /// <param name="delta">フレーム間の時間</param>
        private void UpdateUI( float delta )
        {
            _endPos = GetGoalPosition();

            UpdateLerpPosition( delta );
        }

        /// <summary>
        /// グリッドの位置を線形補間で更新します
        /// </summary>
        /// <param name="delta">フレーム間の時間</param>
        private void UpdateLerpPosition( float delta )
        {
            _totalTime += delta;
            _currentPos = Vector3.Lerp( _beginPos, _endPos, _totalTime / MoveInterpolationTime );

            SetCameraLookAtPosAndDrawCursor( _currentPos );
        }

        /// <summary>
        /// 指定した位置(centralPos)に四角形ラインを描画します
        /// </summary>
        /// <param name="gridSize">1グリッドのサイズ</param>
        /// <param name="centralPos">指定グリッドの中心位置</param>
        private void DrawSquareLine( float gridSize, in Vector3 centralPos )
        {
            float halfSize = 0.5f * gridSize;

            Vector3[] linePoints = new Vector3[]
            {
                new Vector3(-halfSize, GRID_CURSOR_OFFSET_Y, -halfSize) + centralPos,
                new Vector3(-halfSize, GRID_CURSOR_OFFSET_Y,  halfSize) + centralPos,
                new Vector3( halfSize, GRID_CURSOR_OFFSET_Y,  halfSize) + centralPos,
                new Vector3( halfSize, GRID_CURSOR_OFFSET_Y, -halfSize) + centralPos,
            };

            // SetVertexCountは廃止されているはずだが、使用しないと正常に描画されなかったため使用(2023/5/26)
            _lineRenderer.SetVertexCount( linePoints.Length );
            _lineRenderer.SetPositions( linePoints );
        }

        /// <summary>
        /// 線形補間移動を開始します
        /// </summary>
        private void StartLerpMove()
        {
            _beginPos   = GetGoalPosition();
            _totalTime  = 0f;
        }

        private void SetCameraLookAtPosAndDrawCursor( in Vector3 pos )
        {
            DrawSquareLine( TILE_SIZE, pos );
            _btlCamCtrl.SetLookAtBasedOnSelectCursor( pos );
        }

        /// <summary>
        /// グリッドの現在座標を取得します
        /// </summary>
        /// <returns>グリッドの現在座標</returns>
        private Vector3 GetGoalPosition()
        {
            return _stageDataProvider.CurrentData.GetTileStaticData( _tileIndex ).CharaStandPos;
        }
    }
}