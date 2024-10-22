using Frontier;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class GeneralUISystem : MonoBehaviour
    {
        public static GeneralUISystem Instance { get; private set; }

        [Header("KeyGuidePresenter")]
        public KeyGuidePresenter KeyGuideView;  // �L�[�K�C�h�\��

        void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// �L�[�K�C�h�\���̑J�ڏ������s���܂�
        /// </summary>
        /// <param name="keyGuideList">�\������L�[�K�C�h���X�g</param>
        public void TransitKeyGuide(List<KeyGuideUI.KeyGuide> keyGuideList)
        {
            KeyGuideView.Transit(keyGuideList);
        }
    }
}