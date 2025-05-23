﻿using Frontier.Battle;
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
        [Header("階層管理・オブジェクト生成クラス")]
        private HierarchyBuilder _hierarchyBld;

        [SerializeField]
        [Header("UI")]
        private UISystem _UISystem;

        [SerializeField]
        [Header("UIカメラのオブジェクト")]
        private GameObject _UICameraObject;

        [SerializeField]
        [Header("各種マネージャのプロバイダオブジェクト")]
        private GameObject _managerProvider;

        [SerializeField]
        [Header("ステージ開始時に表示する時間(秒)")]
        private float stageStartDelay = 2f;

        private GameObject _stageImage;
        private FocusRoutineController _focusRtnCtrl;
        private InputFacade _inputFcd;
        private TutorialFacade _tutorialFcd;
        private BattleRoutineController _btlRtnCtrl;
        private GamePhase _Phase;

        public static GameMain instance = null;

        [Inject]
        public void Construct( InputFacade inputFcd, TutorialFacade tutorialFcd, BattleRoutineController btlRtnCtrl )
        {
            _inputFcd       = inputFcd;
            _tutorialFcd    = tutorialFcd;
            _btlRtnCtrl     = btlRtnCtrl;
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

            if (transform.parent != null)
            {
                transform.SetParent(null); // ルートに移動
            }

            DontDestroyOnLoad(gameObject);

            if ( _focusRtnCtrl == null )
            {
                _focusRtnCtrl = _hierarchyBld.InstantiateWithDiContainer<FocusRoutineController>();
                NullCheck.AssertNotNull(_focusRtnCtrl);
            }

            Debug.Assert(_hierarchyBld != null, "Error : インスタンスの生成管理を行うオブジェクトが設定されていません。");
            Debug.Assert(_inputFcd != null, "Error : 入力窓口のオブジェクトが設定されていません。");

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

        private void Update()
        {
            _focusRtnCtrl.Update();
        }

        /// <summary>
        /// ゲームを初期化します
        /// </summary>
        private void InitGame()
        {
            // アニメーションデータの初期化
            AnimDatas.Init();
            // 入力関連の初期化
            _inputFcd.Init();
            // チュートリアル関連の初期化
            _tutorialFcd.Init();

            // 戦闘マネージャの初期化
            // _btlRtnCtrl.Init();

            InitFocusRoutine();

            _stageImage = GameObject.Find("StageLevelImage");
            if (_stageImage != null)
            {
                Invoke("StageLevelImage", stageStartDelay);
            }

            _Phase = GamePhase.GAME_START;
        }

        /// <summary>
        /// ステージレベルの画像表示を取りやめます
        /// Invoke関数で参照されます
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
                        // StartCoroutine(_btlRtnCtrl.Battle());
                        // Battleの終了を待つ
                        // yield return new WaitUntil(() => _btlRtnCtrl.isEnd());

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

        /// <summary>
        /// フォーカスルーチンを初期化します
        /// </summary>
        private void InitFocusRoutine()
        {
            _focusRtnCtrl.Init();
            _focusRtnCtrl.Register(_btlRtnCtrl, _btlRtnCtrl.GetPriority());
            _focusRtnCtrl.Register(_tutorialFcd.GetFocusRoutine(), _tutorialFcd.GetFocusRoutine().GetPriority());
            _focusRtnCtrl.RunRoutineAndPauseOthers(FocusRoutinePriority.BATTLE);
        }
    }
}