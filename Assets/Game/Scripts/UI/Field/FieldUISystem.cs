using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// フィールド画面専用のUIシステムです。BattleUi/ShopUiと同格の、IUiSystem直下のUIシステムとして扱います。
    /// フィールドメニューからしか開かれないUI(キャラクター編集等)をGeneralUIから分離して保持します。
    /// </summary>
    public class FieldUISystem : MonoBehaviour
    {
        [Header( "CharacterEdit" )]
        [SerializeField] private CharacterEditUI _characterEditView;    // キャラクター編集UI

        public CharacterEditUI CharacterEditView => _characterEditView;

        public void Setup()
        {
            _characterEditView?.Setup();
        }
    }
}
