using System.Collections.Generic;

namespace Frontier.Stage
{
    /// <summary>
    /// 移動範囲・攻撃範囲の計算時に、盤面上のキャラクターの配置を一時的に差し替えるための指定です。
    /// 計算用に複製した盤面に対してのみ適用され、基の盤面データには影響しません。
    /// 移動操作中のキャラクターについて「移動前の位置からは離れ、移動先には立っている」状態を想定した計算などに用います。
    /// </summary>
    public class TileOccupancyOverride
    {
        // 計算上、盤面上に存在しないものとして扱うキャラクター(移動前の位置から離れる予定のキャラクターなど)
        private readonly HashSet<CharacterKey> _ignoredCharaKeys = new HashSet<CharacterKey>();
        // 計算上、指定のタイルに立っているものとして扱うキャラクター(移動先に立つ予定のキャラクターなど)
        private readonly List<(int tileIndex, CharacterKey charaKey)> _placedCharas = new List<(int tileIndex, CharacterKey charaKey)>();

        public IReadOnlyCollection<CharacterKey> IgnoredCharaKeys => _ignoredCharaKeys;
        public IReadOnlyList<(int tileIndex, CharacterKey charaKey)> PlacedCharas => _placedCharas;

        public void Clear()
        {
            _ignoredCharaKeys.Clear();
            _placedCharas.Clear();
        }

        /// <summary>
        /// 盤面上に存在しないものとして扱うキャラクターを追加します
        /// </summary>
        public void AddIgnored( CharacterKey charaKey )
        {
            _ignoredCharaKeys.Add( charaKey );
        }

        /// <summary>
        /// 指定のタイルに立っているものとして扱うキャラクターを追加します
        /// </summary>
        public void AddPlaced( int tileIndex, CharacterKey charaKey )
        {
            _placedCharas.Add( (tileIndex, charaKey) );
        }

        /// <summary>
        /// 盤面上に存在しないものとして扱うキャラクターが、指定のキャラクター群と一致するかを判定します(重複を含まない前提)
        /// </summary>
        public bool HasSameIgnored( IReadOnlyList<CharacterKey> charaKeys )
        {
            int count = 0;
            foreach( var key in charaKeys )
            {
                if( !_ignoredCharaKeys.Contains( key ) ) { return false; }
                ++count;
            }

            return count == _ignoredCharaKeys.Count;
        }

        /// <summary>
        /// 計算用の書き換え用の層へ、配置の差し替えを適用します(基の盤面データには影響しません)。
        /// 先に存在しないものとして扱うキャラクターを取り除いてから、指定のタイルへキャラクターを配置します
        /// (移動前の位置と移動先が同じキャラクターであっても、移動先に立っている状態となるようにするため)。
        /// </summary>
        public void ApplyTo( TileDynamicDataOverlay overlay )
        {
            if( 0 < _ignoredCharaKeys.Count )
            {
                for( int i = 0; i < overlay.Length; ++i )
                {
                    var charaKey = overlay.Peek( i ).CharaKey;
                    if( charaKey.IsValid() && _ignoredCharaKeys.Contains( charaKey ) )
                    {
                        overlay.GetWritable( i ).CharaKey = CharacterKey.Invalid;
                    }
                }
            }

            foreach( var (tileIndex, charaKey) in _placedCharas )
            {
                if( 0 <= tileIndex && tileIndex < overlay.Length )
                {
                    overlay.GetWritable( tileIndex ).CharaKey = charaKey;
                }
            }
        }
    }
}
