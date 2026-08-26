using TMPro;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// 戦闘中に撃破した敵から獲得したアニマを常時表示するView。
    /// 戦闘開始時に0リセットされる戦闘専用の累計値であり、戦闘開始前から所持していたアニマ
    /// (UserDomain.Anima)は含まない。入力ガイドバーの左端付近に配置する想定。
    /// </summary>
    public class BattleAnimaUI : UiMonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _animaText;

        /// <summary>
        /// UiMonoBehaviour.Setup()の既定実装はgameObjectを非表示にするため、
        /// 常時表示としたいこのUIではSetup完了時点で明示的に表示状態へ戻す。
        /// </summary>
        public override void Setup()
        {
            base.Setup();

            gameObject.SetActive( true );
        }

        public void SetAnima( int anima )
        {
            _animaText.text = anima.ToString();
        }

        /// <summary>
        /// ステージクリア時など、この表示を明示的に隠したい場面で呼び出します。
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive( false );
        }
    }
}
