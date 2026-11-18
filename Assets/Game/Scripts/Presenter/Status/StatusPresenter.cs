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
            SetToolTipPosition( item.GetRectTransform() );
        }

        public bool IsToolTipActive()
        {
            return _tooltipUi.gameObject.activeSelf;
        }

        /// <summary>
        /// ツールチップを、選択中の項目の横に配置します。
        /// 説明文の量やウィンドウの大きさによらず、画面外へのはみ出しや入力ガイドとの重なりが起きない位置に調整します。
        /// </summary>
        private void SetToolTipPosition( RectTransform itemRect )
        {
            var bounds = CalculateToolTipBounds();
            var rect   = TooltipPlacement.Calculate( _tooltipUi.ToLocalRect( itemRect ), _tooltipUi.GetSize(), bounds, new Vector2( TOOLTIP_WINDOW_SPACE_X, TOOLTIP_WINDOW_SPACE_Y ) );

            _tooltipUi.SetLocalRect( rect );
        }

        /// <summary>
        /// ツールチップを表示してよい範囲を求めます。画面から余白を除き、入力ガイドが表示されている場合はその上端より上に限ります。
        /// MEMO : 入力ガイドは表示内容に応じて横幅がアニメーションしながら変わるため、横方向の範囲によらず、
        ///        ガイドの上端より下の帯全体を表示不可とします(表示中にガイドの幅が変わっても重ならないようにするため)。
        /// </summary>
        private Rect CalculateToolTipBounds()
        {
            var screenRect = _tooltipUi.ToLocalRect( _uiSystem.GeneralUi.GetComponentInParent<Canvas>().GetComponent<RectTransform>() );
            float xMin = screenRect.xMin + TOOLTIP_WINDOW_SCREEN_MARGIN;
            float xMax = screenRect.xMax - TOOLTIP_WINDOW_SCREEN_MARGIN;
            float yMin = screenRect.yMin + TOOLTIP_WINDOW_SCREEN_MARGIN;
            float yMax = screenRect.yMax - TOOLTIP_WINDOW_SCREEN_MARGIN;

            var guide = _uiSystem.GeneralUi.InputGuideView;
            if( guide != null && guide.isActiveAndEnabled && guide.BackgroundRect != null )
            {
                var guideRect = _tooltipUi.ToLocalRect( guide.BackgroundRect );
                yMin = Mathf.Max( yMin, guideRect.yMax + TOOLTIP_WINDOW_SCREEN_MARGIN );
            }

            return Rect.MinMaxRect( xMin, yMin, xMax, yMax );
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