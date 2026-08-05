using TMPro;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// キャラクター編集画面から遷移するステータス上昇画面の見た目のみを担当するView。
    /// LevelUpUIを踏襲しつつ、レベル・所持ポイント・次のレベルに必要なポイントの表示は
    /// 現在/割り振り後のStatusPoint表示に置き換え、能力値もMaxHP/Atk/Defに加えて
    /// MoveRange/JumpForce/MaxActionGaugeの6種を対象とする(各行に1ポイント上げるために
    /// 必要なStatusPointの表示を追加する)。
    /// 開閉・選択状態・割り振りの判断はStatusUpPresenterが行い、このクラスは表示指示を
    /// 受けて反映するだけに留める。
    /// </summary>
    public class StatusUpUI : UiMonoBehaviour
    {
        [Header( "SP(現在/仮)" )]
        [SerializeField] private TextMeshProUGUI _spCurrentText;
        [SerializeField] private TextMeshProUGUI _spTentativeText;

        [Header( "MaxHP行(ラベル/現在値/仮の値/必要SP)" )]
        [SerializeField] private TextMeshProUGUI _maxHpLabelText;
        [SerializeField] private TextMeshProUGUI _maxHpCurrentText;
        [SerializeField] private TextMeshProUGUI _maxHpTentativeText;
        [SerializeField] private TextMeshProUGUI _maxHpCostText;

        [Header( "Atk行(ラベル/現在値/仮の値/必要SP)" )]
        [SerializeField] private TextMeshProUGUI _atkLabelText;
        [SerializeField] private TextMeshProUGUI _atkCurrentText;
        [SerializeField] private TextMeshProUGUI _atkTentativeText;
        [SerializeField] private TextMeshProUGUI _atkCostText;

        [Header( "Def行(ラベル/現在値/仮の値/必要SP)" )]
        [SerializeField] private TextMeshProUGUI _defLabelText;
        [SerializeField] private TextMeshProUGUI _defCurrentText;
        [SerializeField] private TextMeshProUGUI _defTentativeText;
        [SerializeField] private TextMeshProUGUI _defCostText;

        [Header( "MoveRange行(ラベル/現在値/仮の値/必要SP)" )]
        [SerializeField] private TextMeshProUGUI _moveRangeLabelText;
        [SerializeField] private TextMeshProUGUI _moveRangeCurrentText;
        [SerializeField] private TextMeshProUGUI _moveRangeTentativeText;
        [SerializeField] private TextMeshProUGUI _moveRangeCostText;

        [Header( "JumpForce行(ラベル/現在値/仮の値/必要SP)" )]
        [SerializeField] private TextMeshProUGUI _jumpForceLabelText;
        [SerializeField] private TextMeshProUGUI _jumpForceCurrentText;
        [SerializeField] private TextMeshProUGUI _jumpForceTentativeText;
        [SerializeField] private TextMeshProUGUI _jumpForceCostText;

        [Header( "MaxActionGauge行(ラベル/現在値/仮の値/必要SP)" )]
        [SerializeField] private TextMeshProUGUI _maxActionGaugeLabelText;
        [SerializeField] private TextMeshProUGUI _maxActionGaugeCurrentText;
        [SerializeField] private TextMeshProUGUI _maxActionGaugeTentativeText;
        [SerializeField] private TextMeshProUGUI _maxActionGaugeCostText;

        [Header( "RecoveryActionGauge行(ラベル/現在値/仮の値/必要SP)" )]
        [SerializeField] private TextMeshProUGUI _recoveryActionGaugeLabelText;
        [SerializeField] private TextMeshProUGUI _recoveryActionGaugeCurrentText;
        [SerializeField] private TextMeshProUGUI _recoveryActionGaugeTentativeText;
        [SerializeField] private TextMeshProUGUI _recoveryActionGaugeCostText;

        [Header( "決定(OK)項目" )]
        [SerializeField] private TextMeshProUGUI _okLabelText;

        [SerializeField] private Color _normalColor       = Color.white;
        [SerializeField] private Color _selectedColor     = Color.red;
        [SerializeField] private Color _insufficientColor = Color.red;

        public void Show() => gameObject.SetActive( true );

        public void Hide() => gameObject.SetActive( false );

        public void SetSpValues( int current, int tentative )
        {
            _spCurrentText.text   = current.ToString();
            _spTentativeText.text = tentative.ToString();
        }

        public void SetMaxHpValues( int current, int tentative, int cost, bool insufficient, bool isMaxed )
            => SetRow( _maxHpCurrentText, _maxHpTentativeText, _maxHpCostText, current, tentative, cost, insufficient, isMaxed );

        public void SetAtkValues( int current, int tentative, int cost, bool insufficient, bool isMaxed )
            => SetRow( _atkCurrentText, _atkTentativeText, _atkCostText, current, tentative, cost, insufficient, isMaxed );

        public void SetDefValues( int current, int tentative, int cost, bool insufficient, bool isMaxed )
            => SetRow( _defCurrentText, _defTentativeText, _defCostText, current, tentative, cost, insufficient, isMaxed );

        public void SetMoveRangeValues( int current, int tentative, int cost, bool insufficient, bool isMaxed )
            => SetRow( _moveRangeCurrentText, _moveRangeTentativeText, _moveRangeCostText, current, tentative, cost, insufficient, isMaxed );

        public void SetJumpForceValues( int current, int tentative, int cost, bool insufficient, bool isMaxed )
            => SetRow( _jumpForceCurrentText, _jumpForceTentativeText, _jumpForceCostText, current, tentative, cost, insufficient, isMaxed );

        public void SetMaxActionGaugeValues( int current, int tentative, int cost, bool insufficient, bool isMaxed )
            => SetRow( _maxActionGaugeCurrentText, _maxActionGaugeTentativeText, _maxActionGaugeCostText, current, tentative, cost, insufficient, isMaxed );

        public void SetRecoveryActionGaugeValues( int current, int tentative, int cost, bool insufficient, bool isMaxed )
            => SetRow( _recoveryActionGaugeCurrentText, _recoveryActionGaugeTentativeText, _recoveryActionGaugeCostText, current, tentative, cost, insufficient, isMaxed );

        /// <summary>
        /// isMaxedがtrueの場合、既に最大値に達しているものとして必要SPの代わりに"(-)"を表示します。
        /// </summary>
        private void SetRow( TextMeshProUGUI currentText, TextMeshProUGUI tentativeText, TextMeshProUGUI costText, int current, int tentative, int cost, bool insufficient, bool isMaxed )
        {
            currentText.text   = current.ToString();
            tentativeText.text = tentative.ToString();
            costText.text      = isMaxed ? "(-)" : $"({cost})";
            costText.color     = ( !isMaxed && insufficient ) ? _insufficientColor : _normalColor;
        }

        /// <summary>
        /// 選択中の行を表示(ラベルの色)に反映します
        /// (0:MaxHP, 1:Atk, 2:Def, 3:MoveRange, 4:JumpForce, 5:MaxActionGauge, 6:RecoveryActionGauge, 7:OK)。
        /// </summary>
        public void SetSelectedRow( int rowIndex )
        {
            _maxHpLabelText.color               = ( rowIndex == 0 ) ? _selectedColor : _normalColor;
            _atkLabelText.color                 = ( rowIndex == 1 ) ? _selectedColor : _normalColor;
            _defLabelText.color                 = ( rowIndex == 2 ) ? _selectedColor : _normalColor;
            _moveRangeLabelText.color           = ( rowIndex == 3 ) ? _selectedColor : _normalColor;
            _jumpForceLabelText.color           = ( rowIndex == 4 ) ? _selectedColor : _normalColor;
            _maxActionGaugeLabelText.color      = ( rowIndex == 5 ) ? _selectedColor : _normalColor;
            _recoveryActionGaugeLabelText.color = ( rowIndex == 6 ) ? _selectedColor : _normalColor;
            _okLabelText.color                  = ( rowIndex == 7 ) ? _selectedColor : _normalColor;
        }
    }
}
