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
    public BattleManager m_BattleManager;
    GameObject m_StageImage;
    public float stageStartDelay = 2f;				// ステージ開始時に表示する時間(秒)

    private GamePhase m_Phase;

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

        m_Phase = GamePhase.GAME_START;
    }

    // ステージレベル表示終了
    void StageLevelImage()
    {
        m_StageImage.SetActive(false);
    }

    IEnumerator GameFlow()
    {
        while (m_Phase != GamePhase.GAME_END)
        {
            Debug.Log(m_Phase);
            yield return null;

            switch (m_Phase)
            {
                case GamePhase.GAME_START:
                    m_Phase = GamePhase.GAME_TITLE_MENU;
                    break;
                case GamePhase.GAME_TITLE_MENU:

                    m_Phase = GamePhase.GAME_BATTLE;
                    break;
                case GamePhase.GAME_BATTLE:
                    StartCoroutine(m_BattleManager.Battle());
                    // Battleの終了を待つ
                    yield return new WaitUntil(() => m_BattleManager.isEnd());

                    m_Phase = GamePhase.GAME_END_SCENE;
                    break;
                case GamePhase.GAME_END_SCENE:
                    m_Phase = GamePhase.GAME_END;
                    break;
                case GamePhase.GAME_END:
                    break;
            }
        }
    }
}
