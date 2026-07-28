using TMPro;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// セーブ/ロード画面の1スロット分の見た目を担当するView。
    /// 「どのステージか」(_stageText)と「いつセーブされたか」(_dateText)を表示し、
    /// オートセーブスロットの場合のみ_autoLabelを表示します。
    /// </summary>
    public class SaveSlotItemUI : UiMonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _stageText;
        [SerializeField] private TextMeshProUGUI _dateText;
        [SerializeField] private GameObject      _autoLabel;

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
        /// カーソルによる選択状態を表示に反映します
        /// </summary>
        public void SetSelected( bool isSelected )
        {
            var color = isSelected ? Color.red : Color.white;
            _stageText.color = color;
            _dateText.color  = color;
        }
    }
}
