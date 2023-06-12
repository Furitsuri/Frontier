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
    public GameObject stageGridObject;
    private BattlePhase _phase;
    private PhaseManagerBase _phaseManager;
    private StageGrid _stageGrid;
    private List<Player> _players = new List<Player>(Constants.CHARACTER_MAX_NUM);
    private List<Enemy> _enemies = new List<Enemy>(Constants.CHARACTER_MAX_NUM);
    private Character _prevCharacter = null;
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
            Instantiate(stageGridObject);
        }

        _phaseManager = new PlayerPhaseManager();
    }

    void Start()
    {
        _phaseManager.Init();

        // �����̒l��ݒ�
        Quaternion[] rot = new Quaternion[(int)Constants.Direction.NUM_MAX];
        for (int i = 0; i < (int)Constants.Direction.NUM_MAX; ++i)
        {
            rot[i] = Quaternion.AngleAxis(90 * i, Vector3.up);
        }


        // �e�v���C���[�L�����N�^�[�̈ʒu��ݒ�
        for (int i = 0; i < _players.Count; ++i)
        {
            Player player = _players[i];
            // �X�e�[�W�J�n���̃v���C���[�����ʒu(�C���f�b�N�X)���L���b�V��
            int gridIndex                               = player.param.initGridIndex;
            // �v���C���[�̉�ʏ�̈ʒu��ݒ�
            player.transform.position                   = _stageGrid.getGridCharaStandPos(gridIndex);
            // ������ݒ�
            player.transform.rotation = rot[(int)player.param.initDir];
            // �Ή�����O���b�h�ɗ����Ă���v���C���[�̃C���f�b�N�X��ݒ�
            _stageGrid.getGridInfo(gridIndex).charaIndex = player.param.characterIndex;
        }

        // �e�G�l�~�[�L�����N�^�[�̈ʒu��ݒ�
        for (int i = 0; i < _enemies.Count; ++i)
        {
            Enemy enemy = _enemies[i];
            // �X�e�[�W�J�n���̃v���C���[�����ʒu(�C���f�b�N�X)���L���b�V��
            int gridIndex = enemy.param.initGridIndex;
            // �G�l�~�[�̉�ʏ�̈ʒu��ݒ�
            enemy.transform.position = _stageGrid.getGridCharaStandPos(gridIndex);
            // ������ݒ�
            enemy.transform.rotation = rot[(int)enemy.param.initDir];
            // �Ή�����O���b�h�ɗ����Ă���v���C���[�̃C���f�b�N�X��ݒ�
            _stageGrid.getGridInfo(gridIndex).charaIndex = enemy.param.characterIndex;
        }
    }

    void Update()
    {
        // ���݂̃O���b�h��ɑ��݂���L�����N�^�[�����X�V
        SelectCharacterIndex = StageGrid.instance.getCurrentGridInfo().charaIndex;

        // �L�����N�^�[�̃p�����[�^�\���̍X�V
        UpdateCharacterParameter();

        _phaseManager.Update();
    }

    void LateUpdate()
    {
        _phaseManager.LateUpdate();

        // �X�e�[�W�O���b�h��̃L���������X�V
        StageGrid.instance.ClearGridsCharaIndex();
        foreach( Player player in _players )
        {
            _stageGrid.getGridInfo(player.tmpParam.gridIndex).charaIndex = player.param.characterIndex;
        }
        foreach (Enemy enemy in _enemies)
        {
            _stageGrid.getGridInfo(enemy.tmpParam.gridIndex).charaIndex = enemy.param.characterIndex;
        }
    }

    // �v���C���[�s���t�F�[�Y
    void PlayerPhase()
    {
        _phase = BattlePhase.BATTLE_PLAYER_COMMAND;
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
            if (character != null && _prevCharacter != character)
            {
                character.gameObject.SetLayerRecursively(LayerMask.NameToLayer("ParamRender"));
            }

            _prevCharacter = character;
        }
        else
        {
            // �p�����[�^�\�����X�V
            var character = SearchCharacterFromCharaIndex(SelectCharacterIndex);
            BattleUI.TogglePlayerParameter(character != null && character.param.charaTag == Character.CHARACTER_TAG.CHARACTER_PLAYER);
            BattleUI.ToggleEnemyParameter(character != null && character.param.charaTag == Character.CHARACTER_TAG.CHARACTER_ENEMY);

            // �O�t���[���őI�������L�����ƈقȂ�ꍇ�̓��C���[�����ɖ߂�
            if (_prevCharacter != null && _prevCharacter != character)
            {
                _prevCharacter.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Character"));
            }
            // �I�����Ă���L�����N�^�[�̃��C���[���p�����[�^UI�\���̂��߂Ɉꎞ�I�ɕύX
            if (character != null && _prevCharacter != character)
            {
                character.gameObject.SetLayerRecursively(LayerMask.NameToLayer("ParamRender"));
            }

            _prevCharacter = character;
        }
    }

    public IEnumerator Battle()
    {
        while (_phase != BattlePhase.BATTLE_END)
        {
            yield return null;
            Debug.Log(_phase);

            switch (_phase)
            {
                case BattlePhase.BATTLE_START:
                    _phase = BattlePhase.BATTLE_PLAYER_COMMAND;
                    break;
                case BattlePhase.BATTLE_PLAYER_COMMAND:
                    PlayerPhase();
                    break;
                case BattlePhase.BATTLE_PLAYER_EXECUTE:
                    _phase = BattlePhase.BATTLE_ENEMY_COMMAND;
                    break;
                case BattlePhase.BATTLE_ENEMY_COMMAND:
                    _phase = BattlePhase.BATTLE_ENEMY_EXECUTE;
                    break;
                case BattlePhase.BATTLE_ENEMY_EXECUTE:
                    _phase = BattlePhase.BATTLE_RESULT;
                    break;
                case BattlePhase.BATTLE_RESULT:
                    _phase = BattlePhase.BATTLE_END;
                    break;
                case BattlePhase.BATTLE_END:
                    break;
            }
        }
    }

    // �v���C���[�����X�g�ɓo�^
    public void AddPlayerToList( Player player )
    {
        _players.Add( player );
    }

    // �G�l�~�[�����X�g�ɓo�^
    public void AddEnemyToList( Enemy enemy )
    {
        _enemies.Add( enemy );
    }

    public void registStageGrid( StageGrid script )
    {
        _stageGrid = script;
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

    /// <summary>
    /// �L�����N�^�[�C���f�b�N�X��f�ɊY������v���C���[��T���܂�
    /// </summary>
    /// <param name="characterIndex">��������v���C���[�L�����N�^�[�̃C���f�b�N�X</param>
    /// <returns>�Y�������v���C���[�L�����N�^�[</returns>
    public Player SearchPlayerFromCharaIndex( int characterIndex)
    {
        foreach (Player player in _players)
        {
            if (player.param.characterIndex == characterIndex)
            {
                return player;
            }
        }

        return null;
    }

    /// <summary>
    /// �L�����N�^�[�C���f�b�N�X��f�ɊY������L�����N�^�[��T���܂�
    /// </summary>
    /// <param name="characterIndex">��������L�����N�^�[�̃C���f�b�N�X</param>
    /// <returns>�Y�������L�����N�^�[</returns>
    public Character SearchCharacterFromCharaIndex(int characterIndex)
    {
        foreach (Player player in _players)
        {
            if (player.param.characterIndex == characterIndex)
            {
                return player;
            }
        }
        foreach (Enemy enemy in _enemies)
        {
            if (enemy.param.characterIndex == characterIndex)
            {
                return enemy;
            }
        }

        return null;
    }

    /// <summary>
    /// ���ݑI�����Ă���O���b�h��̃L�����N�^�[���擾���܂�
    /// </summary>
    /// <returns>�I�����Ă���O���b�h��̃L�����N�^�[</returns>
    public Character GetSelectCharacter()
    {
        return SearchCharacterFromCharaIndex(SelectCharacterIndex);
    }

    /// <summary>
    /// �I����Ԃ��ǂ����𔻒肵�܂�
    /// </summary>
    /// <returns>true : �I��</returns>
    public bool isEnd()
    {
        return _phase == BattlePhase.BATTLE_END;
    }
}