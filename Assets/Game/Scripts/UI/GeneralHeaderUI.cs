using TMPro;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// 画面上部の全幅に常時表示する所持アニマ・部隊人数(現在数/上限数)のHUD。
    /// GeneralUI.prefab側で管理される共通UIであり、FieldScene/RecruitScene等、
    /// GeneralUIを利用する全シーンで共通して表示される。
    /// </summary>
    public class GeneralHeaderUI : UiMonoBehaviour
    {
        [Header( "所持アニマのアイコン右側に表示する数値テキスト" )]
        [SerializeField] private TextMeshProUGUI _animaValueText;

        [Header( "部隊人数(現在数/上限数)テキスト" )]
        [SerializeField] private TextMeshProUGUI _memberCountText;

        public void Show() => gameObject.SetActive( true );

        public void Hide() => gameObject.SetActive( false );

        public void SetHeaderInfo( int anima, int currentMemberNum, int maxMemberNum )
        {
            _animaValueText.text = anima.ToString();
            _memberCountText.text = $"{currentMemberNum}/{maxMemberNum}";
        }
    }
}
