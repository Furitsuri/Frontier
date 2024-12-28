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
        [Header("入力関連窓口")]
        private InputFacade _inputFacade;

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
        [Header("バトルマネージャオブジェクト")]
        private GameObject _btlMgrObj;

        [SerializeField]
        [Header("ステージ開始時に表示する時間(秒)")]
        private float stageStartDelay = 2f;

        private BattleManager _btlMgr;
        private GameObject _stageImage;
        private GamePhase _Phase;

        public static GameMain instance = null;

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

            Debug.Assert(_hierarchyBld != null, "Error : インスタンスの生成管理を行うオブジェクトが設定されていません。");
            Debug.Assert(_inputFacade != null, "Error : 入力窓口のオブジェクトが設定されていません。");

            DontDestroyOnLoad(gameObject);

            if (ManagerProvider.Instance == null)
            {
                _hierarchyBld.CreateComponentAndOrganize<ManagerProvider>(_managerProvider, true);
            }
            if( _btlMgr == null )
            {
                _btlMgr = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<BattleManager>(_btlMgrObj, true, true);
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            Inject();

            InitGame();

            StartCoroutine(GameFlow());
        }

        /// <summary>
        /// 各インスタンスへのインジェクションを行います
        /// </summary>
        private void Inject()
        {
            _btlMgr.Inject(_hierarchyBld, _inputFacade);
        }

        /// <summary>
        /// ゲームを初期化します
        /// </summary>
        private void InitGame()
        {
            // アニメーションデータの初期化
            AnimDatas.Init();
            // 入力関連の初期化
            _inputFacade.Init();
            // 戦闘マネージャの初期化
            // _btlMgr.Init();

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
                        // StartCoroutine(_btlMgr.Battle());
                        // Battleの終了を待つ
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