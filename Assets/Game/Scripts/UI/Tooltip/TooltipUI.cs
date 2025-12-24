using TMPro;
using UnityEngine;

namespace Frontier.UI
{
    public class TooltipUI : UiMonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _explanation;

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

        override public void Setup()
        {

        }
    }
}
