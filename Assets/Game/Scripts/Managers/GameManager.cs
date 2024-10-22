using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Frontier
{
    public class GameManager : MonoBehaviour
    {
        enum GamePhase
        {
            GAME_START = 0,
            GAME_TITLE_MENU,
            GAME_BATTLE,
            GAME_END_SCENE,
            GAME_END,
        }

        public static GameManager instance = null;
        private GameObject _stageImage;
        private GamePhase _Phase;
        public GameObject _managerProvider;
        public float stageStartDelay = 2f;              // ステージ開始時に表示する時間(秒)

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

            DontDestroyOnLoad(gameObject);

            if (ManagerProvider.Instance == null)
            {
                Instantiate(_managerProvider);
            }

            InitGame();
        }

        // Start is called before the first frame update
        void Start()
        {
            StartCoroutine(GameFlow());
        }

        /// <summary>
        /// ゲームを初期化します
        /// </summary>
        void InitGame()
        {
            // アニメーションデータの初期化
            AnimDatas.Init();

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
        void StageLevelImage()
        {
            _stageImage.SetActive(false);
        }

        IEnumerator GameFlow()
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