using Frontier.Combat;
using TMPro;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// ショップ画面(商品一覧)の見た目のみを担当するView。所持アニマは画面上部のヘッダー(GeneralHeaderUI)が表示する。
    /// 商品行はSkillBoxUIを流用し(SkillEquipUIの所持スキル一覧と同じ見た目)、右隣に価格と在庫数を表示する。
    /// どの商品を並べるか・購入可否によるグレー表示・カーソル位置の判断はShopPresenterが行い、
    /// このクラスは指示された内容をそのまま反映するだけに留める。
    /// </summary>
    public class ShopView : UiMonoBehaviour
    {
        [Header( "商品行(SkillID.NUM個分をあらかじめ用意し、使わない分は非表示にする)" )]
        [SerializeField] private SkillBoxUI[] _rowBoxes;
        [SerializeField] private TextMeshProUGUI[] _priceTexts;
        [SerializeField] private TextMeshProUGUI[] _stockTexts;

        [Header( "ウィンドウ背景(表示する行数に応じて高さを調整する)" )]
        [SerializeField] private RectTransform _windowRect;
        [SerializeField] private float _windowBottomPadding = 20f;

        [SerializeField] private Color _normalColor      = Color.white;
        [SerializeField] private Color _unavailableColor = Color.gray;

        public override void Setup()
        {
            base.Setup();

            foreach ( var box in _rowBoxes ) { box.Setup(); }
        }

        public void Show() => gameObject.SetActive( true );

        public void Hide() => gameObject.SetActive( false );

        /// <summary>
        /// 表示する商品行の数を設定します。あらかじめ用意した行のうち、この件数を超える分は非表示にし、
        /// 最後に表示する行の下端に合わせてウィンドウ背景の高さを調整します。
        /// </summary>
        public void SetRowCount( int visibleCount )
        {
            for ( int i = 0; i < _rowBoxes.Length; ++i )
            {
                _rowBoxes[i].gameObject.SetActive( i < visibleCount );
            }

            if ( visibleCount <= 0 || _windowRect == null ) { return; }

            var lastRow = _rowBoxes[Mathf.Min( visibleCount, _rowBoxes.Length ) - 1].GetComponent<RectTransform>();
            float height = -lastRow.anchoredPosition.y + lastRow.sizeDelta.y + _windowBottomPadding;
            _windowRect.SetSizeWithCurrentAnchors( RectTransform.Axis.Vertical, height );
        }

        /// <summary>
        /// 指定行にスキル商品の内容を設定します。unavailableがtrueの場合(在庫切れ・アニマ不足等)は
        /// SkillBoxUI.SetUseableOrNot(false)と、価格・在庫テキストのグレー表示で購入不可であることを示します。
        /// </summary>
        public void SetRowSkill( int rowIndex, SkillID skillID, int price, int stock, bool unavailable )
        {
            _rowBoxes[rowIndex].ApplySkillForEditing( skillID );
            _rowBoxes[rowIndex].SetUseableOrNot( !unavailable );

            var color = unavailable ? _unavailableColor : _normalColor;
            _priceTexts[rowIndex].text  = price.ToString();
            _priceTexts[rowIndex].color = color;
            _stockTexts[rowIndex].text  = $"x{stock}";
            _stockTexts[rowIndex].color = color;
        }

        /// <summary>
        /// カーソルが指している行をフォーカス外枠で示します(-1で全行のフォーカスを外します)。
        /// </summary>
        public void SetSelectedRow( int rowIndex )
        {
            for ( int i = 0; i < _rowBoxes.Length; ++i )
            {
                _rowBoxes[i].SetCursorHighlighted( i == rowIndex, scaleUp: false );
            }
        }
    }
}
