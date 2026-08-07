using Frontier.Combat;
using TMPro;
using UnityEngine;
using Zenject;

namespace Frontier.UI
{
    /// <summary>
    /// キャラクター編集画面から遷移する装備スキル設定画面の見た目のみを担当するView。
    /// 左側に選択中キャラクターの装備枠(SkillBoxUIを流用し、上部パラメータ表示と同じ見た目・
    /// より大きいサイズで表示)+OK、右側に「スキルを外す」(常に一番上に固定)+所持スキル一覧
    /// (ID順、所持数0や選択中枠と同じスキルは選択不可の見た目にする)を表示する。
    /// 右側の所持スキル一覧もSkillBoxUIを流用しており、コスト等の情報を見た目だけでもある程度
    /// 把握できるようにしている(名前とカウントのみのプレーンテキストは廃止)。
    /// フォーカス中のスキルUIはSkillBoxUI.CursorFrameを使って外枠を縁取ることで示す
    /// (PlSelectSkillState等、戦闘中のスキル選択と同じ仕組み。拡大表示は行わず、外枠のみで示す)。
    /// 右側ウィンドウへ遷移した際は、左側のどの枠が編集対象かを示すため、対象の枠のみ通常表示・
    /// 他はグレーアウトする(SetLockedSlot)。この左右ペイン切り替えは同一画面内の遷移であり、
    /// ISelectableMenuView(CharacterEditUI等、確定して別画面へ遷移する「メニュー」向け)とは
    /// 意味合いが異なるため、あえて実装せずSkillEquipUI固有のメソッドとしている。
    /// 開閉・選択状態・割り振りの判断はSkillEquipPresenterが行い、このクラスは表示指示を
    /// 受けて反映するだけに留める。
    /// </summary>
    public class SkillEquipUI : UiMonoBehaviour
    {
        [Header( "左側: 装備枠(EQUIPABLE_SKILL_MAX_NUM個)" )]
        [SerializeField] private SkillBoxUI[] _slotBoxes;

        [Header( "左側: 決定(OK)項目" )]
        [SerializeField] private TextMeshProUGUI _okLabelText;

        [Header( "右側: 「スキルを外す」項目(常に一番上に固定表示)" )]
        [SerializeField] private TextMeshProUGUI _removeRowText;
        [SerializeField] private GameObject _removeRowFrame;

        [Header( "右側: 所持スキル一覧(SkillID.NUM個分をあらかじめ用意し、使わない分は非表示にする)" )]
        [SerializeField] private SkillBoxUI[] _inventorySkillBoxes;
        [SerializeField] private TextMeshProUGUI[] _inventoryCountTexts;

        [Header( "選択中スキルの説明文" )]
        [SerializeField] private GameObject _explanationPanel;
        [SerializeField] private TextMeshProUGUI _explanationText;

        [SerializeField] private Color _normalColor       = Color.white;
        [SerializeField] private Color _selectedColor     = Color.red;
        [SerializeField] private Color _unavailableColor  = Color.gray;
        [SerializeField] private Color _lockedColor       = Color.gray;

        [Inject] private ILocalizationService _localization = null;

        public override void Setup()
        {
            base.Setup();

            foreach ( var box in _slotBoxes ) { box.Setup(); }
            foreach ( var box in _inventorySkillBoxes ) { box.Setup(); }

            RefreshRemoveRowText();
            if ( _localization != null ) { _localization.OnLanguageChanged += RefreshRemoveRowText; }
        }

        private void OnDestroy()
        {
            if ( _localization != null ) { _localization.OnLanguageChanged -= RefreshRemoveRowText; }
        }

        private void RefreshRemoveRowText()
        {
            _removeRowText.text = _localization.Get( "UI_CMD_SKILL_REMOVE" );
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
        /// 左側(装備枠+OK)の通常のカーソル選択状態を反映します(0〜3:装備枠、4:OK、
        /// -1:左側は非選択(右側が選択中、またはまだ画面へ遷移していないプレビュー表示中))。
        /// ロック状態(SetLockedSlot)の解除にも使用します。
        /// </summary>
        public void SetSelectedSlot( int slotIndex )
        {
            for ( int i = 0; i < _slotBoxes.Length; ++i )
            {
                _slotBoxes[i].SetCursorHighlighted( i == slotIndex, scaleUp: false );
                _slotBoxes[i].SetUseableOrNot( true );
            }

            _okLabelText.color = ( slotIndex == _slotBoxes.Length ) ? _selectedColor : _normalColor;
        }

        /// <summary>
        /// activeSlotIndexの装備枠のみ通常表示のまま、他の枠(及びOK)をグレーアウトします。
        /// 右側ウィンドウへ遷移した際、左側でどの枠が編集対象かを分かりやすくするために使用します。
        /// </summary>
        public void SetLockedSlot( int activeSlotIndex )
        {
            for ( int i = 0; i < _slotBoxes.Length; ++i )
            {
                bool isActive = ( i == activeSlotIndex );
                _slotBoxes[i].SetCursorHighlighted( isActive, scaleUp: false );
                _slotBoxes[i].SetUseableOrNot( isActive );
            }

            _okLabelText.color = _lockedColor;
        }

        /// <summary>
        /// 「スキルを外す」項目のグレー表示(編集中の枠が元々未装備で、外す対象が無い場合)を切り替えます。
        /// </summary>
        public void SetRemoveRowUnavailable( bool unavailable )
        {
            _removeRowText.color = unavailable ? _unavailableColor : _normalColor;
        }

        /// <summary>
        /// 右側ウィンドウの選択状態(フォーカス外枠)を反映します
        /// (-1:「スキルを外す」を選択中、0以上:所持スキル一覧の行index、
        /// どちらでもない場合(左側が選択中)はSetRightSelectedRow(-2)相当として全て非表示にしてください)。
        /// </summary>
        public void SetRightSelectedRow( int inventoryRowIndex )
        {
            if ( _removeRowFrame != null ) { _removeRowFrame.SetActive( inventoryRowIndex == -1 ); }

            for ( int i = 0; i < _inventorySkillBoxes.Length; ++i )
            {
                _inventorySkillBoxes[i].SetCursorHighlighted( i == inventoryRowIndex, scaleUp: false );
            }
        }

        /// <summary>
        /// 右側の所持スキル一覧の行数(表示する分)を設定します。あらかじめ用意した行のうち、
        /// この件数を超える分は非表示にします。
        /// </summary>
        public void SetInventoryRowCount( int visibleCount )
        {
            for ( int i = 0; i < _inventorySkillBoxes.Length; ++i )
            {
                _inventorySkillBoxes[i].gameObject.SetActive( i < visibleCount );
            }
        }

        /// <summary>
        /// 右側の指定行の内容を設定します。unavailableがtrueの場合(所持数0、または左側で
        /// 選択中の枠と同じスキルの場合)はSkillBoxUI.SetUseableOrNot(false)でグレー表示にします
        /// (戦闘中の使用不可スキルと同じ暗転表現)。
        /// </summary>
        public void SetInventoryRow( int rowIndex, SkillID skillID, int count, bool unavailable )
        {
            _inventorySkillBoxes[rowIndex].ApplySkillForEditing( skillID );
            _inventorySkillBoxes[rowIndex].SetUseableOrNot( !unavailable );

            _inventoryCountTexts[rowIndex].text  = count.ToString();
            _inventoryCountTexts[rowIndex].color = unavailable ? _unavailableColor : _normalColor;
        }

        /// <summary>
        /// 指定スキルの説明文を表示します。無効なSkillIDの場合は非表示にします。
        /// </summary>
        public void SetExplanation( SkillID skillID )
        {
            if ( !SkillsData.IsValidSkill( skillID ) ) { HideExplanation(); return; }

            var textKey = SkillsData.data[( int ) skillID].ExplainTextKey;
            _explanationText.text = string.IsNullOrEmpty( textKey ) ? "" : _localization.Get( textKey );
            _explanationPanel.SetActive( true );
        }

        public void HideExplanation() => _explanationPanel.SetActive( false );
    }
}
