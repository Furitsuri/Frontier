using System;
using System.Collections;
using TMPro;
using UnityEngine;
using Frontier.Combat;

namespace Frontier.UI
{
    public class CommandNameUI : UiMonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _commandNameText;

        private RectTransform _rectTransform;
        private Coroutine _autoHideCoroutine;

        private const float CHAR_WIDTH = 32f;

        public override void Setup()
        {
            base.Setup();
            LazyInject.GetOrCreate( ref _rectTransform, () => GetComponent<RectTransform>() );
        }

        /// <summary>
        /// スキル名を表示します。
        /// duration が 0 以上の場合、指定秒数後に自動で非表示にします。
        /// duration が負の値の場合は自動非表示を行わず、Hide() による明示的な非表示が必要です。
        /// </summary>
        /// <param name="skillId">表示するスキルのID</param>
        /// <param name="duration">自動非表示までの秒数。負の値で無効。</param>
        /// <param name="onComplete">自動非表示後に呼ばれるコールバック。duration が負の値の場合は呼ばれません。</param>
        public void Show( SkillID skillId, float duration = -1f, Action onComplete = null )
        {
            string skillName = SkillsData.data[( int ) skillId].Name;
            _commandNameText.text = skillName;

            var sizeDelta = _rectTransform.sizeDelta;
            sizeDelta.x = skillName.Length * CHAR_WIDTH;
            _rectTransform.sizeDelta = sizeDelta;

            if( _autoHideCoroutine != null )
            {
                StopCoroutine( _autoHideCoroutine );
                _autoHideCoroutine = null;
            }

            gameObject.SetActive( true );

            if( duration >= 0f )
            {
                _autoHideCoroutine = StartCoroutine( AutoHideCoroutine( duration, onComplete ) );
            }
        }

        /// <summary>
        /// スキル名の表示を非表示にします。自動非表示タイマーも停止します。
        /// </summary>
        public void Hide()
        {
            if( _autoHideCoroutine != null )
            {
                StopCoroutine( _autoHideCoroutine );
                _autoHideCoroutine = null;
            }

            gameObject.SetActive( false );
        }

        private IEnumerator AutoHideCoroutine( float duration, Action onComplete = null )
        {
            yield return new WaitForSeconds( duration );
            Hide();
            onComplete?.Invoke();
        }
    }
}