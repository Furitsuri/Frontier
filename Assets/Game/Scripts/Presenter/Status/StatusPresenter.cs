using Frontier.Entities;
using Frontier.UI;
using System.Collections.Generic;
using System.Text;
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

            // 攻撃力・防御力のツールチップに、補正値がどの要因によって加算されているかを記述する
            var breakdown = new List<ModifierBreakdown.Entry>();
            chara.BattleParams.GetAtkBreakdown( chara.GetStatusRef, breakdown );
            string atkText = FormatBreakdown( breakdown );
            breakdown.Clear();
            chara.BattleParams.GetDefBreakdown( chara.GetStatusRef, breakdown );
            string defText = FormatBreakdown( breakdown );
            _statusUi.SetBonusBreakdownText( atkText, defText );

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
            int itemCount = _statusUi.GetStatusItemList().Count;

            _selectedItemIndex = ( _selectedItemIndex + itemCount ) % itemCount;

            var itemList = _statusUi.GetStatusItemList();
            if( _selectedItemIndex < 0 || itemList.Count <= _selectedItemIndex )
            {
                _statusUi.SetSelectCursorActive( false );
                return;
            }
            var item = itemList[_selectedItemIndex];
            _statusUi.SetSelectCursorRect( item.GetRectTransform().anchoredPosition, item.GetRectTransform().sizeDelta );

            // ツールチップのテキストと位置を設定(テキストに応じてウィンドウの縦幅が変わるため、テキストを先に設定する)
            SetToolTipText( item );
            SetToolTipPosition( item.GetRectTransform().anchoredPosition, item.GetRectTransform().sizeDelta, item.GetRectTransform().pivot );
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

            Vector2 toolTipSize     = _tooltipUi.GetSize();
            Vector2 toolTipPivot    = _tooltipUi.GetPivot();
            float toolTipRight      = posX + toolTipSize.x * ( 1 - toolTipPivot.x );   // ツールチップの右端
            float toolTipBottom     = posY - toolTipSize.y * toolTipPivot.y;           // ツールチップの下端

            var screenSize = _uiSystem.GeneralUi.GetScreenSize();

            bool isOverlapRight     = toolTipRight > screenSize.x * 0.5f;
            bool isOverlapBottom    = toolTipBottom < - screenSize.y * 0.5f;

            // 画面をはみ出てしまう場合は位置を修正
            if( isOverlapRight )
            {
                // 項目の左側に表示する
                posX -= ( 2 * itemPivotSizeX + toolTipSize.x + 2 * TOOLTIP_WINDOW_SPACE_X );
            }
            if( isOverlapBottom )
            {
                // 説明文の行数によって縦幅が変わるため、下端が画面の下端に揃うまで上へずらす
                posY += ( - screenSize.y * 0.5f ) - toolTipBottom;
            }

            _tooltipUi.SetPosition( new Vector2( posX, posY ) );
        }

        /// <summary>
        /// 補正値の内訳を「ATK UP : + 1」の形式で1要因1行の文字列にします。内訳が無い場合は空文字列を返します。
        /// </summary>
        static private string FormatBreakdown( List<ModifierBreakdown.Entry> breakdown )
        {
            var sb = new StringBuilder();
            foreach( var entry in breakdown )
            {
                if( 0 < sb.Length ) { sb.Append( '\n' ); }

                // SkillBoxUIと同様、スキルデータの名前(アンダースコア区切り)を表示用に整形する
                string name = entry.Name?.Replace( "_", " " ) ?? string.Empty;
                string sign = ( entry.Value < 0 ) ? "-" : "+";
                sb.Append( $"{name} : {sign} {Mathf.Abs( entry.Value )}" );
            }

            return sb.ToString();
        }

        private void SetToolTipText( ITooltipContent content )
        {
            var text = content.GetTooltipText();

            _tooltipUi.SetText( text );
        }
    }
}