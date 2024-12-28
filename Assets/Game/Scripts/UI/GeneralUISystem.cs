using Frontier;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class GeneralUISystem : MonoBehaviour
    {
        public static GeneralUISystem Instance { get; private set; }

        [Header("InputGuidePresenter")]
        public InputGuidePresenter InputGuideView;  // �L�[�K�C�h�\��

        void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// �L�[�K�C�h�\���p�̃��X�g��ݒ肵�܂�
        /// </summary>
        /// <param name="keyGuideList">�\������L�[�K�C�h���X�g</param>
        public void SetInputGuideList(List<InputGuideUI.InputGuide> keyGuideList)
        {
            InputGuideView.SetGuides(keyGuideList);
        }
    }
}