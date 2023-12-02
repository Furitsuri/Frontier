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
                // UIï\é¶Çñ≥å¯Ç…
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="showTime"></param>
        public void Init( float showTime )
        {
            _isUpdate = false;
            _showTime = showTime;
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
        /// ï\é¶ÇèIóπÇµÇƒÇ¢ÇÈÇ©Çï‘ÇµÇ‹Ç∑
        /// </summary>
        public bool IsShowEnd()
        {
            return _isUpdate && !gameObject.activeSelf;
        }
    }
}