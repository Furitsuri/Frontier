using TMPro;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// フィールド画面右上に常時表示する所持アニマ・部隊人数(現在数/上限数)のHUD。
    /// TroopEditUI/CharacterEditUIのヘッダー表示と同じ位置・書式に揃えている。
    /// MEMO : 元は所持金・SP(部隊共有ポイント、仮称)の2欄に分かれていたが、両者を区別する必要が
    /// 無くなったため単一のアニマとして統合した。テキスト欄自体は暫定的に2つとも残し、同じ値を表示する。
    /// </summary>
    public class FieldHeaderUI : UiMonoBehaviour
    {
        [Header( "所持アニマテキスト(TroopEdit/CharacterEdit画面と同じ位置に表示する)" )]
        [SerializeField] private TextMeshProUGUI _moneyText;

        [Header( "所持アニマテキスト(暫定的にもう1箇所へも表示する)" )]
        [SerializeField] private TextMeshProUGUI _spText;

        [Header( "部隊人数(現在数/上限数)テキスト" )]
        [SerializeField] private TextMeshProUGUI _memberCountText;

        public void Show() => gameObject.SetActive( true );

        public void Hide() => gameObject.SetActive( false );

        public void SetHeaderInfo( int anima, int currentMemberNum, int maxMemberNum )
        {
            _moneyText.text = anima.ToString();
            _spText.text = anima.ToString();
            _memberCountText.text = $"{currentMemberNum}/{maxMemberNum}";
        }
    }
}
