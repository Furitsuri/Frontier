namespace Frontier.Stage
{
    /// <summary>
    /// 範囲の計算などで、ステージの各タイルの動的データを基データに影響を与えずに書き換えるための層です。
    /// 書き換えるタイルのみを、初めて書き換える時点で複製します(ステージ全体は複製しません)。
    ///
    /// ・読み取りのみの場合は Peek を用います。まだ複製していないタイルは基データをそのまま返すため、返り値を変更してはいけません
    /// ・書き換える場合や、計算結果(ActionableTileData等)へ登録する場合は GetWritable を用います。返り値は複製であり、変更しても基データには影響しません
    ///
    /// TileDataHandler が使い回すため、Reset で初期化してから用います。
    /// </summary>
    public class TileDynamicDataOverlay
    {
        private StageData _baseData                 = null;
        private TileDynamicData[] _writableDatas    = null;

        /// <summary>
        /// タイルの総数
        /// </summary>
        public int Length => _writableDatas.Length;

        /// <summary>
        /// 指定のステージの現在の動的データを基データとして、複製済みのデータを全て破棄します
        /// </summary>
        public void Reset( StageData baseData )
        {
            int tileNum = baseData.Tiles.Length;
            if( _writableDatas == null || _writableDatas.Length != tileNum )
            {
                _writableDatas = new TileDynamicData[tileNum];
            }
            else
            {
                System.Array.Clear( _writableDatas, 0, tileNum );
            }

            _baseData = baseData;
        }

        /// <summary>
        /// 読み取り用に、指定のタイルの動的データを取得します。
        /// 複製済みであれば複製を、そうでなければ基データを返します(返り値を変更してはいけません)
        /// </summary>
        public TileDynamicData Peek( int tileIndex )
        {
            return _writableDatas[tileIndex] ?? _baseData.GetTileDynamicData( tileIndex );
        }

        /// <summary>
        /// 書き換え用に、指定のタイルの動的データを取得します。
        /// 未複製であれば、その時点の基データを複製して返します(以降は同じ複製を返します)
        /// </summary>
        public TileDynamicData GetWritable( int tileIndex )
        {
            return _writableDatas[tileIndex] ??= _baseData.GetTileDynamicData( tileIndex ).DeepClone();
        }
    }
}
