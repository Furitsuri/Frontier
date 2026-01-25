using TMPro;
using Unity.VisualScripting;
using UnityEngine;

namespace Frontier.UI
{
    public class BounceAnimator : MonoBehaviour
    {
        [SerializeField] private Vector2 _start;
        [SerializeField] private Vector2 _end;
        [SerializeField] private float _speed = 1.0f;

        private float _time;
        private RectTransform _rect;

        void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        void Update()
        {
            if( _rect == null ) return;

            // 時間経過に応じて0〜1を往復させる
            _time += Time.deltaTime * _speed;
            float t = Mathf.PingPong( _time, 1f );

            // start → end を補間して往復
            Vector2 pos = Vector2.Lerp( _start, _end, t );
            _rect.anchoredPosition = pos;
        }
    }

}