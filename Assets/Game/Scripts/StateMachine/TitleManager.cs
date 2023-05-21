using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleManager : MonoBehaviour
{
    enum TitlePhase
    {
        TITLE_START = 0,
        TITLE_GAME_START,
        TITLE_EXIT,
        TITLE_END,
    }

    TitlePhase phase;

    // Start is called before the first frame update
    void Start()
    {
        phase = TitlePhase.TITLE_START;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator Title()
    {
        while (phase != TitlePhase.TITLE_END)
        {
            yield return null;
            Debug.Log(phase);

            switch (phase)
            {
                // äOïîÇ©ÇÁåƒÇ—èoÇµÇë“Ç¬
                case TitlePhase.TITLE_START:
                    break;
                case TitlePhase.TITLE_GAME_START:
                    phase = TitlePhase.TITLE_END;
                    break;
                case TitlePhase.TITLE_EXIT:
                    // èIóπ
                    break;
                case TitlePhase.TITLE_END:
                    break;
            }
        }
    }

    public void PressStart()
    {
        Debug.Log("Press Start");
        phase = TitlePhase.TITLE_GAME_START;
    }

    public bool isGameStart()
    {
        return phase == TitlePhase.TITLE_GAME_START;
    }
}
