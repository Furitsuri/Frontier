using Frontier.Entities;
using Frontier.UI;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Presenter
{
    public class StatusPresenter
    {
        [Inject] private IUiSystem _uiSystem = null;

        private int _selectedItemIndex = 0;
        private StatusUI _statusUi = null;
        private TooltipUI _tooltipUi = null;

        public void Init()
        {
            _statusUi = _uiSystem.GeneralUi.CharacterStatusView;
            _tooltipUi = _uiSystem.GeneralUi.ToolTipView;
            _selectedItemIndex = 0;

            _statusUi.Setup();
            _tooltipUi.Setup();
        }

        public void OpenCharacterStatus( Character chara )
        {
            _selectedItemIndex = 0;
            _statusUi.gameObject.SetActive( true );
            _statusUi.AssignCharacter( chara );

            AddSelectCursorItemIndex( _selectedItemIndex );
        }

        public void CloseCharacterStatus()
        {
            _tooltipUi.gameObject.SetActive( false );
            _statusUi.gameObject.SetActive( false );
        }

        public void ToggleToolTipActive()
        {
            bool toggledActive = !_tooltipUi.gameObject.activeSelf;

            _tooltipUi.gameObject.SetActive( toggledActive );
            _statusUi.SetSelectCursorActive( toggledActive );
        }

        public void AddSelectCursorItemIndex( int addValue )
        {
            _selectedItemIndex += addValue;
            int itemCount = _statusUi.GetStatusItemRectList().Count;

            _selectedItemIndex = ( _selectedItemIndex + itemCount ) % itemCount;

            var itemRectList = _statusUi.GetStatusItemRectList();
            if( _selectedItemIndex < 0 || itemRectList.Count <= _selectedItemIndex )
            {
                _statusUi.SetSelectCursorActive( false );
                return;
            }
            var itemRect = itemRectList[_selectedItemIndex];
            _statusUi.SetSelectCursorRect( itemRect.anchoredPosition, itemRect.sizeDelta );

            // ツールチップの位置とテキストを設定
            SetToolTipPosition( itemRect.anchoredPosition, itemRect.sizeDelta, itemRect.pivot );
            SetToolTipText( "aaa" );
        }

        public bool IsToolTipActive()
        {
            return _tooltipUi.gameObject.activeSelf;
        }

        private void SetToolTipPosition( in Vector2 itemPos, in Vector2 itemSize, in Vector2 itemPivot )
        {
            float itemPivotSizeX = itemSize.x * ( 1 - itemPivot.x );
            float itemPivotSizeY = itemSize.y * ( 1 - itemPivot.y );
            float posX = itemPos.x + itemPivotSizeX + TOOLTIP_WINDOW_SPACE_X;
            float posY = itemPos.y + itemPivotSizeY + TOOLTIP_WINDOW_SPACE_Y;

            Vector2 toolTipPivotSize = _tooltipUi.GetSize();
            toolTipPivotSize.x *= ( 1 - _tooltipUi.GetPivot().x );
            toolTipPivotSize.y *= ( 1 - _tooltipUi.GetPivot().y );

            var screenSize = _uiSystem.GeneralUi.GetScreenSize();

            bool isOverlapRight     = ( posX + toolTipPivotSize.x ) > screenSize.x * 0.5f;
            bool isOverlapBottom    = ( posY + toolTipPivotSize.y ) < - screenSize.y * 0.5f;

            // 画面をはみ出てしまう場合は位置を修正
            if( isOverlapRight )
            {
                posX -= ( 2 * itemPivotSizeX + toolTipPivotSize.x + 2 * TOOLTIP_WINDOW_SPACE_X );
            }
            if( isOverlapBottom )
            {
                posY += ( 2 * itemPivotSizeY + toolTipPivotSize.y + 2 * TOOLTIP_WINDOW_SPACE_Y );
            }

            _tooltipUi.SetPosition( new Vector2( posX, posY ) );
        }

        private void SetToolTipText( string text )
        {
            _tooltipUi.SetText( text );
        }
    }
}