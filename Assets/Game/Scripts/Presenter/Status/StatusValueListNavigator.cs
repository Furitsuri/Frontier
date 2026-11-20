using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Presenter
{
    /// <summary>
    /// ステータス画面左側(ステータス値の縦1列)の操作体系です。
    /// 上下入力で並び順に移動し、端に達した場合は反対側の端へ戻ります。左右入力では領域内を移動しません。
    /// </summary>
    public class StatusValueListNavigator : IStatusCursorNavigator
    {
        public int Move( IReadOnlyList<Rect> itemRects, int currentIndex, Direction dir )
        {
            int count = itemRects.Count;
            if( count <= 0 ) { return -1; }

            switch( dir )
            {
                case Direction.FORWARD: return ( currentIndex - 1 + count ) % count;
                case Direction.BACK:    return ( currentIndex + 1 ) % count;
                default:                return -1;
            }
        }
    }
}
