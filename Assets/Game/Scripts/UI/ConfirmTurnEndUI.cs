using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Frontier
{
    public class ConfirmTurnEndUI : MonoBehaviour
    {
        public TextMeshProUGUI[] _confirmTMPTexts;

        /// <summary>
        /// �I�����Ă���C���f�b�N�X�ɊY�����镶���F��ύX���܂�
        /// </summary>
        /// <param name="selectIndex">�I�𒆂̃C���f�b�N�X�l</param>
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
