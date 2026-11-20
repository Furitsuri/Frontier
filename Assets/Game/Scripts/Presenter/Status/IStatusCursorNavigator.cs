using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Presenter
{
    /// <summary>
    /// ステータス画面の1つの領域(左側のステータス値・右側のスキル)内における、カーソルの移動先を決める操作体系です。
    /// 領域をまたぐ移動は扱わず、StatusPresenter が受け持ちます。
    /// </summary>
    public interface IStatusCursorNavigator
    {
        /// <summary>
        /// 領域内で指定方向へ移動した先の項目の添字を返します。領域内に移動先が無い場合は -1 を返します。
        /// </summary>
        /// <param name="itemRects">領域内の各項目の矩形(共通の座標系、y は上向きが正)</param>
        /// <param name="currentIndex">選択中の項目の添字</param>
        /// <param name="dir">入力方向</param>
        int Move( IReadOnlyList<Rect> itemRects, int currentIndex, Direction dir );
    }
}
