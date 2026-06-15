using System;
using System.Collections.Generic;
using Zenject;
using static Constants;

namespace Frontier.Stage
{
    /// <summary>
    /// エンティティの占有タイル情報を管理し、グリッドカーソルのサイズと位置を自動調整するクラスです。
    /// StageController（ゲーム）と StageEditorController（エディター）の両方から使用されます。
    ///
    /// [移動ルール]
    ///   サイズ s のエンティティ（アンカー位置）からの移動:
    ///     FORWARD / RIGHT: アンカーから +s（遠い端を越える）
    ///     BACK   / LEFT  : アンカーから -1（近い端から出る）
    ///   移動先が別エンティティの非アンカータイルの場合、そのエンティティのアンカーへスナップします。
    /// </summary>
    public class GridCursorSizeAdjuster
    {
        [Inject] private IStageDataProvider _stageDataProvider = null;

        private GridCursorController _gridCursorCtrl = null;

        // StageProp: 占有タイル → アンカー、アンカー → サイズ
        private Dictionary<int, int> _stagePropOccupiedTileToAnchor = new Dictionary<int, int>();
        private Dictionary<int, int> _stagePropAnchorToSize         = new Dictionary<int, int>();

        // Character: 占有タイル → アンカー、アンカー → サイズ
        private Dictionary<int, int> _characterOccupiedTileToAnchor = new Dictionary<int, int>();
        private Dictionary<int, int> _characterAnchorToSize         = new Dictionary<int, int>();

        // ──── 初期化 ──────────────────────────────────────────────────────────

        /// <summary>
        /// GridCursorController を設定し、エンティティ対応の移動コールバックを登録します。
        /// GridCursorController.Init() の後に呼んでください。
        /// </summary>
        public void SetGridCursorController( GridCursorController gridCursorCtrl )
        {
            _gridCursorCtrl = gridCursorCtrl;
            _gridCursorCtrl.SetDirectionMoveCallbacks( CreateEntityAwareDirectionCallbacks() );
        }

        // ──── StageProp 登録 / 解除 ──────────────────────────────────────────

        public void RegisterStagePropOccupied( int anchor, int size )
            => RegisterOccupiedTiles( anchor, size, _stagePropOccupiedTileToAnchor, _stagePropAnchorToSize );

        public void UnregisterStagePropOccupied( int anchor, int size )
            => UnregisterOccupiedTiles( anchor, size, _stagePropOccupiedTileToAnchor, _stagePropAnchorToSize );

        public void ClearStagePropOccupied()
        {
            _stagePropOccupiedTileToAnchor.Clear();
            _stagePropAnchorToSize.Clear();
        }

        // ──── Character 登録 / 解除 ──────────────────────────────────────────

        public void RegisterCharacterOccupied( int anchor, int size )
            => RegisterOccupiedTiles( anchor, size, _characterOccupiedTileToAnchor, _characterAnchorToSize );

        public void UnregisterCharacterOccupied( int anchor, int size )
            => UnregisterOccupiedTiles( anchor, size, _characterOccupiedTileToAnchor, _characterAnchorToSize );

        public void ClearCharacterOccupied()
        {
            _characterOccupiedTileToAnchor.Clear();
            _characterAnchorToSize.Clear();
        }

        // ──── サイズ・位置調整 ────────────────────────────────────────────────

        /// <summary>
        /// 指定タイルのエンティティに合わせてカーソルのサイズを調整し、
        /// 非アンカータイルに着地した場合はアンカーへスナップします。
        /// </summary>
        public void AdjustCursorSizeForTile( int tileIndex )
        {
            var ( anchor, size ) = GetAnchorAndSize( tileIndex );

            if ( anchor >= 0 && anchor != tileIndex )
            {
                _gridCursorCtrl.SetGridCursorTileIndex( anchor );
                _gridCursorCtrl.SyncGridCursorPosition();
            }

            _gridCursorCtrl.SetGridCursorSize( size );
        }

        // ──── 内部: エンティティ対応移動コールバック ─────────────────────────

        private Func<int, int>[] CreateEntityAwareDirectionCallbacks()
        {
            return new Func<int, int>[( int ) Direction.NUM]
            {
                // Direction.FORWARD（行 +size or +1）
                ( tileIndex ) =>
                {
                    int colNum   = _stageDataProvider.CurrentData.TileColNum;
                    int rowNum   = _stageDataProvider.CurrentData.GetTileTotalNum() / colNum;
                    var ( anchor, size ) = GetAnchorAndSize( tileIndex );
                    int baseIdx  = anchor >= 0 ? anchor : tileIndex;
                    int row      = baseIdx / colNum;
                    int col      = baseIdx % colNum;
                    row += size;
                    if ( row >= rowNum ) row = 0;
                    return row * colNum + col;
                },
                // Direction.RIGHT（列 +size or +1）
                ( tileIndex ) =>
                {
                    int colNum   = _stageDataProvider.CurrentData.TileColNum;
                    var ( anchor, size ) = GetAnchorAndSize( tileIndex );
                    int baseIdx  = anchor >= 0 ? anchor : tileIndex;
                    int row      = baseIdx / colNum;
                    int col      = baseIdx % colNum;
                    col += size;
                    if ( col >= colNum ) col = 0;
                    return row * colNum + col;
                },
                // Direction.BACK（行 -1）
                ( tileIndex ) =>
                {
                    int colNum   = _stageDataProvider.CurrentData.TileColNum;
                    int rowNum   = _stageDataProvider.CurrentData.GetTileTotalNum() / colNum;
                    var ( anchor, size ) = GetAnchorAndSize( tileIndex );
                    int baseIdx  = anchor >= 0 ? anchor : tileIndex;
                    int row      = baseIdx / colNum;
                    int col      = baseIdx % colNum;
                    row--;
                    if ( row < 0 ) row = rowNum - 1;
                    return row * colNum + col;
                },
                // Direction.LEFT（列 -1）
                ( tileIndex ) =>
                {
                    int colNum   = _stageDataProvider.CurrentData.TileColNum;
                    var ( anchor, size ) = GetAnchorAndSize( tileIndex );
                    int baseIdx  = anchor >= 0 ? anchor : tileIndex;
                    int row      = baseIdx / colNum;
                    int col      = baseIdx % colNum;
                    col--;
                    if ( col < 0 ) col = colNum - 1;
                    return row * colNum + col;
                },
            };
        }

        // ──── 内部: ルックアップ ──────────────────────────────────────────────

        /// <summary>タイルインデックスからアンカーとサイズを返します。エンティティがない場合は (-1, GRID_SIZE_MIN)。</summary>
        private ( int anchor, int size ) GetAnchorAndSize( int tileIndex )
        {
            if ( _stagePropOccupiedTileToAnchor.TryGetValue( tileIndex, out int propAnchor ) &&
                 _stagePropAnchorToSize.TryGetValue( propAnchor, out int propSize ) )
                return ( propAnchor, propSize );

            if ( _characterOccupiedTileToAnchor.TryGetValue( tileIndex, out int charAnchor ) &&
                 _characterAnchorToSize.TryGetValue( charAnchor, out int charSize ) )
                return ( charAnchor, charSize );

            return ( -1, GRID_SIZE_MIN );
        }

        // ──── 内部: マップ操作 ────────────────────────────────────────────────

        private void RegisterOccupiedTiles( int anchor, int size,
            Dictionary<int, int> occupiedTileToAnchor, Dictionary<int, int> anchorToSize )
        {
            int colNum = _stageDataProvider.CurrentData.TileColNum;
            for ( int row = 0; row < size; row++ )
                for ( int col = 0; col < size; col++ )
                    occupiedTileToAnchor[anchor + col + row * colNum] = anchor;
            anchorToSize[anchor] = size;
        }

        private void UnregisterOccupiedTiles( int anchor, int size,
            Dictionary<int, int> occupiedTileToAnchor, Dictionary<int, int> anchorToSize )
        {
            int colNum = _stageDataProvider.CurrentData.TileColNum;
            for ( int row = 0; row < size; row++ )
                for ( int col = 0; col < size; col++ )
                    occupiedTileToAnchor.Remove( anchor + col + row * colNum );
            anchorToSize.Remove( anchor );
        }
    }
}
