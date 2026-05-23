using Frontier.Entities;
using System;
using System.Collections.ObjectModel;
using UnityEngine;
using Zenject;
using static Constants;

#pragma warning disable 0618

namespace Frontier.Stage
{
    public class GridCursor : MonoBehaviour
    {
        [Header( "移動補間時間" )]
        [SerializeField]
        private float MoveInterpolationTime = 1f;

        private IStageDataProvider _stageDataProvider  = null;
        private BattleCameraController _btlCamCtrl     = null;

        private int _tileIndex              = 0;
        private float _totalTime            = 0;
        private bool _isFocusCamera         = false;
        private GridCursorState _gridState  = GridCursorState.NONE;
        private Vector3 _beginPos           = Vector3.zero;
        private Vector3 _endPos             = Vector3.zero;
        private Vector3 _currentPos         = Vector3.zero;
        private Character _bindCharacter    = null;
        private LineRenderer _lineRenderer;
        private Func<int, int>[] _moveCallbacks;

        public int CurrentTileIndex => _tileIndex;
        public GridCursorState GridState => _gridState;
        public Character BindCharacter => _bindCharacter;

        [Inject] public void Construct( Color color, bool isFocusCamera, IStageDataProvider stageDataProvider, BattleCameraController btlCamCtrl )
        {
            _stageDataProvider  = stageDataProvider;
            _btlCamCtrl         = btlCamCtrl;

            _lineRenderer = gameObject.GetComponent<LineRenderer>();

            _isFocusCamera = isFocusCamera;
            var renderer = GetComponent<Renderer>();
            Debug.Assert( renderer != null, "GridCursorクラスのRendererコンポーネントがアタッチされていません" );
            renderer.material.color = color;
        }

        public void Init( int initIndex, Func<int, int>[] moveCallbacks )
        {
            _bindCharacter  = null;
            _gridState      = GridCursorState.NONE;
            _moveCallbacks  = moveCallbacks;

            SetTileIndex( initIndex );
        }

        public void Bind( GridCursorState gridState, Character bindCharacter )
        {
            _gridState      = gridState;
            _bindCharacter  = bindCharacter;
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

            _tileIndex = _moveCallbacks[ ( int )direction ]( _tileIndex );
        }

        public void SetActive( bool isActive )
        {
            gameObject.SetActive( isActive );
        }

        public void SetTileIndex( int index )
        {
            _tileIndex = index;
        }

        /// <summary>
        /// タイルインデックスによるグリッド座標を即時に反映させます。
        /// ※Updateを経由せずに即時反映させたい場合に使用します。
        /// </summary>
        public void SyncPositionToTile()
        {
            _beginPos = _endPos = _currentPos = GetGoalPosition();

            SetCameraLookAtPosAndDrawCursor( _currentPos );
        }

        public void TransitAttackTarget( Direction direction )
        {
            StartLerpMove();
        }

        public void SetCameraFocus( bool isFocus )
        {
            _isFocusCamera = isFocus;
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

            // カメラがグリッドを注視する設定の場合、グリッドの位置に基づいてカメラの注視点を更新する
            if( _isFocusCamera )
            {
                _btlCamCtrl.SetLookAtBasedOnSelectCursor( pos );
            }
        }

        /// <summary>
        /// グリッドの現在座標を取得します
        /// </summary>
        /// <returns>グリッドの現在座標</returns>
        private Vector3 GetGoalPosition()
        {
            var offsetY = _stageDataProvider.CurrentData.GetTile( _tileIndex ).GetTileMeshPosYOffset();
            var goalPos = _stageDataProvider.CurrentData.GetTileStaticData( _tileIndex ).CharaStandPos + new Vector3( 0f, offsetY, 0f );

            return goalPos;
        }
    }
}