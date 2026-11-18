using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// ツールチップウィンドウの表示位置を求めます。
    /// 対象の項目の横(右→左の順)に、表示可能範囲からはみ出さないよう配置します。
    /// 座標は全て同じ座標系(ツールチップの親のローカル座標)で扱います。
    /// </summary>
    static public class TooltipPlacement
    {
        /// <summary>
        /// ツールチップの配置先の矩形を求めます。
        /// 1. 項目の右側 → 2. 項目の左側 の順に、横方向に収まる位置を探します(上端は項目の上端に揃え、縦方向は表示可能範囲に収まるようずらします)。
        /// 3. 左右どちらにも収まらない場合は、項目の下側 → 上側の順に探します(横方向は表示可能範囲に収まるようずらします)。
        /// ツールチップが表示可能範囲より大きい場合は、上端・左端を優先して揃えます。
        /// </summary>
        /// <param name="target">ツールチップを表示する対象の項目の矩形</param>
        /// <param name="size">ツールチップの大きさ</param>
        /// <param name="bounds">ツールチップを表示してよい範囲(画面から余白や入力ガイド等を除いた範囲)</param>
        /// <param name="space">項目とツールチップの間隔</param>
        /// <returns>ツールチップの矩形</returns>
        static public Rect Calculate( Rect target, Vector2 size, Rect bounds, Vector2 space )
        {
            float x;
            float top;

            if( target.xMax + space.x + size.x <= bounds.xMax )
            {
                x   = target.xMax + space.x;
                top = target.yMax + space.y;
            }
            else if( bounds.xMin <= target.xMin - space.x - size.x )
            {
                x   = target.xMin - space.x - size.x;
                top = target.yMax + space.y;
            }
            else
            {
                x = ClampMin( target.xMin, bounds.xMin, bounds.xMax - size.x );

                float belowTop = target.yMin - space.y;
                float aboveTop = target.yMax + space.y + size.y;
                if( bounds.yMin <= belowTop - size.y )      { top = belowTop; }
                else if( aboveTop <= bounds.yMax )          { top = aboveTop; }
                else                                        { top = target.yMax + space.y; }
            }

            float yMin = ClampMin( top - size.y, bounds.yMin, bounds.yMax - size.y );
            if( bounds.height < size.y ) { yMin = bounds.yMax - size.y; }   // 収まらない場合は上端を揃える(下側がはみ出す)

            return new Rect( x, yMin, size.x, size.y );
        }

        /// <summary>
        /// 値をmin～maxに収めます。maxがminを下回る(範囲より大きい)場合はminを優先します。
        /// </summary>
        static private float ClampMin( float value, float min, float max )
        {
            return Mathf.Max( min, Mathf.Min( value, max ) );
        }
    }
}
