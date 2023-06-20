using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using static Character;

public class BattleManager : Singleton<BattleManager>
{
    enum BattlePhase
    {
        BATTLE_START = 0,
        BATTLE_PLAYER_COMMAND,
        BATTLE_ENEMY_COMMAND,
        BATTLE_ENEMY_EXECUTE,
        BATTLE_RESULT,
        BATTLE_END,
    }

    public enum TurnType
    {
        PLAYER_TURN = 0,
        ENEMY_TURN,

        NUM
    }

    public GameObject stageGridObject;
    private BattlePhase _phase;
    private PhaseManagerBase _currentPhaseManager;
    private StageGrid _stageGrid;
    private PhaseManagerBase[] _phaseManagers = new PhaseManagerBase[((int)TurnType.NUM)];
    private List<Player> _players = new List<Player>(Constants.CHARACTER_MAX_NUM);
    private List<Enemy> _enemies = new List<Enemy>(Constants.CHARACTER_MAX_NUM);
    private Character _prevCharacter = null;
    private bool _transitNextPhase = false;
    private int _phaseManagerIndex = 0;
    // ���ݑI�𒆂̃L�����N�^�[�C���f�b�N�X
    public int SelectCharacterIndex { get; private set; } = -1;
    // �U���t�F�[�Y���ɂ����āA�U�����J�n����L�����N�^�[
    public Character AttackerCharacter { get; private set; } = null;

    override protected void Init()
    {
        if (StageGrid.Instance == null)
        {
            Instantiate(stageGridObject);
        }

        _phaseManagers[(int)TurnType.PLAYER_TURN]   = new PlayerPhaseManager();
        _phaseManagers[(int)TurnType.ENEMY_TURN]    = new EnemyPhaseManager();
        _currentPhaseManager                        = _phaseManagers[(int)TurnType.PLAYER_TURN];
    }

    override protected void OnStart()
    {
        _currentPhaseManager.Init();

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
            _stageGrid.GetGridInfo(gridIndex).charaIndex = player.param.characterIndex;
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
            _stageGrid.GetGridInfo(gridIndex).charaIndex = enemy.param.characterIndex;
        }
    }

    override protected void OnUpdate()
    {
        // TODO : ���B���ƂŃ��t�@�N�^
        if( GameManager.instance.IsInvoking() )
        {
            return;
        }

        // ���݂̃O���b�h��ɑ��݂���L�����N�^�[�����X�V
        SelectCharacterIndex = StageGrid.Instance.GetCurrentGridInfo().charaIndex;
        // �L�����N�^�[�̃p�����[�^�\���̍X�V
        UpdateCharacterParameter();
        // �t�F�[�Y�}�l�[�W�����X�V
        _transitNextPhase = _currentPhaseManager.Update();
    }

    override protected void OnLateUpdate()
    {
        // �X�e�[�W�O���b�h��̃L���������X�V
        StageGrid.Instance.ClearGridsCharaIndex();
        foreach( Player player in _players )
        {
            _stageGrid.GetGridInfo(player.tmpParam.gridIndex).charaIndex = player.param.characterIndex;
        }
        foreach (Enemy enemy in _enemies)
        {
            _stageGrid.GetGridInfo(enemy.tmpParam.gridIndex).charaIndex = enemy.param.characterIndex;
        }

        if (!_transitNextPhase)
        {
            _currentPhaseManager.LateUpdate();
        }
        else
        {
            // ���̃}�l�[�W���ɐ؂�ւ���
            _phaseManagerIndex = (_phaseManagerIndex + 1) % (int)TurnType.NUM;
            _currentPhaseManager = _phaseManagers[_phaseManagerIndex];
            _currentPhaseManager.Init();
            // �ꎞ�p�����[�^�����Z�b�g
            ResetTmpParamAllCharacter();
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
        if( AttackerCharacter == null )
        {
            return;
        }

        // �I�����Ă���L�����C���f�b�N�X���U���ΏۃL�������猳�ɖ߂�
        SelectCharacterIndex = AttackerCharacter.param.characterIndex;

        AttackerCharacter.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Character"));
        AttackerCharacter = null;
    }


    /// <summary>
    /// �U���t�F�[�Y��Ԃ��ǂ������m�F���܂�
    /// </summary>
    /// <returns>true : �U���t�F�[�Y��Ԃł��� false : �U���t�F�[�Y��Ԃł͂Ȃ� </returns>
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
    public Player SearchPlayerFromCharaIndex( int characterIndex )
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
    /// �S�Ă̍s���\�L�����N�^�[�̍s�����I���������𔻒肵�܂�
    /// </summary>
    /// <returns>�S�Ă̍s���\�L�����N�^�[�̍s�����I��������</returns>
    public bool IsEndAllCharacterWaitCommand()
    {
        foreach (Player player in _players)
        {
            if ( !player.tmpParam.isEndCommand[(int)Character.BaseCommand.COMMAND_WAIT] )
            {
                return false;
            }
        }

        return true;
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
        return SearchCharacterFromCharaIndex(StageGrid.Instance.GetCurrentGridInfo().charaIndex);
    }

    /// <summary>
    /// �G�����X�g���珇�ԂɎ擾���܂�
    /// </summary>
    /// <returns>�G�L�����N�^�[</returns>
    public IEnumerable<Enemy> GetEnemies()
    {
        foreach( Enemy enemy in _enemies)
        {
            yield return enemy;
        }

        yield break;
    }

    /// <summary>
    /// �S�Ẵv���C���[�L�����N�^�[��ҋ@�ς݂ɕύX���܂�
    /// ��Ƀ^�[�����I��������ۂɎg�p���܂�
    /// </summary>
    public void ApplyAllPlayerWaitEnd()
    {
        foreach( Player player in _players )
        {
            player.tmpParam.isEndCommand[(int)Character.BaseCommand.COMMAND_WAIT] = true;
        }
    }

    /// <summary>
    /// �_���[�W�\����K�����܂�
    /// </summary>
    /// <param name="attacker">�U���L�����N�^�[</param>
    /// <param name="target">�W�I�L�����N�^�[</param>
    public void ApplyDamageExpect(Character attacker, Character target)
    {
        if (target == null)
        {
            return;
        }

        target.tmpParam.expectedChangeHP = Mathf.Min(target.param.Def - attacker.param.Atk, 0);
    }

    public void ResetTmpParamAllCharacter()
    {
        foreach (Player player in _players)
        {
            player.tmpParam.Reset();
        }
        foreach (Enemy enemy in _enemies)
        {
            enemy.tmpParam.Reset();
        }
    }

    /// <summary>
    /// �_���[�W�\�������Z�b�g���܂�
    /// </summary>
    /// <param name="attacker">�U���L�����N�^�[</param>
    /// <param name="target">�W�I�L�����N�^�[</param>
    public void ResetDamageExpect(Character attacker, Character target)
    {
        if( target == null )
        {
            return;
        }

        target.tmpParam.expectedChangeHP = 0;
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