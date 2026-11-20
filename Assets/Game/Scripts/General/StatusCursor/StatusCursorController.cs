using Frontier.Presenter;
using System.Collections.Generic;
using UnityEngine;
using static Constants;

namespace Frontier.StatusCursor
{
    /// <summary>
    /// ステータス画面における、ツールチップ用カーソルの移動を管理するクラスです。
    /// 選択中の項目(領域と添字)を保持し、方向入力に対する移動先を決めて StatusPresenter に選択表示を指示します。
    /// ・領域内の移動 : 領域ごとの操作体系(IStatusCursorNavigator)に委ねます。
    /// ・領域間の移動 : 領域内に移動先が無く、もう一方の領域へ向かう左右入力の場合に限り、
    ///                  もう一方の領域のうち縦位置が最も近い項目へ移ります(縦のずれに上限は設けません)。
    /// 項目の位置は StatusPresenter から事実として受け取り、表示には直接触れません。
    /// </summary>
    public class StatusCursorController
    {
        private readonly IStatusCursorNavigator _statusValueNavigator  = new StatusValueListNavigator();
        private readonly IStatusCursorNavigator _skillNavigator        = new SkillGridNavigator();

        private StatusPresenter _statusPresenter    = null;
        private StatusItemArea _selectedArea        = StatusItemArea.StatusValue;
        private int _selectedItemIndex              = 0;

        public void Init( StatusPresenter statusPresenter )
        {
            _statusPresenter = statusPresenter;
        }

        /// <summary>
        /// カーソルを初期位置(ステータス値の先頭)に戻します。
        /// </summary>
        public void ResetSelection()
        {
            Select( StatusItemArea.StatusValue, 0 );
        }

        /// <summary>
        /// 方向入力に応じてカーソルを移動します。移動した場合は true を返します。
        /// </summary>
        public bool Move( Direction dir )
        {
            var rects = _statusPresenter.GetItemRects( _selectedArea );
            if( _selectedItemIndex < 0 || rects.Count <= _selectedItemIndex ) { return false; }

            int next = GetNavigator( _selectedArea ).Move( rects, _selectedItemIndex, dir );
            if( 0 <= next )
            {
                Select( _selectedArea, next );
                return true;
            }

            if( !TryGetAdjacentArea( _selectedArea, dir, out var adjacentArea ) ) { return false; }

            int target = FindNearestItemInY( _statusPresenter.GetItemRects( adjacentArea ), rects[_selectedItemIndex] );
            if( target < 0 ) { return false; }

            Select( adjacentArea, target );
            return true;
        }

        private void Select( StatusItemArea area, int index )
        {
            _selectedArea       = area;
            _selectedItemIndex  = index;
            _statusPresenter.SelectItem( area, index );
        }

        private IStatusCursorNavigator GetNavigator( StatusItemArea area )
        {
            return ( area == StatusItemArea.StatusValue ) ? _statusValueNavigator : _skillNavigator;
        }

        /// <summary>
        /// 指定の領域から、指定の方向に隣接する領域を返します(左側のステータス値の右隣がスキル、スキルの左隣がステータス値)。
        /// </summary>
        private static bool TryGetAdjacentArea( StatusItemArea area, Direction dir, out StatusItemArea adjacentArea )
        {
            adjacentArea = area;

            if( area == StatusItemArea.StatusValue && dir == Direction.RIGHT ) { adjacentArea = StatusItemArea.Skill; return true; }
            if( area == StatusItemArea.Skill && dir == Direction.LEFT )        { adjacentArea = StatusItemArea.StatusValue; return true; }

            return false;
        }

        /// <summary>
        /// 縦位置が最も近い項目の添字を返します。縦の距離が同じ項目が複数ある場合は、横の距離が近い方を選びます。項目が無い場合は -1 を返します。
        /// </summary>
        private static int FindNearestItemInY( IReadOnlyList<Rect> itemRects, Rect from )
        {
            int bestIndex = -1;
            float bestDy  = float.MaxValue;
            float bestDx  = float.MaxValue;

            for( int i = 0; i < itemRects.Count; ++i )
            {
                float dy = Mathf.Abs( itemRects[i].center.y - from.center.y );
                float dx = Mathf.Abs( itemRects[i].center.x - from.center.x );
                bool isCloserY = dy < bestDy - STATUS_CURSOR_POSITION_TOLERANCE;
                bool isSameY   = Mathf.Abs( dy - bestDy ) <= STATUS_CURSOR_POSITION_TOLERANCE;
                if( isCloserY || ( isSameY && dx < bestDx ) )
                {
                    bestIndex = i;
                    bestDy    = dy;
                    bestDx    = dx;
                }
            }

            return bestIndex;
        }
    }
}
