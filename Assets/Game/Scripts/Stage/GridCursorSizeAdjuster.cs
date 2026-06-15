using System.Collections.Generic;
using Zenject;
using static Constants;

namespace Frontier.Stage
{
    /// <summary>
    /// エンティティの占有タイル情報を管理し、グリッドカーソルのサイズを自動調整するクラスです。
    /// StageController（ゲーム）と StageEditorController（エディター）の両方から使用されます。
    /// </summary>
    public class GridCursorSizeAdjuster
    {
        [Inject] private IStageDataProvider _stageDataProvider = null;

        private GridCursorController _gridCursorCtrl = null;

        /// <summary>StageProp が占有するタイルインデックス → そのサイズ の逆引きマップ</summary>
        private Dictionary<int, int> _stagePropOccupiedTileToSize = new Dictionary<int, int>();

        /// <summary>Character が占有するタイルインデックス → そのサイズ の逆引きマップ</summary>
        private Dictionary<int, int> _characterOccupiedTileToSize = new Dictionary<int, int>();

        /// <summary>GridCursorController を設定します。Init / Construct 後に呼んでください。</summary>
        public void SetGridCursorController( GridCursorController gridCursorCtrl )
        {
            _gridCursorCtrl = gridCursorCtrl;
        }

        // ──── StageProp ──────────────────────────────────────────────────────

        public void RegisterStagePropOccupied( int anchor, int size )
            => RegisterOccupiedTiles( anchor, size, _stagePropOccupiedTileToSize );

        public void UnregisterStagePropOccupied( int anchor, int size )
            => UnregisterOccupiedTiles( anchor, size, _stagePropOccupiedTileToSize );

        public void ClearStagePropOccupied()
            => _stagePropOccupiedTileToSize.Clear();

        // ──── Character ──────────────────────────────────────────────────────

        public void RegisterCharacterOccupied( int anchor, int size )
            => RegisterOccupiedTiles( anchor, size, _characterOccupiedTileToSize );

        public void UnregisterCharacterOccupied( int anchor, int size )
            => UnregisterOccupiedTiles( anchor, size, _characterOccupiedTileToSize );

        public void ClearCharacterOccupied()
            => _characterOccupiedTileToSize.Clear();

        // ──── サイズ調整 ──────────────────────────────────────────────────────

        /// <summary>
        /// 指定タイルに存在するエンティティのサイズに合わせてグリッドカーソルのサイズを変更します。
        /// 何も存在しない場合は GRID_SIZE_MIN にリセットします。
        /// </summary>
        public void AdjustCursorSizeForTile( int tileIndex )
        {
            int size = GRID_SIZE_MIN;

            if ( _stagePropOccupiedTileToSize.TryGetValue( tileIndex, out int propSize ) )
                size = propSize;
            else if ( _characterOccupiedTileToSize.TryGetValue( tileIndex, out int charSize ) )
                size = charSize;

            _gridCursorCtrl?.SetGridCursorSize( size );
        }

        // ──── 内部ヘルパー ────────────────────────────────────────────────────

        private void RegisterOccupiedTiles( int anchor, int size, Dictionary<int, int> map )
        {
            int colNum = _stageDataProvider.CurrentData.TileColNum;
            for ( int row = 0; row < size; row++ )
                for ( int col = 0; col < size; col++ )
                    map[anchor + col + row * colNum] = size;
        }

        private void UnregisterOccupiedTiles( int anchor, int size, Dictionary<int, int> map )
        {
            int colNum = _stageDataProvider.CurrentData.TileColNum;
            for ( int row = 0; row < size; row++ )
                for ( int col = 0; col < size; col++ )
                    map.Remove( anchor + col + row * colNum );
        }
    }
}
