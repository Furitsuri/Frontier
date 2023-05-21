using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

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
    public GameObject m_BattleManager;
    GameObject m_StageImage;
    public float stageStartDelay = 2f;				// ステージ開始時に表示する時間(秒)

    private GamePhase phase;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if( instance != this )
        {
            Destroy( gameObject );
        }

        DontDestroyOnLoad( gameObject );

        if (BattleManager.instance == null)
        {
            Instantiate(m_BattleManager);
        }

        InitGame();
    }

    // Start is called before the first frame update
    void Start()
    {   
        StartCoroutine(GameFlow());
    }

    void InitGame()
    {
        m_StageImage = GameObject.Find("StageImage");

        Invoke("StageLevelImage", stageStartDelay);

        phase = GamePhase.GAME_START;
    }

    // ステージレベル表示終了
    void StageLevelImage()
    {
        m_StageImage.SetActive(false);
    }

    IEnumerator GameFlow()
    {
        while (phase != GamePhase.GAME_END)
        {
            Debug.Log(phase);
            yield return null;

            switch (phase)
            {
                case GamePhase.GAME_START:
                    phase = GamePhase.GAME_TITLE_MENU;
                    break;
                case GamePhase.GAME_TITLE_MENU:

                    phase = GamePhase.GAME_BATTLE;
                    break;
                case GamePhase.GAME_BATTLE:
                    // StartCoroutine(m_BattleManager.Battle());
                    // Battleの終了を待つ
                    // yield return new WaitUntil(() => m_BattleManager.isEnd());

                    phase = GamePhase.GAME_END_SCENE;
                    break;
                case GamePhase.GAME_END_SCENE:
                    phase = GamePhase.GAME_END;
                    break;
                case GamePhase.GAME_END:
                    break;
            }
        }
    }
}
