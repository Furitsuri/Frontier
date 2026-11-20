using Frontier.Combat;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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
    /// 左側の行番号は「アクティブ枠(_slotBoxes) → パッシブ枠(_passiveSlotBoxes) → OK」の順に通しで扱う
    /// (SkillEquipPresenterの左側の行番号と一致させている)。
    /// </summary>
    public class SkillEquipUI : UiMonoBehaviour
    {
        /// <summary>
        /// アクションゲージ残量表示の目盛り1つ分の表示状態です。
        /// </summary>
        public enum ActionGaugePipState
        {
            Hidden = 0,     // 素のゲージ最大値を超える分(表示しない)
            Available,      // 残っているゲージ
            Consumed,       // パッシブスキルの装備コストとして消費しているゲージ
            WillConsume,    // フォーカス中のパッシブスキルを装備すると消費されるゲージ(点滅)
            WillRestore,    // フォーカス中の操作でパッシブスキルを外す(差し替える)と戻るゲージ(点滅)
        }

        [Header( "左側: アクティブスキルの装備枠(EQUIPABLE_ACTIVE_SKILL_MAX_NUM個)" )]
        [SerializeField] private SkillBoxUI[] _slotBoxes;

        [Header( "左側: パッシブスキルの装備枠(EQUIPABLE_PASSIVE_SKILL_MAX_NUM個)" )]
        [SerializeField] private SkillBoxUI[] _passiveSlotBoxes;

        [Header( "左側: 決定(OK)項目" )]
        [SerializeField] private TextMeshProUGUI _okLabelText;

        [Header( "右側: 「スキルを外す」項目(常に一番上に固定表示)" )]
        [SerializeField] private TextMeshProUGUI _removeRowText;
        [SerializeField] private GameObject _removeRowFrame;

        [Header( "右側: 所持スキル一覧の表示行(一度に表示する行数分。所持スキルがこれより多い場合はスクロールして表示する)" )]
        [SerializeField] private SkillBoxUI[] _inventorySkillBoxes;
        [SerializeField] private TextMeshProUGUI[] _inventoryCountTexts;

        [Header( "右側: 所持スキル一覧の上下に続きがあることを示す表示" )]
        [SerializeField] private GameObject _scrollUpIndicator;
        [SerializeField] private GameObject _scrollDownIndicator;

        [Header( "アクションゲージの残量表示(目盛りはテンプレートをACTION_GAUGE_MAX個複製してコンテナへ並べる)" )]
        [SerializeField] private RawImage _actGaugePipTemplate;
        [SerializeField] private Transform _actGaugePipContainer;
        [SerializeField] private TextMeshProUGUI _actGaugeValueText;
        [SerializeField] private Color _pipAvailableColor   = Color.green;
        [SerializeField] private Color _pipConsumedColor    = Color.gray;
        [SerializeField] private float _pipBlinkDuration    = 0.5f;

        [Header( "警告表示(装備によって使用出来なくなるアクティブスキルがある場合等)" )]
        [SerializeField] private GameObject _warningPanel;
        [SerializeField] private TextMeshProUGUI _warningText;

        [Header( "選択中スキルの説明文" )]
        [SerializeField] private GameObject _explanationPanel;
        [SerializeField] private TextMeshProUGUI _explanationText;

        [SerializeField] private Color _normalColor       = Color.white;
        [SerializeField] private Color _selectedColor     = Color.red;
        [SerializeField] private Color _unavailableColor  = Color.gray;
        [SerializeField] private Color _lockedColor       = Color.gray;

        [Inject] private ILocalizationService _localization = null;

        // 左側の行番号順(アクティブ枠→パッシブ枠)に並べた装備枠。要素数がそのままOK行の行番号になる
        private SkillBoxUI[] _leftRowBoxes = System.Array.Empty<SkillBoxUI>();

        // 左側の各枠のスキルが使用可能か(アクションゲージ最大値の不足で使用出来ない場合はグレー表示する)
        private bool[] _leftRowUseable = System.Array.Empty<bool>();

        private readonly List<RawImage> _actGaugePips = new List<RawImage>( Constants.ACTION_GAUGE_MAX );
        private readonly List<ActionGaugePipState> _actGaugePipStates = new List<ActionGaugePipState>( Constants.ACTION_GAUGE_MAX );
        private float _pipBlinkElapsedTime = 0f;

        public override void Setup()
        {
            base.Setup();

            _leftRowBoxes = new SkillBoxUI[_slotBoxes.Length + _passiveSlotBoxes.Length];
            _slotBoxes.CopyTo( _leftRowBoxes, 0 );
            _passiveSlotBoxes.CopyTo( _leftRowBoxes, _slotBoxes.Length );

            _leftRowUseable = new bool[_leftRowBoxes.Length];
            for ( int i = 0; i < _leftRowUseable.Length; ++i ) { _leftRowUseable[i] = true; }

            foreach ( var box in _leftRowBoxes ) { box.Setup(); }
            foreach ( var box in _inventorySkillBoxes ) { box.Setup(); }

            SetupActionGaugePips();
            HideWarning();

            RefreshRemoveRowText();
            if ( _localization != null ) { _localization.OnLanguageChanged += RefreshRemoveRowText; }
        }

        private void SetupActionGaugePips()
        {
            if ( _actGaugePipTemplate == null || _actGaugePipContainer == null || 0 < _actGaugePips.Count ) { return; }

            _actGaugePipTemplate.gameObject.SetActive( false );
            for ( int i = 0; i < Constants.ACTION_GAUGE_MAX; ++i )
            {
                var pip = Instantiate( _actGaugePipTemplate, _actGaugePipContainer, false );
                pip.name = $"{_actGaugePipTemplate.name}_{i}";
                pip.gameObject.SetActive( false );
                _actGaugePips.Add( pip );
                _actGaugePipStates.Add( ActionGaugePipState.Hidden );
            }
        }

        private void Update()
        {
            // 増減するゲージの目盛りを点滅させる(戦闘中のゲージ消費時の点滅と同じ表現)
            _pipBlinkElapsedTime += Time.unscaledDeltaTime;
            float alpha = ( 0f < _pipBlinkDuration ) ? Mathf.PingPong( _pipBlinkElapsedTime / _pipBlinkDuration, 1.0f ) : 1.0f;

            for ( int i = 0; i < _actGaugePips.Count; ++i )
            {
                var state = _actGaugePipStates[i];
                if ( state != ActionGaugePipState.WillConsume && state != ActionGaugePipState.WillRestore ) { continue; }

                var color = _actGaugePips[i].color;
                color.a = alpha;
                _actGaugePips[i].color = color;
            }
        }

        private void OnDestroy()
        {
            if ( _localization != null ) { _localization.OnLanguageChanged -= RefreshRemoveRowText; }
        }

        private void RefreshRemoveRowText()
        {
            _removeRowText.text = _localization.Get( LocKey.UI_CMD_SKILL_REMOVE );
        }

        public void Show() => gameObject.SetActive( true );

        /// <summary>
        /// 右側の所持スキル一覧で、一度に表示出来る行数です。
        /// </summary>
        public int VisibleInventoryRowCount => _inventorySkillBoxes.Length;

        public void Hide() => gameObject.SetActive( false );

        /// <summary>
        /// 左側の指定行の枠に表示するアクティブスキルを設定します(未装備の場合はActiveSkillID.NONEを渡してください)。
        /// </summary>
        public void SetSlotSkill( int row, ActiveSkillID skillID )
        {
            _leftRowBoxes[row].ApplySkillForEditing( skillID );
        }

        /// <summary>
        /// 左側の指定行の枠に表示するパッシブスキルを設定します(未装備の場合はPassiveSkillID.NONEを渡してください)。
        /// </summary>
        public void SetSlotSkill( int row, PassiveSkillID skillID )
        {
            _leftRowBoxes[row].ApplyPassiveSkillForEditing( skillID );
        }

        /// <summary>
        /// 左側(装備枠+OK)の通常のカーソル選択状態を反映します(0〜:装備枠(アクティブ→パッシブの順)、末尾:OK、
        /// -1:左側は非選択(右側が選択中、またはまだ画面へ遷移していないプレビュー表示中))。
        /// ロック状態(SetLockedSlot)の解除にも使用します。
        /// </summary>
        public void SetSelectedSlot( int slotIndex )
        {
            for ( int i = 0; i < _leftRowBoxes.Length; ++i )
            {
                _leftRowBoxes[i].SetCursorHighlighted( i == slotIndex, scaleUp: false );
                _leftRowBoxes[i].SetUseableOrNot( _leftRowUseable[i] );
            }

            _okLabelText.color = ( slotIndex == _leftRowBoxes.Length ) ? _selectedColor : _normalColor;
        }

        /// <summary>
        /// 左側の指定行の枠のスキルが使用可能かを設定します。使用出来ない場合はグレー表示にします
        /// (右側ウィンドウの操作中(SetLockedSlot)は、編集対象以外の枠は使用可否によらずグレー表示になります)。
        /// </summary>
        public void SetSlotUseable( int row, bool useable )
        {
            _leftRowUseable[row] = useable;
        }

        /// <summary>
        /// アクションゲージの残量表示を設定します。
        /// </summary>
        /// <param name="pipStates">目盛りごとの表示状態(ACTION_GAUGE_MAX個)</param>
        /// <param name="remaining">残りゲージ(パッシブスキルの装備コストを差し引いた最大値)</param>
        /// <param name="baseMax">素のゲージ最大値</param>
        public void SetActionGauge( IReadOnlyList<ActionGaugePipState> pipStates, int remaining, int baseMax )
        {
            for ( int i = 0; i < _actGaugePips.Count; ++i )
            {
                var state = ( i < pipStates.Count ) ? pipStates[i] : ActionGaugePipState.Hidden;
                _actGaugePipStates[i] = state;

                var pip = _actGaugePips[i];
                pip.gameObject.SetActive( state != ActionGaugePipState.Hidden );
                // 消費されるゲージは残っている色で、戻るゲージは消費中の色で点滅させる
                pip.color = ( state == ActionGaugePipState.Available || state == ActionGaugePipState.WillConsume ) ? _pipAvailableColor : _pipConsumedColor;
            }

            _pipBlinkElapsedTime = 0f;

            if ( _actGaugeValueText != null ) { _actGaugeValueText.text = $"{remaining} / {baseMax}"; }
        }

        /// <summary>
        /// 警告文を表示します。
        /// </summary>
        public void ShowWarning( string text )
        {
            if ( _warningText != null ) { _warningText.text = text; }
            if ( _warningPanel != null ) { _warningPanel.SetActive( true ); }
        }

        public void HideWarning()
        {
            if ( _warningPanel != null ) { _warningPanel.SetActive( false ); }
        }

        /// <summary>
        /// activeSlotIndexの装備枠のみ通常表示のまま、他の枠(及びOK)をグレーアウトします。
        /// 右側ウィンドウへ遷移した際、左側でどの枠が編集対象かを分かりやすくするために使用します。
        /// </summary>
        public void SetLockedSlot( int activeSlotIndex )
        {
            for ( int i = 0; i < _leftRowBoxes.Length; ++i )
            {
                bool isActive = ( i == activeSlotIndex );
                _leftRowBoxes[i].SetCursorHighlighted( isActive, scaleUp: false );
                _leftRowBoxes[i].SetUseableOrNot( isActive );
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
        /// (-1:「スキルを外す」を選択中、0以上:所持スキル一覧の表示行index(スクロール位置を差し引いた、画面上の行)、
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
        /// この件数を超える分は非表示にします(VisibleInventoryRowCountを超える値は指定しないでください)。
        /// </summary>
        public void SetInventoryRowCount( int visibleCount )
        {
            for ( int i = 0; i < _inventorySkillBoxes.Length; ++i )
            {
                _inventorySkillBoxes[i].gameObject.SetActive( i < visibleCount );
            }
        }

        /// <summary>
        /// 右側の指定表示行の内容を設定します。unavailableがtrueの場合(所持数0、または左側で
        /// 選択中の枠と同じスキルの場合)はSkillBoxUI.SetUseableOrNot(false)でグレー表示にします
        /// (戦闘中の使用不可スキルと同じ暗転表現)。
        /// </summary>
        public void SetInventoryRow( int rowIndex, ActiveSkillID skillID, int count, bool unavailable )
        {
            _inventorySkillBoxes[rowIndex].ApplySkillForEditing( skillID );
            ApplyInventoryRowState( rowIndex, count, unavailable );
        }

        /// <summary>
        /// SetInventoryRowのパッシブスキル版です。
        /// </summary>
        public void SetInventoryRow( int rowIndex, PassiveSkillID skillID, int count, bool unavailable )
        {
            _inventorySkillBoxes[rowIndex].ApplyPassiveSkillForEditing( skillID );
            ApplyInventoryRowState( rowIndex, count, unavailable );
        }

        /// <summary>
        /// 所持スキル一覧の上下に、表示しきれていない続きがあることを示す表示を切り替えます。
        /// </summary>
        public void SetScrollIndicators( bool hasAbove, bool hasBelow )
        {
            if ( _scrollUpIndicator != null )   { _scrollUpIndicator.SetActive( hasAbove ); }
            if ( _scrollDownIndicator != null ) { _scrollDownIndicator.SetActive( hasBelow ); }
        }

        private void ApplyInventoryRowState( int rowIndex, int count, bool unavailable )
        {
            _inventorySkillBoxes[rowIndex].SetUseableOrNot( !unavailable );

            _inventoryCountTexts[rowIndex].text  = count.ToString();
            _inventoryCountTexts[rowIndex].color = unavailable ? _unavailableColor : _normalColor;
        }

        /// <summary>
        /// 指定スキルの説明文を表示します。無効なSkillIDの場合は非表示にします。
        /// </summary>
        public void SetExplanation( ActiveSkillID skillID )
        {
            if ( !ActiveSkillsData.IsValidSkill( skillID ) ) { HideExplanation(); return; }

            ShowExplanation( ActiveSkillsData.data[( int ) skillID].ExplainTextKey );
        }

        /// <summary>
        /// 指定パッシブスキルの説明文を表示します。無効なPassiveSkillIDの場合は非表示にします。
        /// </summary>
        public void SetExplanation( PassiveSkillID skillID )
        {
            if ( !PassiveSkillsData.IsValidSkill( skillID ) ) { HideExplanation(); return; }

            ShowExplanation( PassiveSkillsData.data[( int ) skillID].ExplainTextKey );
        }

        private void ShowExplanation( LocKey textKey )
        {
            _explanationText.text = textKey == LocKey.None ? "" : _localization.Get( textKey );
            _explanationPanel.SetActive( true );
        }

        public void HideExplanation() => _explanationPanel.SetActive( false );
    }
}
