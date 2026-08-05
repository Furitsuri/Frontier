using TMPro;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// フィールド画面右上に常時表示する所持金・SP(部隊共有ポイント、仮称)・部隊人数(現在数/上限数)のHUD。
    /// TroopEditUI/CharacterEditUIのヘッダー表示と同じ位置・書式に揃えている。
    /// </summary>
    public class FieldHeaderUI : UiMonoBehaviour
    {
        [Header( "所持金テキスト(TroopEdit/CharacterEdit画面と同じ位置に表示する)" )]
        [SerializeField] private TextMeshProUGUI _moneyText;

        [Header( "SP(部隊共有ポイント、仮称)テキスト" )]
        [SerializeField] private TextMeshProUGUI _spText;

        [Header( "部隊人数(現在数/上限数)テキスト" )]
        [SerializeField] private TextMeshProUGUI _memberCountText;

        public void Show() => gameObject.SetActive( true );

        public void Hide() => gameObject.SetActive( false );

        public void SetHeaderInfo( int money, int sp, int currentMemberNum, int maxMemberNum )
        {
            _moneyText.text = money.ToString();
            _spText.text = sp.ToString();
            _memberCountText.text = $"{currentMemberNum}/{maxMemberNum}";
        }
    }
}
