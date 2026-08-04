using TMPro;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// キャラクター編集画面から遷移するレベルアップ画面の見た目のみを担当するView。
    /// エルデンリング/ダークソウルのレベルアップ画面を踏襲し、レベル・所持ポイント・
    /// 次のレベルに必要なポイント、及び能力値(MaxHP/Atk/Def)を上下カーソルで選択、
    /// 左右で仮に割り振り/取り消しできるようにする。
    /// 開閉・選択状態・割り振りの判断はLevelUpPresenterが行い、このクラスは表示指示を
    /// 受けて反映するだけに留める。
    /// </summary>
    public class LevelUpUI : UiMonoBehaviour
    {
        [Header( "レベル(現在/仮)" )]
        [SerializeField] private TextMeshProUGUI _levelCurrentText;
        [SerializeField] private TextMeshProUGUI _levelTentativeText;

        [Header( "所持ポイント(仮称。現在/仮)" )]
        [SerializeField] private TextMeshProUGUI _expCurrentText;
        [SerializeField] private TextMeshProUGUI _expTentativeText;

        [Header( "次のレベルに必要なポイント(不足時は赤色にする)" )]
        [SerializeField] private TextMeshProUGUI _requiredCostText;

        [Header( "MaxHP行(ラベル/現在値/仮の値)" )]
        [SerializeField] private TextMeshProUGUI _maxHpLabelText;
        [SerializeField] private TextMeshProUGUI _maxHpCurrentText;
        [SerializeField] private TextMeshProUGUI _maxHpTentativeText;

        [Header( "Atk行(ラベル/現在値/仮の値)" )]
        [SerializeField] private TextMeshProUGUI _atkLabelText;
        [SerializeField] private TextMeshProUGUI _atkCurrentText;
        [SerializeField] private TextMeshProUGUI _atkTentativeText;

        [Header( "Def行(ラベル/現在値/仮の値)" )]
        [SerializeField] private TextMeshProUGUI _defLabelText;
        [SerializeField] private TextMeshProUGUI _defCurrentText;
        [SerializeField] private TextMeshProUGUI _defTentativeText;

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
        /// 次のレベルに必要なポイントを表示します。所持ポイントが不足している場合は赤色にします。
        /// </summary>
        public void SetRequiredCost( int cost, bool insufficient )
        {
            _requiredCostText.text  = cost.ToString();
            _requiredCostText.color = insufficient ? _insufficientColor : _normalColor;
        }

        public void SetMaxHpValues( int current, int tentative )
        {
            _maxHpCurrentText.text   = current.ToString();
            _maxHpTentativeText.text = tentative.ToString();
        }

        public void SetAtkValues( int current, int tentative )
        {
            _atkCurrentText.text   = current.ToString();
            _atkTentativeText.text = tentative.ToString();
        }

        public void SetDefValues( int current, int tentative )
        {
            _defCurrentText.text   = current.ToString();
            _defTentativeText.text = tentative.ToString();
        }

        /// <summary>
        /// 選択中の行を表示(ラベルの色)に反映します(0:MaxHP, 1:Atk, 2:Def, 3:OK)。
        /// </summary>
        public void SetSelectedRow( int rowIndex )
        {
            _maxHpLabelText.color = ( rowIndex == 0 ) ? _selectedColor : _normalColor;
            _atkLabelText.color   = ( rowIndex == 1 ) ? _selectedColor : _normalColor;
            _defLabelText.color   = ( rowIndex == 2 ) ? _selectedColor : _normalColor;
            _okLabelText.color    = ( rowIndex == 3 ) ? _selectedColor : _normalColor;
        }
    }
}
