using Frontier.Combat;
using TMPro;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// キャラクター編集画面から遷移する装備スキル設定画面の見た目のみを担当するView。
    /// 左側に選択中キャラクターの装備枠(SkillBoxUIを流用し、上部パラメータ表示と同じ見た目・
    /// より大きいサイズで表示)+OK、右側に所持スキル一覧(ID順、所持数0や選択中枠と同じスキルは
    /// 選択不可の見た目にする)を表示する。
    /// 開閉・選択状態・割り振りの判断はSkillEquipPresenterが行い、このクラスは表示指示を
    /// 受けて反映するだけに留める。
    /// </summary>
    public class SkillEquipUI : UiMonoBehaviour
    {
        [Header( "左側: 装備枠(EQUIPABLE_SKILL_MAX_NUM個)" )]
        [SerializeField] private SkillBoxUI[] _slotBoxes;

        [Header( "左側: 決定(OK)項目" )]
        [SerializeField] private TextMeshProUGUI _okLabelText;

        [Header( "右側: 所持スキル一覧の行(SkillID.NUM個分をあらかじめ用意し、使わない行は非表示にする)" )]
        [SerializeField] private GameObject[] _inventoryRows;
        [SerializeField] private TextMeshProUGUI[] _inventoryNameTexts;
        [SerializeField] private TextMeshProUGUI[] _inventoryCountTexts;

        [SerializeField] private Color _normalColor       = Color.white;
        [SerializeField] private Color _selectedColor     = Color.red;
        [SerializeField] private Color _unavailableColor  = Color.gray;

        public override void Setup()
        {
            base.Setup();

            foreach ( var box in _slotBoxes ) { box.Setup(); }
        }

        public void Show() => gameObject.SetActive( true );

        public void Hide() => gameObject.SetActive( false );

        /// <summary>
        /// 指定枠に表示するスキルを設定します(未装備の場合はSkillID.NONEを渡してください)。
        /// </summary>
        public void SetSlotSkill( int slotIndex, SkillID skillID )
        {
            _slotBoxes[slotIndex].ApplySkillForEditing( skillID );
        }

        /// <summary>
        /// 左側(装備枠+OK)の選択状態を反映します(0〜3:装備枠、4:OK、-1:左側は非選択(右側が選択中))。
        /// </summary>
        public void SetLeftSelectedRow( int rowIndex )
        {
            for ( int i = 0; i < _slotBoxes.Length; ++i )
            {
                _slotBoxes[i].SetCursorHighlighted( i == rowIndex );
            }

            _okLabelText.color = ( rowIndex == _slotBoxes.Length ) ? _selectedColor : _normalColor;
        }

        /// <summary>
        /// 右側の所持スキル一覧の行数(表示する分)を設定します。あらかじめ用意した行のうち、
        /// この件数を超える分は非表示にします。
        /// </summary>
        public void SetInventoryRowCount( int visibleCount )
        {
            for ( int i = 0; i < _inventoryRows.Length; ++i )
            {
                _inventoryRows[i].SetActive( i < visibleCount );
            }
        }

        /// <summary>
        /// 右側の指定行の内容を設定します。unavailableがtrueの場合(所持数0、または左側で
        /// 選択中の枠と同じスキルの場合)はグレー表示にします。
        /// </summary>
        public void SetInventoryRow( int rowIndex, string skillName, SituationType situationType, int count, bool unavailable )
        {
            Color[] typeColor = { Color.red, new Color( 0.1f, 0.6f, 1.0f ), Color.yellow };

            var color = unavailable ? _unavailableColor : typeColor[( int ) situationType];
            _inventoryNameTexts[rowIndex].text  = skillName?.Replace( "_", " " ) ?? "";
            _inventoryNameTexts[rowIndex].color = color;
            _inventoryCountTexts[rowIndex].text  = count.ToString();
            _inventoryCountTexts[rowIndex].color = color;
        }

        /// <summary>
        /// 右側一覧の選択状態を反映します(-1で非選択(左側が選択中))。
        /// 選択中行は文字色を選択色にします(unavailableな行が選択されることは想定していません)。
        /// </summary>
        public void SetInventorySelectedRow( int rowIndex )
        {
            for ( int i = 0; i < _inventoryRows.Length; ++i )
            {
                bool isSelected = ( i == rowIndex );
                _inventoryNameTexts[i].fontStyle = isSelected ? FontStyles.Underline : FontStyles.Normal;
                _inventoryCountTexts[i].fontStyle = isSelected ? FontStyles.Underline : FontStyles.Normal;
            }
        }
    }
}
