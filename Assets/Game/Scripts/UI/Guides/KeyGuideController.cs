using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    /// <summary>
    /// �L�[�K�C�h�֘A�̐�����s���܂�
    /// </summary>
    public class KeyGuideController : MonoBehaviour
    {
        /// <summary>
        /// �e�L�[�̃A�C�R��
        /// </summary>
        public enum KeyIcon : int
        {
            UP = 0,     // ��
            DOWN,       // ��
            LEFT,       // ��
            RIGHT,      // �E
            DECISION,   // ����
            CANCEL,     // �߂�
        }

        /// <summary>
        /// �L�[�̃A�C�R���Ƃ��̐������̍\����
        /// </summary>
        public struct KeyGuide
        {
            // �L�[�A�C�R��
            public KeyIcon type;
            // �A�C�R���ɑ΂��������
            public string explanation;
        }

        // ���݂̏󋵂ɂ����āA�L���ƂȂ�L�[�Ƃ�������������ۂ̐���
        List<KeyGuide> _keyGuides;



        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        /// <summary>
        /// �J�ڐ�̃L�[�K�C�h��ݒ肵�܂�
        /// </summary>
        /// <param name="guides"></param>
        public void Transit(List<KeyGuide> guides )
        {
            _keyGuides = guides;
        }
    }
}