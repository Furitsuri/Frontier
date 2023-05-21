using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    enum BattlePhase
    {
        BATTLE_START = 0,
        BATTLE_PLAYER_COMMAND,
        BATTLE_PLAYER_EXECUTE,
        BATTLE_ENEMY_COMMAND,
        BATTLE_ENEMY_EXECUTE,
        BATTLE_RESULT,
        BATTLE_END,
    }

    public static BattleManager instance = null;
    StageGrid grid;
    BattlePhase phase;

    private void Awake()
    {
        grid = GetComponent<StageGrid>();
    }

    // Start is called before the first frame update
    void Start()
    {
        phase = BattlePhase.BATTLE_START;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator Battle()
    {
        while (phase != BattlePhase.BATTLE_END)
        {
            yield return null;
            Debug.Log(phase);

            switch (phase)
            {
                case BattlePhase.BATTLE_START:
                    phase = BattlePhase.BATTLE_PLAYER_COMMAND;
                    break;
                case BattlePhase.BATTLE_PLAYER_COMMAND:
                    // INPUT‚ÌI—¹‚ð‘Ò‚Â
                    yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

                    phase = BattlePhase.BATTLE_PLAYER_EXECUTE;
                    break;
                case BattlePhase.BATTLE_PLAYER_EXECUTE:
                    phase = BattlePhase.BATTLE_ENEMY_COMMAND;
                    break;
                case BattlePhase.BATTLE_ENEMY_COMMAND:
                    phase = BattlePhase.BATTLE_ENEMY_EXECUTE;
                    break;
                case BattlePhase.BATTLE_ENEMY_EXECUTE:
                    phase = BattlePhase.BATTLE_RESULT;
                    break;
                case BattlePhase.BATTLE_RESULT:
                    phase = BattlePhase.BATTLE_END;
                    break;
                case BattlePhase.BATTLE_END:
                    break;
            }
        }
    }

    public bool isEnd()
    {
        return phase == BattlePhase.BATTLE_END;
    }
}
