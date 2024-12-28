using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Frontier
{
    public class PlayerCommandUI : MonoBehaviour
    {
        [SerializeField]
        private GameObject _TMPCommandStringSample;

        [Inject]
        private HierarchyBuilder _hierarchyBld = null;

        private List<TextMeshProUGUI> _commandTexts = new List<TextMeshProUGUI>();
        private RectTransform _commandUIBaseRectTransform;
        private VerticalLayoutGroup _cmdTextVerticalLayout;
        private PLSelectCommandState _PLSelectScript;
        private string[] _commandStrings;

        void Awake()
        {
            Debug.Assert(_hierarchyBld != null, "HierarchyBuilder�̃C���X�^���X����������Ă��܂���BInject�̐ݒ���m�F���Ă��������B");

            _commandUIBaseRectTransform     = gameObject.GetComponent<RectTransform>();
            _cmdTextVerticalLayout          = gameObject.GetComponent<VerticalLayoutGroup>();
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
            float marginSize      = _cmdTextVerticalLayout.padding.top * 2f;  // �㉺�̃}�[�W���T�C�Y�����݂��邽��2�{�̒l
            float intervalSize    = _cmdTextVerticalLayout.spacing;

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
        /// ���s�\�ȃR�}���h���R�}���h���X�gUI�ɐݒ肵�܂�
        /// </summary>
        /// <param name="executableCommands">���s�\�ȃR�}���h���X�g</param>
        public void SetExecutableCommandList( in List<Character.Command.COMMAND_TAG> executableCommands )
        {
            float fontSize = 0;

            foreach ( var cmdText in _commandTexts )
            {
                Destroy(cmdText.gameObject);
            }
            _commandTexts.Clear();

            // ���s�\�ȃR�}���h�̕���������X�g�ɒǉ����A���̃Q�[���I�u�W�F�N�g���q�Ƃ��ēo�^
            for (int i = 0; i < executableCommands.Count; ++i)
            {
                TextMeshProUGUI commandString = _hierarchyBld.CreateComponentAndOrganize<TextMeshProUGUI>(true);
                commandString.transform.SetParent(this.gameObject.transform, false);
                commandString.SetText(_commandStrings[(int)executableCommands[i]]);
                _commandTexts.Add(commandString);
                fontSize = commandString.fontSize;
            }

            // �I���\�ȃR�}���h����p����UI�̉��n�̑傫����ύX
            ResizeUIBaseRectTransform(fontSize, executableCommands.Count);
        }
    }
}