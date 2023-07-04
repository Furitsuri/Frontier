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

    TitlePhase _phase;

    // Start is called before the first frame update
    void Start()
    {
        _phase = TitlePhase.TITLE_START;
    }

    public IEnumerator Title()
    {
        while (_phase != TitlePhase.TITLE_END)
        {
            yield return null;
            Debug.Log(_phase);

            switch (_phase)
            {
                // 外部から呼び出しを待つ
                case TitlePhase.TITLE_START:
                    break;
                case TitlePhase.TITLE_GAME_START:
                    _phase = TitlePhase.TITLE_END;
                    break;
                case TitlePhase.TITLE_EXIT:
                    // 終了
                    break;
                case TitlePhase.TITLE_END:
                    break;
            }
        }
    }

    /// <summary>
    /// タイトルからゲームに遷移します
    /// タイトル画面のButtonから呼び出されます
    /// </summary>
    public void PressStart()
    {
        Debug.Log("Press Start");
        _phase = TitlePhase.TITLE_GAME_START;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bool isGameStart()
    {
        return _phase == TitlePhase.TITLE_GAME_START;
    }
}
