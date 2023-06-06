using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public enum TurnType
    {
        PLAYER_TURN = 0,
        ENEMY_TURN,
    }

    public static BattleManager instance = null;
    public GameObject m_StageGridObject;
    private PhaseManagerBase m_PhaseManager;
    private StageGrid m_StageGrid;
    private BattlePhase m_Phase;
    private List<Player> m_Players = new List<Player>(Constants.CHARACTER_MAX_NUM);
    private List<Enemy> m_Enemies = new List<Enemy>(Constants.CHARACTER_MAX_NUM);
    // ���ݑI�𒆂̃L�����N�^�[�C���f�b�N�X
    public int SelectCharacterIndex { get; private set; } = -1;

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

        if (StageGrid.instance == null)
        {
            Instantiate(m_StageGridObject);
        }

        m_PhaseManager = new PlayerPhaseManager();
    }

    // Start is called before the first frame update
    void Start()
    {
        m_PhaseManager.Init();

        // �e�v���C���[�L�����N�^�[�̈ʒu��ݒ�
        for (int i = 0; i < m_Players.Count; ++i)
        {
            Player player = m_Players[i];
            // �X�e�[�W�J�n���̃v���C���[�����ʒu(�C���f�b�N�X)���L���b�V��
            int gridIndex                               = player.param.initGridIndex;
            // �v���C���[�̉�ʏ�̈ʒu��ݒ�
            player.transform.position                   = m_StageGrid.getGridCharaStandPos(gridIndex);
            // �Ή�����O���b�h�ɗ����Ă���v���C���[�̃C���f�b�N�X��ݒ�
            m_StageGrid.getGridInfo(gridIndex).charaIndex = player.param.characterIndex;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // ���݂̃O���b�h��ɑ��݂���L�����N�^�[�����X�V
        SelectCharacterIndex = StageGrid.instance.getCurrentGridInfo().charaIndex;

        m_PhaseManager.Update();
    }

    void LateUpdate()
    {
        m_PhaseManager.LateUpdate();

        // �X�e�[�W�O���b�h��̃L���������X�V
        StageGrid.instance.ClearGridsCharaIndex();
        foreach( Player player in m_Players )
        {
            m_StageGrid.getGridInfo(player.tmpParam.gridIndex).charaIndex = player.param.characterIndex;
        }
    }

    // �v���C���[�s���t�F�[�Y
    void PlayerPhase()
    {
        m_Phase = BattlePhase.BATTLE_PLAYER_COMMAND;
    }

    public IEnumerator Battle()
    {
        while (m_Phase != BattlePhase.BATTLE_END)
        {
            yield return null;
            Debug.Log(m_Phase);

            switch (m_Phase)
            {
                case BattlePhase.BATTLE_START:
                    m_Phase = BattlePhase.BATTLE_PLAYER_COMMAND;
                    break;
                case BattlePhase.BATTLE_PLAYER_COMMAND:
                    PlayerPhase();
                    break;
                case BattlePhase.BATTLE_PLAYER_EXECUTE:
                    m_Phase = BattlePhase.BATTLE_ENEMY_COMMAND;
                    break;
                case BattlePhase.BATTLE_ENEMY_COMMAND:
                    m_Phase = BattlePhase.BATTLE_ENEMY_EXECUTE;
                    break;
                case BattlePhase.BATTLE_ENEMY_EXECUTE:
                    m_Phase = BattlePhase.BATTLE_RESULT;
                    break;
                case BattlePhase.BATTLE_RESULT:
                    m_Phase = BattlePhase.BATTLE_END;
                    break;
                case BattlePhase.BATTLE_END:
                    break;
            }
        }
    }

    // �v���C���[�����X�g�ɓo�^.
    public void AddPlayerToList( Player player )
    {
        m_Players.Add( player );
    }

    public void registStageGrid( StageGrid script )
    {
        m_StageGrid = script;
    }

    public Player GetPlayerFromIndex( int index)
    {
        foreach (Player player in m_Players)
        {
            if (player.param.characterIndex == index)
            {
                return player;
            }
        }

        return null;
    }

    public bool isEnd()
    {
        return m_Phase == BattlePhase.BATTLE_END;
    }
}