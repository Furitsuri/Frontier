using TMPro;
using UnityEngine;

namespace Frontier.UI
{
    public class TooltipUI : MonoBehaviour, IUiMonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _explanation;

        public void Setup()
        {

        }

        void Awake()
        {
            gameObject.SetActive( false );
        }

        public void SetText( string text )
        {
            _explanation.text = text;
        }

        public void SetPosirion( Vector2 pos )
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            rectTransform.anchoredPosition = pos;
        }
    }
}
