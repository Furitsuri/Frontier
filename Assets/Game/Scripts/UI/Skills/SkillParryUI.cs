using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Frontier
{
    public class SkillParryUI : MonoBehaviour
    {
        

        [SerializeField]
        private TextMeshProUGUI TMPJudge;
        
        private bool _isUpdate = false;
        private float _showTime;
        private string[] _judgeTexts = new string[] { $"SUCCESS!", $"FAILED...", $"JUST SUCCESS!!" };
        private Color[] _judgeColors = new Color[] { Color.white, Color.red, Color.yellow };

        void Awake()
        {
            Debug.Assert(_judgeTexts.Length == (int)SkillParryController.JudgeResult.MAX, "The number of elements in the enums does not match the number of elements in the strings.");
            Debug.Assert(_judgeColors.Length == (int)SkillParryController.JudgeResult.MAX, "The number of elements in the enums does not match the number of elements in the colors.");
        }

        void Update()
        {
            if (!_isUpdate) return;

            _showTime -= Time.deltaTime;

            if (_showTime < 0f)
            {
                // UI表示を無効に
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        /// <param name="showTime">表示時間</param>
        public void Init( float showTime )
        {
            _isUpdate = false;
            _showTime = showTime;
        }

        /// <summary>
        /// 終了させます
        /// </summary>
        public void terminate()
        {
            gameObject.SetActive(false);
            _isUpdate = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="result"></param>
        public void ShowResult(SkillParryController.JudgeResult result)
        {
            TMPJudge.text = _judgeTexts[(int)result];
            TMPJudge.color = _judgeColors[(int)result];
            _isUpdate = true;
        }

        /// <summary>
        /// 表示を終了しているかを返します
        /// </summary>
        public bool IsShowEnd()
        {
            return _isUpdate && !gameObject.activeSelf;
        }
    }
}