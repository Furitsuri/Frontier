using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Frontier
{
    public class PlayerCommandUI : MonoBehaviour
    {
        [SerializeField]
        private GameObject _TMPCommandStringSample;

        private List<TextMeshProUGUI> _commandTexts = new List<TextMeshProUGUI>();
        private RectTransform _commandUIBaseRectTransform;
        private PLSelectCommandState _PLSelectScript;
        private string[] _commandStrings;

        void Awake()
        {
            _commandUIBaseRectTransform     = gameObject.GetComponent<RectTransform>();
            TextMeshProUGUI[] commandNames  = gameObject.GetComponentsInChildren<TextMeshProUGUI>();

            // �R�}���h�������������
            InitCommandStrings();

            // �N�������Active��Off��
            this.gameObject.SetActive(false);
        }

        /// <summary>
        /// �R�}���h�̕���������������܂�
        /// MEMO : �R�}���h���V����������x�ɕ������ǉ����Ă�������
        /// </summary>
        void InitCommandStrings()
        {
            _commandStrings = new string[(int)Character.Command.COMMAND_TAG.NUM]
            {
                "MOVE",
                "ATTACK",
                "WAIT"
            };

            Debug.Assert( _commandStrings.Length == (int)Character.Command.COMMAND_TAG.NUM );
        }

        // Update is called once per frame
        void Update()
        {
            UpdateSelectCommand();
        }

        /// <summary>
        /// �v���C���[�R�}���h�̍X�V�������s���܂�
        /// </summary>
        void UpdateSelectCommand()
        {
            // ��x�S�Ă𔒐F�ɐݒ�
            foreach (var text in _commandTexts)
            {
                text.color = Color.white;
            }

            // �I�����ڂ�ԐF�ɐݒ�
            _commandTexts[_PLSelectScript.SelectCommandIndex].color = Color.red;
        }

        /// <summary>
        /// �v���C���[�R�}���h�̑I��UI�̉��n�ƂȂ�RectTransform�̑傫�����X�V���܂�
        /// </summary>
        /// <param name="PLCommands">�v���C���[�̃R�}���h�\���̔z��</param>
        void ResizeUIBaseRectTransform( float fontSize, int executableCmdNum )
        {
            const float marginSize      = 20f;  // �㉺�̃}�[�W���T�C�Y�����ꂼ��10f�ł��邽��2�{�̒l
            const float intervalSize    = 10f;

            _commandUIBaseRectTransform.sizeDelta = new Vector2(_commandUIBaseRectTransform.sizeDelta.x, marginSize + (fontSize + intervalSize ) * executableCmdNum - intervalSize);
        }

        /// <summary>
        /// �v���C���[�R�}���h�̃X�N���v�g��o�^���܂�
        /// </summary>
        /// <param name="script">�v���C���[�R�}���h�̃X�N���v�g</param>
        public void RegistPLCommandScript(Frontier.PLSelectCommandState script)
        {
            _PLSelectScript = script;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="executables"></param>
        public void SetExecutableCommandList( in List<Character.Command.COMMAND_TAG> executableCommands )
        {
            float fontSize = 0;
            const float marjin = 10f;

            foreach ( var cmdText in _commandTexts )
            {
                Destroy(cmdText.gameObject);
            }
            _commandTexts.Clear();

            // ���s�\�ȃR�}���h�̕���������X�g�ɒǉ����A���̃Q�[���I�u�W�F�N�g���q�Ƃ��ēo�^
            for (int i = 0; i < executableCommands.Count; ++i)
            {
                GameObject stringObject = Instantiate(_TMPCommandStringSample);
                if (stringObject == null) continue;
                TextMeshProUGUI commandString = stringObject.GetComponent<TextMeshProUGUI>();
                commandString.transform.SetParent(this.gameObject.transform);
                commandString.SetText(_commandStrings[(int)executableCommands[i]]);
                commandString.rectTransform.anchoredPosition = new Vector2(0f, -marjin - 30f * i);
                commandString.gameObject.SetActive( true );
                _commandTexts.Add(commandString);
                fontSize = commandString.fontSize;
            }

            // �I���\�ȃR�}���h����p����UI�̉��n�̑傫����ύX
            ResizeUIBaseRectTransform(fontSize, executableCommands.Count);
        }
    }
}