using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Frontier.Field
{
    /// <summary>
    /// フィールド上のノード1個を表すワールド空間コンポーネント。
    /// SpriteRenderer で表示し、Physics2DRaycaster + IPointerClickHandler でクリックを検出する。
    /// </summary>
    [RequireComponent( typeof( SpriteRenderer ) )]
    [RequireComponent( typeof( BoxCollider2D ) )]
    public class FieldNodeView : MonoBehaviour, IPointerClickHandler
    {
        private static readonly Color ColorReachable    = Color.white;
        private static readonly Color ColorNotReachable = new Color( 0.4f, 0.4f, 0.4f, 1f );

        [SerializeField] private TextMeshPro _label = null;

        private SpriteRenderer _spriteRenderer = null;
        private int            _nodeId;
        private bool           _isReachable;
        private Action<int>    _onSelected;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Setup( FieldNodeData data, bool isReachable, Action<int> onSelected )
        {
            _nodeId     = data.Id;
            _onSelected = onSelected;

            if ( _label != null ) _label.text = ( ( FieldNodeType ) data.Type ).ToString();

            SetReachable( isReachable );
        }

        public void SetReachable( bool isReachable )
        {
            _isReachable = isReachable;
            if ( _spriteRenderer != null )
                _spriteRenderer.color = isReachable ? ColorReachable : ColorNotReachable;
        }

        public void OnPointerClick( PointerEventData eventData )
        {
            if ( !_isReachable ) return;
            _onSelected?.Invoke( _nodeId );
        }
    }
}
