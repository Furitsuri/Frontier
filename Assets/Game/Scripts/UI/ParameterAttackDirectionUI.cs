using TMPro;

namespace Frontier.UI
{
    public class ParameterAttackDirectionUI : UiMonoBehaviour
    {
        public TextMeshProUGUI attackCursorP2E;
        public TextMeshProUGUI attackCursorE2P;

        public override void Setup()
        {
            // base.Setup(); // ※ 親クラスのSetupは呼ばない

            gameObject.SetActive( true );
        }
    }
}