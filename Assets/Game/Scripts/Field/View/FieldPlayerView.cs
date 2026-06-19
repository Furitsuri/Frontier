using System;
using System.Collections;
using UnityEngine;

namespace Frontier.Field
{
    /// <summary>
    /// フィールド上のプレイヤーアイコン。
    /// 現在ノードに配置され、ノード選択時に目標ノードへ移動する。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class FieldPlayerView : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private Color _iconColor = new Color(0.2f, 0.6f, 1f, 1f);

        private SpriteRenderer _spriteRenderer;
        private Coroutine      _moveCoroutine;

        private void Awake()
        {
            _spriteRenderer       = GetComponent<SpriteRenderer>();
            _spriteRenderer.color = _iconColor;
            _spriteRenderer.sortingOrder = 10;
        }

        public void Setup(Vector3 startPos)
        {
            transform.position = startPos;
        }

        /// <summary>ノード間を移動する。完了後 onComplete を呼ぶ。</summary>
        public void MoveTo(Vector3 target, Action onComplete = null)
        {
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            _moveCoroutine = StartCoroutine(MoveRoutine(target, onComplete));
        }

        private IEnumerator MoveRoutine(Vector3 target, Action onComplete)
        {
            while (Vector3.Distance(transform.position, target) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, target, _moveSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = target;
            _moveCoroutine = null;
            onComplete?.Invoke();
        }
    }
}
