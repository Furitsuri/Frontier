using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Frontier
{
    public class ConfirmTurnEndUI : MonoBehaviour
    {
        public TextMeshProUGUI[] _confirmTMPTexts;

        public void Init()
        {

        }

        /// <summary>
        /// 選択しているインデックスに該当する文字色を変更します
        /// </summary>
        /// <param name="selectIndex">選択中のインデックス値</param>
        public void ApplyTextColor(int selectIndex)
        {
            for (int i = 0; i < _confirmTMPTexts.Length; ++i)
            {
                if (i == selectIndex)
                {
                    _confirmTMPTexts[i].color = Color.yellow;
                }
                else
                {
                    _confirmTMPTexts[i].color = Color.gray;
                }
            }
        }
    }
}
