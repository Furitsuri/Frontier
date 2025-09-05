using Frontier.Battle;
using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Zenject;
using static Constants;

namespace Frontier
{
    public class GameMain : FocusRoutineController
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
        [Header("UI")]
        private IUiSystem _UISystem;

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
        private DiContainer _diContainer;
        private HierarchyBuilderBase _hierarchyBld;
        private InputFacade _inputFcd;
        private TutorialFacade _tutorialFcd;
        private GamePhase _Phase;
#if UNITY_EDITOR
        private DebugMenuFacade _debugMenuFcd;
        private DebugEditorMonoDriver _debugEditorMonoDrv;
#endif // UNITY_EDITOR

        public static GameMain instance = null;

        [Inject]
        public void Construct( DiContainer diContainer,  InputFacade inputFcd, HierarchyBuilderBase hierarchyBld, TutorialFacade tutorialFcd )
        {
            _diContainer    = diContainer;
            _inputFcd       = inputFcd;
            _hierarchyBld   = hierarchyBld;
            _tutorialFcd    = tutorialFcd;
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

            Debug.Assert(_hierarchyBld != null, "Error : インスタンスの生成管理を行うオブジェクトが設定されていません。");
            Debug.Assert(_inputFcd != null, "Error : 入力窓口のオブジェクトが設定されていません。");

            if (ManagerProvider.Instance == null)
            {
                _hierarchyBld.CreateComponentAndOrganize<ManagerProvider>(_managerProvider, true);
            }

#if UNITY_EDITOR
            if (_debugMenuFcd == null)
            {
                _debugMenuFcd = _hierarchyBld.InstantiateWithDiContainer<DebugMenuFacade>(false);
                NullCheck.AssertNotNull(_debugMenuFcd);
            }
            if( _debugEditorMonoDrv == null )
            {
                _debugEditorMonoDrv = _diContainer.Resolve<DebugEditorMonoDriver>();
                NullCheck.AssertNotNull(_debugEditorMonoDrv);
            }
#endif // UNITY_EDITOR
        }

        // Start is called before the first frame update
        void Start()
        {
            // 入力関連の初期化
            _inputFcd.Init();
            // チュートリアル関連の初期化
            _tutorialFcd.Init();

            base.Init();
            InitGame();

            StartCoroutine(GameFlow());
        }

        void Update()
        {
            base.UpdateRoutine();
        }

        void LateUpdate()
        {
            base.LateUpdateRoutine();
        }

        void FixedUpdate()
        {
            base.FixedUpdateRoutine();
        }

        /// <summary>
        /// ゲームを初期化します
        /// </summary>
        private void InitGame()
        {
            // アニメーションデータの初期化
            AnimDatas.Init();

#if UNITY_EDITOR
            // デバッグモードの初期化
            _debugMenuFcd.Init( CanAcceptDebugTransition, AcceptDebugTransition );
            _debugEditorMonoDrv.Init();
            // デバッグモードへ移行するための入力コードを登録
            ResgiterInputCodes();
#endif // UNITY_EDITOR

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

        /*
        /// <summary>
        /// フォーカスルーチンを初期化します
        /// </summary>
        private void InitFocusRoutine()
        {
            _focusRtnCtrl.Init();
#if UNITY_EDITOR
            _focusRtnCtrl.Register(_debugEditorMonoDrv, (int)FocusRoutinePriority.DEBUG_EDITOR);
            _focusRtnCtrl.Register(_debugMenuFcd.GetFocusRoutine(), _debugMenuFcd.GetFocusRoutine().GetPriority());
#endif // UNITY_EDITOR
            _focusRtnCtrl.Register(_tutorialFcd.GetFocusRoutine(), _tutorialFcd.GetFocusRoutine().GetPriority());
            _focusRtnCtrl.Register(_btlRtnCtrl, _btlRtnCtrl.GetPriority());
            _focusRtnCtrl.RunRoutineAndPauseOthers(FocusRoutinePriority.BATTLE);
        }
        */

#if UNITY_EDITOR
        /// <summary>
        /// デバッグメニューを開くための入力コードを登録します。
        /// ※この入力コードはUnity Editor上ではデバッグ状態以外の全ての状態で有効です。
        /// </summary>
        private void ResgiterInputCodes()
        {
            int hashCode = Hash.GetStableHash(Constants.DEBUG_TRANSION_INPUT_HASH_STRING);

            _inputFcd.RegisterInputCodes((GuideIcon.DEBUG_MENU, "DEBUG", CanAcceptDebugTransition, new AcceptBooleanInput(AcceptDebugTransition), 0.0f, hashCode));
        }

        /// <summary>
        /// デバッグメニューを開くための入力を受け付けるかどうかを判定します。
        /// </summary>
        /// <returns>デバッグメニューへの遷移の可否</returns>
        private bool CanAcceptDebugTransition()
        {
            return true;
        }

        /// <summary>
        /// デバッグメニューへの遷移入力を受け付けた際の処理を行います。
        /// </summary>
        /// <param name="isDebugTranstion">デバッグメニューへの遷移入力</param>
        /// <returns>入力実行の有無</returns>
        private bool AcceptDebugTransition(bool isDebugTranstion)
        {
            if( !isDebugTranstion ) return false;

            _debugMenuFcd.OpenDebugMenu();

            return true;
        }
#endif // UNITY_EDITOR
    }
}