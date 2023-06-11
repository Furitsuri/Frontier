using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.Text;

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
    private Character m_PrevCharacter = null;
    // ���ݑI�𒆂̃L�����N�^�[�C���f�b�N�X
    public int SelectCharacterIndex { get; private set; } = -1;
    // �U���t�F�[�Y���ɂ����āA�U�����J�n����L�����N�^�[
    public Character AttackerCharacter { get; private set; } = null;

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

    void Start()
    {
        m_PhaseManager.Init();

        // �����̒l��ݒ�
        Quaternion[] rot = new Quaternion[(int)Constants.Direction.NUM_MAX];
        for (int i = 0; i < (int)Constants.Direction.NUM_MAX; ++i)
        {
            rot[i] = Quaternion.AngleAxis(90 * i, Vector3.up);
        }


        // �e�v���C���[�L�����N�^�[�̈ʒu��ݒ�
        for (int i = 0; i < m_Players.Count; ++i)
        {
            Player player = m_Players[i];
            // �X�e�[�W�J�n���̃v���C���[�����ʒu(�C���f�b�N�X)���L���b�V��
            int gridIndex                               = player.param.initGridIndex;
            // �v���C���[�̉�ʏ�̈ʒu��ݒ�
            player.transform.position                   = m_StageGrid.getGridCharaStandPos(gridIndex);
            // ������ݒ�
            player.transform.rotation = rot[(int)player.param.initDir];
            // �Ή�����O���b�h�ɗ����Ă���v���C���[�̃C���f�b�N�X��ݒ�
            m_StageGrid.getGridInfo(gridIndex).charaIndex = player.param.characterIndex;
        }

        // �e�G�l�~�[�L�����N�^�[�̈ʒu��ݒ�
        for (int i = 0; i < m_Enemies.Count; ++i)
        {
            Enemy enemy = m_Enemies[i];
            // �X�e�[�W�J�n���̃v���C���[�����ʒu(�C���f�b�N�X)���L���b�V��
            int gridIndex = enemy.param.initGridIndex;
            // �G�l�~�[�̉�ʏ�̈ʒu��ݒ�
            enemy.transform.position = m_StageGrid.getGridCharaStandPos(gridIndex);
            // ������ݒ�
            enemy.transform.rotation = rot[(int)enemy.param.initDir];
            // �Ή�����O���b�h�ɗ����Ă���v���C���[�̃C���f�b�N�X��ݒ�
            m_StageGrid.getGridInfo(gridIndex).charaIndex = enemy.param.characterIndex;
        }
    }

    void Update()
    {
        // ���݂̃O���b�h��ɑ��݂���L�����N�^�[�����X�V
        SelectCharacterIndex = StageGrid.instance.getCurrentGridInfo().charaIndex;

        // �L�����N�^�[�̃p�����[�^�\���̍X�V
        UpdateCharacterParameter();

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
        foreach (Enemy enemy in m_Enemies)
        {
            m_StageGrid.getGridInfo(enemy.tmpParam.gridIndex).charaIndex = enemy.param.characterIndex;
        }
    }

    // �v���C���[�s���t�F�[�Y
    void PlayerPhase()
    {
        m_Phase = BattlePhase.BATTLE_PLAYER_COMMAND;
    }

    /// <summary>
    /// �I���O���b�h��ɂ�����L�����N�^�[�̃p�����[�^�����X�V���܂�
    /// </summary>
    void UpdateCharacterParameter()
    {
        var BattleUI = BattleUISystem.Instance;

        // �U���L�����N�^�[�����݂���ꍇ�͍X�V���Ȃ�
        if (AttackerCharacter != null)
        {
            // �p�����[�^�\�����X�V
            var character = SearchCharacterFromCharaIndex(SelectCharacterIndex);
            BattleUI.ToggleEnemyParameter(true);

            // �I�����Ă���L�����N�^�[�̃��C���[���p�����[�^UI�\���̂��߂Ɉꎞ�I�ɕύX
            if (character != null && m_PrevCharacter != character)
            {
                character.gameObject.SetLayerRecursively(LayerMask.NameToLayer("ParamRender"));
            }

            m_PrevCharacter = character;
        }
        else
        {
            // �p�����[�^�\�����X�V
            var character = SearchCharacterFromCharaIndex(SelectCharacterIndex);
            BattleUI.TogglePlayerParameter(character != null && character.param.charaTag == Character.CHARACTER_TAG.CHARACTER_PLAYER);
            BattleUI.ToggleEnemyParameter(character != null && character.param.charaTag == Character.CHARACTER_TAG.CHARACTER_ENEMY);

            // �O�t���[���őI�������L�����ƈقȂ�ꍇ�̓��C���[�����ɖ߂�
            if (m_PrevCharacter != null && m_PrevCharacter != character)
            {
                m_PrevCharacter.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Character"));
            }
            // �I�����Ă���L�����N�^�[�̃��C���[���p�����[�^UI�\���̂��߂Ɉꎞ�I�ɕύX
            if (character != null && m_PrevCharacter != character)
            {
                character.gameObject.SetLayerRecursively(LayerMask.NameToLayer("ParamRender"));
            }

            m_PrevCharacter = character;
        }
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

    // �v���C���[�����X�g�ɓo�^
    public void AddPlayerToList( Player player )
    {
        m_Players.Add( player );
    }

    // �G�l�~�[�����X�g�ɓo�^
    public void AddEnemyToList( Enemy enemy )
    {
        m_Enemies.Add( enemy );
    }

    public void registStageGrid( StageGrid script )
    {
        m_StageGrid = script;
    }

    /// <summary>
    /// �U���L�����N�^�[��ݒ肵�܂�
    /// �p�����[�^UI�\���J�����O�̂��߂Ƀ��C���[��ύX���܂�
    /// </summary>
    /// <param name="character">�A�^�b�J�[�ݒ肷��L�����N�^�[</param>
    public void SetAttackerCharacter(Character character)
    {
        AttackerCharacter = character;
        AttackerCharacter.gameObject.SetLayerRecursively(LayerMask.NameToLayer("ParamRenderAttacker"));
    }

    /// <summary>
    /// �U���L�����N�^�[�̐ݒ���������܂�
    /// �p�����[�^UI�\���J�����O�̂��߂ɕύX���Ă������C���[�����ɖ߂��܂�
    /// </summary>
    public void ResetAttackerCharacter()
    {
        AttackerCharacter = null;
        AttackerCharacter.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Character"));
    }


    /// <summary>
    /// �U���t�F�[�Y��Ԃ��ǂ������m�F���܂�
    /// </summary>
    /// <returns>true ; �U���t�F�[�Y��Ԃł��� false : �U���t�F�[�Y��Ԃł͂Ȃ� </returns>
    public bool IsAttackPhaseState()
    {
        // �U���t�F�[�Y��Ԃ̍ۂɂ�AttackerCharacter���K��null�ł͂Ȃ�
        return AttackerCharacter != null;
    }

    public Player SearchPlayerFromCharaIndex( int characterIndex)
    {
        foreach (Player player in m_Players)
        {
            if (player.param.characterIndex == characterIndex)
            {
                return player;
            }
        }

        return null;
    }

    public Character SearchCharacterFromCharaIndex(int characterIndex)
    {
        foreach (Player player in m_Players)
        {
            if (player.param.characterIndex == characterIndex)
            {
                return player;
            }
        }
        foreach (Enemy enemy in m_Enemies)
        {
            if (enemy.param.characterIndex == characterIndex)
            {
                return enemy;
            }
        }

        return null;
    }

    public Character GetSelectCharacter()
    {
        return SearchCharacterFromCharaIndex(SelectCharacterIndex);
    }

    public bool isEnd()
    {
        return m_Phase == BattlePhase.BATTLE_END;
    }
}