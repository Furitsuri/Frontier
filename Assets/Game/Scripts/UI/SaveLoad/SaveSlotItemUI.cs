using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Frontier.UI
{
    /// <summary>
    /// セーブ/ロード画面の1スロット分の見た目を担当するView。
    /// 「どのステージか」(_stageText)と「いつセーブされたか」(_dateText)を表示し、
    /// オートセーブスロットの場合のみ_autoLabelを表示します。
    /// カーソルによる選択状態は、SkillBoxUIのフリック処理を参考に背景の点滅で表現します。
    /// </summary>
    public class SaveSlotItemUI : UiMonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _stageText;
        [SerializeField] private TextMeshProUGUI _dateText;
        [SerializeField] private GameObject      _autoLabel;

        private Image _backgroundImage;
        private Color _normalBackgroundColor;
        private ColorFlicker<ImageColorAdapter> _backgroundFlicker;

        public override void Setup()
        {
            base.Setup();

            _backgroundImage      = GetComponent<Image>();
            _normalBackgroundColor = _backgroundImage.color;
            _backgroundFlicker    = new ColorFlicker<ImageColorAdapter>( new ImageColorAdapter( _backgroundImage ) );
        }

        private void Update()
        {
            _backgroundFlicker?.UpdateFlick();
        }

        public void SetStageText( string text )
        {
            _stageText.text = text;
        }

        public void SetDateText( string text )
        {
            _dateText.text = text;
        }

        /// <summary>オートセーブ専用の「AUTO SAVE」表示の可否を設定します。</summary>
        public void SetAutoLabelVisible( bool visible )
        {
            _autoLabel.SetActive( visible );
        }

        /// <summary>
        /// カーソルによる選択状態を、背景の点滅(フリック)で表現します。
        /// </summary>
        public void SetSelected( bool isSelected )
        {
            _backgroundFlicker.setEnabled( isSelected );

            // 選択から外れた場合は元の背景色に戻す(SkillBoxUI.SetFlickEnabledと同様)
            if ( !isSelected )
            {
                _backgroundImage.color = _normalBackgroundColor;
            }
        }
    }
}
