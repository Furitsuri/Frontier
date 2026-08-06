using TMPro;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// キャラクター編集画面から遷移するレベルアップ画面の見た目のみを担当するView。
    /// 能力値の成長はステータス上昇画面(StatusUpUI)の役割のため、このウィンドウは
    /// レベル・所持EXP・次のレベルに必要なEXP・そのレベルまで上げた場合のStatusPointのみを扱う。
    /// カーソルで選択できるのはLEVEL行のみで、左右で仮のレベルを上げ下げする
    /// (現在の実レベルより下げることはできず、最大レベルは99)。
    /// 開閉・選択状態・割り振りの判断はLevelUpPresenterが行い、このクラスは表示指示を
    /// 受けて反映するだけに留める。
    /// </summary>
    public class LevelUpUI : UiMonoBehaviour
    {
        [Header( "レベル(ラベル/現在/仮)" )]
        [SerializeField] private TextMeshProUGUI _levelLabelText;
        [SerializeField] private TextMeshProUGUI _levelCurrentText;
        [SerializeField] private TextMeshProUGUI _levelTentativeText;

        [Header( "所持EXP(現在/仮)" )]
        [SerializeField] private TextMeshProUGUI _expCurrentText;
        [SerializeField] private TextMeshProUGUI _expTentativeText;

        [Header( "次のレベルに必要なEXP(不足時は赤色にする)" )]
        [SerializeField] private TextMeshProUGUI _requiredCostText;

        [Header( "StatusPoint(現在/仮)" )]
        [SerializeField] private TextMeshProUGUI _statusPointCurrentText;
        [SerializeField] private TextMeshProUGUI _statusPointTentativeText;

        [Header( "決定(OK)項目" )]
        [SerializeField] private TextMeshProUGUI _okLabelText;

        [SerializeField] private Color _normalColor       = Color.white;
        [SerializeField] private Color _selectedColor     = Color.red;
        [SerializeField] private Color _insufficientColor = Color.red;

        public void Show() => gameObject.SetActive( true );

        public void Hide() => gameObject.SetActive( false );

        public void SetLevelValues( int current, int tentative )
        {
            _levelCurrentText.text   = current.ToString();
            _levelTentativeText.text = tentative.ToString();
        }

        public void SetExpValues( int current, int tentative )
        {
            _expCurrentText.text   = current.ToString();
            _expTentativeText.text = tentative.ToString();
        }

        /// <summary>
        /// 次のレベルに必要なEXPを表示します。所持EXPが不足している場合は赤色にします。
        /// </summary>
        public void SetRequiredCost( int cost, bool insufficient )
        {
            _requiredCostText.text  = cost.ToString();
            _requiredCostText.color = insufficient ? _insufficientColor : _normalColor;
        }

        public void SetStatusPointValues( int current, int tentative )
        {
            _statusPointCurrentText.text   = current.ToString();
            _statusPointTentativeText.text = tentative.ToString();
        }

        /// <summary>
        /// 選択中の行を表示(ラベルの色)に反映します(0:LEVEL, 1:OK)。
        /// </summary>
        public void SetSelectedRow( int rowIndex )
        {
            _levelLabelText.color = ( rowIndex == 0 ) ? _selectedColor : _normalColor;
            _okLabelText.color    = ( rowIndex == 1 ) ? _selectedColor : _normalColor;
        }
    }
}
