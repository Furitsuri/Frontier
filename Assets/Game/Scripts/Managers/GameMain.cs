using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Zenject;

namespace Frontier
{
    public class GameMain : MonoBehaviour
    {
        enum GamePhase
        {
            GAME_START = 0,
            GAME_TITLE_MENU,
            GAME_BATTLE,
            GAME_END_SCENE,
            GAME_END,
        }

        [SerializeField]
        [Header("�K�w�Ǘ��E�I�u�W�F�N�g�����N���X")]
        private HierarchyBuilder _hierarchyBld;

        [SerializeField]
        [Header("UI")]
        private UISystem _UISystem;

        [SerializeField]
        [Header("UI�J�����̃I�u�W�F�N�g")]
        private GameObject _UICameraObject;

        [SerializeField]
        [Header("�e��}�l�[�W���̃v���o�C�_�I�u�W�F�N�g")]
        private GameObject _managerProvider;

        [SerializeField]
        [Header("�X�e�[�W�J�n���ɕ\�����鎞��(�b)")]
        private float stageStartDelay = 2f;

        private GameObject _stageImage;
        private InputFacade _inputFcd;
        private BattleManager _btlMgr;
        private GamePhase _Phase;

        public static GameMain instance = null;

        [Inject]
        public void Construct( InputFacade inputFcd, BattleManager btlMgr )
        {
            _inputFcd   = inputFcd;
            _btlMgr     = btlMgr;
        }

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }

            Debug.Assert(_hierarchyBld != null, "Error : �C���X�^���X�̐����Ǘ����s���I�u�W�F�N�g���ݒ肳��Ă��܂���B");
            Debug.Assert(_inputFcd != null, "Error : ���͑����̃I�u�W�F�N�g���ݒ肳��Ă��܂���B");

            DontDestroyOnLoad(gameObject);

            if (ManagerProvider.Instance == null)
            {
                _hierarchyBld.CreateComponentAndOrganize<ManagerProvider>(_managerProvider, true);
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            InitGame();

            StartCoroutine(GameFlow());
        }

        /// <summary>
        /// �Q�[�������������܂�
        /// </summary>
        private void InitGame()
        {
            // �A�j���[�V�����f�[�^�̏�����
            AnimDatas.Init();
            // ���͊֘A�̏�����
            _inputFcd.Init();
            // �퓬�}�l�[�W���̏�����
            // _btlMgr.Init();

            _stageImage = GameObject.Find("StageLevelImage");
            if (_stageImage != null)
            {
                Invoke("StageLevelImage", stageStartDelay);
            }

            _Phase = GamePhase.GAME_START;
        }

        /// <summary>
        /// �X�e�[�W���x���̉摜�\��������߂܂�
        /// Invoke�֐��ŎQ�Ƃ���܂�
        /// </summary>
        private void StageLevelImage()
        {
            _stageImage.SetActive(false);
        }

        private IEnumerator GameFlow()
        {
            while (_Phase != GamePhase.GAME_END)
            {
                // Debug.Log(_Phase);
                yield return null;

                switch (_Phase)
                {
                    case GamePhase.GAME_START:
                        _Phase = GamePhase.GAME_TITLE_MENU;
                        break;
                    case GamePhase.GAME_TITLE_MENU:
                        _Phase = GamePhase.GAME_BATTLE;
                        break;
                    case GamePhase.GAME_BATTLE:
                        // StartCoroutine(_btlMgr.Battle());
                        // Battle�̏I����҂�
                        // yield return new WaitUntil(() => _btlMgr.isEnd());

                        _Phase = GamePhase.GAME_END_SCENE;
                        break;
                    case GamePhase.GAME_END_SCENE:
                        _Phase = GamePhase.GAME_END;
                        break;
                    case GamePhase.GAME_END:
                        break;
                }
            }
        }
    }
}