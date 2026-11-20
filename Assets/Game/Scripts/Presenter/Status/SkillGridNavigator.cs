using System.Collections.Generic;
using UnityEngine;
using static Constants;

namespace Frontier.Presenter
{
    /// <summary>
    /// ステータス画面右側(スキル枠が行・列に揃って並ぶ表)の操作体系です。
    /// 行・列は各項目の画面上の位置から求めるため、列数やレイアウトが変わっても追従します。
    /// ・上下入力 : 入力方向で最も近い行へ移り、その行の中で横位置が最も近い項目を選びます。
    /// ・左右入力 : 同じ行の中で、入力方向に最も近い項目を選びます。
    /// いずれも端に達した場合は移動しません(左端からの左入力による左側領域への移動は StatusPresenter が受け持ちます)。
    /// </summary>
    public class SkillGridNavigator : IStatusCursorNavigator
    {
        public int Move( IReadOnlyList<Rect> itemRects, int currentIndex, Direction dir )
        {
            if( currentIndex < 0 || itemRects.Count <= currentIndex ) { return -1; }

            switch( dir )
            {
                case Direction.FORWARD: return MoveVertical( itemRects, currentIndex, 1 );
                case Direction.BACK:    return MoveVertical( itemRects, currentIndex, -1 );
                case Direction.RIGHT:   return MoveHorizontal( itemRects, currentIndex, 1 );
                case Direction.LEFT:    return MoveHorizontal( itemRects, currentIndex, -1 );
                default:                return -1;
            }
        }

        /// <summary>
        /// 縦方向(dirY : 上=正、下=負)で最も近い行のうち、横位置が最も近い項目の添字を返します。
        /// </summary>
        private static int MoveVertical( IReadOnlyList<Rect> itemRects, int currentIndex, int dirY )
        {
            Vector2 current = itemRects[currentIndex].center;
            int bestIndex   = -1;
            float bestDy    = float.MaxValue;
            float bestDx    = float.MaxValue;

            for( int i = 0; i < itemRects.Count; ++i )
            {
                if( i == currentIndex ) { continue; }

                Vector2 candidate = itemRects[i].center;
                float dy = ( candidate.y - current.y ) * dirY;
                // 同じ行の項目は上下移動の対象外
                if( dy <= STATUS_CURSOR_POSITION_TOLERANCE ) { continue; }

                float dx = Mathf.Abs( candidate.x - current.x );
                bool isCloserRow = dy < bestDy - STATUS_CURSOR_POSITION_TOLERANCE;
                bool isSameRow   = Mathf.Abs( dy - bestDy ) <= STATUS_CURSOR_POSITION_TOLERANCE;
                if( isCloserRow || ( isSameRow && dx < bestDx ) )
                {
                    bestIndex = i;
                    bestDy    = dy;
                    bestDx    = dx;
                }
            }

            return bestIndex;
        }

        /// <summary>
        /// 同じ行の中で、横方向(dirX : 右=正、左=負)に最も近い項目の添字を返します。
        /// </summary>
        private static int MoveHorizontal( IReadOnlyList<Rect> itemRects, int currentIndex, int dirX )
        {
            Vector2 current = itemRects[currentIndex].center;
            int bestIndex   = -1;
            float bestDx    = float.MaxValue;

            for( int i = 0; i < itemRects.Count; ++i )
            {
                if( i == currentIndex ) { continue; }

                Vector2 candidate = itemRects[i].center;
                // 別の行の項目は左右移動の対象外
                if( STATUS_CURSOR_POSITION_TOLERANCE < Mathf.Abs( candidate.y - current.y ) ) { continue; }

                float dx = ( candidate.x - current.x ) * dirX;
                if( dx <= STATUS_CURSOR_POSITION_TOLERANCE ) { continue; }

                if( dx < bestDx )
                {
                    bestIndex = i;
                    bestDx    = dx;
                }
            }

            return bestIndex;
        }
    }
}
