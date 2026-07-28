using TMPro;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// セーブ/ロード画面の最小限のView。
    /// オートセーブ用のスロット1つと、ユーザーが任意にセーブ出来るスロット3つを固定で保持します。
    /// セーブ画面/ロード画面の差分は画面上部のタイトル(SetTitle)のみで、他の構成は完全に共有します。
    /// スロットへの表示内容の反映やカーソル操作等の実際の制御はPresenter側が担います。
    /// </summary>
    public class SaveLoadUI : UiMonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI  _titleText;
        [SerializeField] private SaveSlotItemUI   _autoSaveSlot;
        [SerializeField] private SaveSlotItemUI[] _manualSaveSlots;

        public SaveSlotItemUI   AutoSaveSlot    => _autoSaveSlot;
        public SaveSlotItemUI[] ManualSaveSlots => _manualSaveSlots;

        public override void Setup()
        {
            base.Setup(); // gameObject.SetActive(false)

            _autoSaveSlot.Setup();
            // 個々のスロットは画面自体(親)の表示/非表示に追従させるため、常時アクティブにする
            _autoSaveSlot.gameObject.SetActive( true );
            _autoSaveSlot.SetAutoLabelVisible( true );

            foreach ( var slot in _manualSaveSlots )
            {
                slot.Setup();
                slot.gameObject.SetActive( true );
                slot.SetAutoLabelVisible( false );
            }
        }

        /// <summary>
        /// 画面上部のタイトルを設定します(例: "SAVE" / "LOAD")。
        /// セーブ画面とロード画面の差分はこれのみです。
        /// </summary>
        public void SetTitle( string text )
        {
            _titleText.text = text;
        }
    }
}
