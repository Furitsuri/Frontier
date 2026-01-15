using TMPro;
using UnityEngine;

namespace Frontier.UI
{
    public class TooltipUI : UiMonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _explanation;

        RectTransform _rectTransform = null;

        public void SetText( string text )
        {
            _explanation.text = text;
        }

        public void SetPosition( Vector2 pos )
        {
            _rectTransform.anchoredPosition = pos;
        }

        public Vector2 GetSize()
        {
            return _rectTransform.sizeDelta;
        }

        public Vector2 GetPivot()
        {
            return _rectTransform.pivot;
        }

        public override void Setup()
        {
            _rectTransform = GetComponent<RectTransform>();

            gameObject.SetActive( false );
        }
    }
}
