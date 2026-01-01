using TMPro;

namespace Frontier.UI
{
    public class ParameterAttackDirectionUI : UiMonoBehaviour
    {
        public TextMeshProUGUI attackCursorP2E;
        public TextMeshProUGUI attackCursorE2P;

        override public void Setup()
        {
            // base.Setup(); // ※ 親クラスのSetupは呼ばない

            gameObject.SetActive( true );
        }
    }
}