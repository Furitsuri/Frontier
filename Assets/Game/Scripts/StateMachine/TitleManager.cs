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
                // �O������Ăяo����҂�
                case TitlePhase.TITLE_START:
                    break;
                case TitlePhase.TITLE_GAME_START:
                    _phase = TitlePhase.TITLE_END;
                    break;
                case TitlePhase.TITLE_EXIT:
                    // �I��
                    break;
                case TitlePhase.TITLE_END:
                    break;
            }
        }
    }

    /// <summary>
    /// �^�C�g������Q�[���ɑJ�ڂ��܂�
    /// �^�C�g����ʂ�Button����Ăяo����܂�
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
