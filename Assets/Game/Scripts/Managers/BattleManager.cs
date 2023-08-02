using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using TMPro.Examples;
using UnityEngine;
using UnityEngine.TextCore.Text;
using static Character;
using static StageGrid;

public class BattleManager : Singleton<BattleManager>
{
    enum BattlePhase
    {
        BATTLE_START = 0,
        BATTLE_PLAYER_COMMAND,
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
    private PhaseManagerBase[] _phaseManagers   = new PhaseManagerBase[((int)TurnType.NUM)];
    private List<Player> _players               = new List<Player>(Constants.CHARACTER_MAX_NUM);
    private List<Enemy> _enemies                = new List<Enemy>(Constants.CHARACTER_MAX_NUM);
    private CharacterHashtable _characterHash   = new CharacterHashtable();
    private CharacterHashtable.Key _diedCharacterKey;
    private CharacterHashtable.Key _battleBossCharacterKey;
    private CharacterHashtable.Key _escortTargetCharacterKey;
    private Character _prevCharacter = null;
    private StageGrid.GridInfo _prevInfo;
    private bool _transitNextPhase = false;
    private int _phaseManagerIndex = 0;
    private int _currentStageIndex = 0;
    // ���ݑI�𒆂̃L�����N�^�[�C���f�b�N�X
    public CharacterHashtable.Key SelectCharacterInfo { get; private set; } = new CharacterHashtable.Key(CHARACTER_TAG.CHARACTER_NONE, -1);
    // �U���t�F�[�Y���ɂ����āA�U�����J�n����L�����N�^�[
    public Character AttackerCharacter { get; private set; } = null;

    override protected void Init()
    {
        if (StageGrid.Instance == null)
        {
            Instantiate(stageGridObject);
        }

        // FileReaderManager����json�t�@�C����Ǎ��݁A�e�v���C���[�A�G�ɐݒ肷��
        FileReadManager.Instance.PlayerLoad(_currentStageIndex);
        FileReadManager.Instance.EnemyLord(_currentStageIndex);

        _phaseManagers[(int)TurnType.PLAYER_TURN]   = new PlayerPhaseManager();
        _phaseManagers[(int)TurnType.ENEMY_TURN]    = new EnemyPhaseManager();
        _currentPhaseManager                        = _phaseManagers[(int)TurnType.PLAYER_TURN];

        _phase = BattlePhase.BATTLE_START;
        _diedCharacterKey = new CharacterHashtable.Key(Character.CHARACTER_TAG.CHARACTER_NONE, -1);

        // TODO : �X�e�[�W�̃t�@�C������ǂݍ���Őݒ肷��悤��
        _battleBossCharacterKey = new CharacterHashtable.Key(Character.CHARACTER_TAG.CHARACTER_NONE, -1);
        _escortTargetCharacterKey = new CharacterHashtable.Key(Character.CHARACTER_TAG.CHARACTER_NONE, -1);

        // �X�L���f�[�^�̓Ǎ�
        FileReadManager.Instance.SkillDataLord();
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
            int gridIndex               = player.param.initGridIndex;
            // �v���C���[�̉�ʏ�̈ʒu��ݒ�
            player.transform.position   = _stageGrid.GetGridCharaStandPos(gridIndex);
            // ������ݒ�
            player.transform.rotation   = rot[(int)player.param.initDir];
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
            enemy.transform.position = _stageGrid.GetGridCharaStandPos(gridIndex);
            // ������ݒ�
            enemy.transform.rotation = rot[(int)enemy.param.initDir];
            // �Ή�����O���b�h�ɗ����Ă���v���C���[�̃C���f�b�N�X��ݒ�
            _stageGrid.GetGridInfo(gridIndex).charaIndex = enemy.param.characterIndex;
        }

        // �O���b�h�����X�V
        _stageGrid.UpdateGridInfo();
    }

    override protected void OnUpdate()
    {
        // TODO : ���B���ƂŃ��t�@�N�^
        if( GameManager.instance.IsInvoking() )
        {
            return;
        }

        // ���݂̃O���b�h��ɑ��݂���L�����N�^�[�����X�V
        StageGrid.GridInfo info;
        _stageGrid.FetchCurrentGridInfo(out info);
        if(_prevInfo.charaStandPos != info.charaStandPos)
        {
            BattleCameraController.Instance.SetLookAtBasedOnSelectCursor(info.charaStandPos);
            _prevInfo = info;
        } 

        SelectCharacterInfo = new CharacterHashtable.Key(info.characterTag, info.charaIndex);
        // �L�����N�^�[�̃p�����[�^�\���̍X�V
        UpdateCharacterParameter();

        if (BattleUISystem.Instance.StageClear.isActiveAndEnabled) return;

        if (BattleUISystem.Instance.GameOver.isActiveAndEnabled) return;
        
        // �t�F�[�Y�}�l�[�W�����X�V
        _transitNextPhase = _currentPhaseManager.Update();
    }

    override protected void OnLateUpdate()
    {
        if (BattleUISystem.Instance.StageClear.isActiveAndEnabled) return;

        if (BattleUISystem.Instance.GameOver.isActiveAndEnabled) return;

        // �����A�S�Ń`�F�b�N���s��
        if( CheckVictoryOrDefeat(_diedCharacterKey) ) {  return; }

        if (!_transitNextPhase)
        {
            _currentPhaseManager.LateUpdate();
        }
        else
        {
            // �ꎞ�p�����[�^�����Z�b�g
            ResetTmpParamAllCharacter();

            // ���̃}�l�[�W���ɐ؂�ւ���
            _phaseManagerIndex = (_phaseManagerIndex + 1) % (int)TurnType.NUM;
            _currentPhaseManager = _phaseManagers[_phaseManagerIndex];
            _currentPhaseManager.Init();
        }
    }

    /// <summary>
    /// �I���O���b�h��ɂ�����L�����N�^�[�̃p�����[�^�����X�V���܂�
    /// </summary>
    void UpdateCharacterParameter()
    {
        var BattleUI                = BattleUISystem.Instance;
        Character selectCharacter   = GetCharacterFromHashtable(SelectCharacterInfo);
        bool isAttaking             = IsAttackPhaseState();

        BattleUI.PlayerParameter.SetAttacking(false);
        BattleUI.EnemyParameter.SetAttacking(false);

        // �U���ΏۑI����
        if (isAttaking)
        {
            Debug.Assert(AttackerCharacter != null);

            // ��ʍ\���͈ȉ��̒ʂ�
            //   ��        �E
            // PLAYER �� ENEMY
            // OTHER  �� ENEMY
            // PLAYER �� OTHER
            if ( AttackerCharacter.param.characterTag != Character.CHARACTER_TAG.CHARACTER_ENEMY )
            {
                BattleUI.PlayerParameter.SetCharacter(AttackerCharacter);
                BattleUI.PlayerParameter.SetAttacking(true);
                BattleUI.EnemyParameter.SetCharacter(selectCharacter);
            }
            else
            {
                BattleUI.PlayerParameter.SetCharacter(selectCharacter);
                BattleUI.EnemyParameter.SetCharacter(AttackerCharacter);
                BattleUI.EnemyParameter.SetAttacking(true);
            }
        }
        else
        {
            // ��1�t���[������gameObject�̃A�N�e�B�u�؂�ւ��𕡐���s���Ɛ��������f����Ȃ����߁A���ʂ������ċC�����������ȉ��̔��蕶��p����
            BattleUI.TogglePlayerParameter(selectCharacter != null && selectCharacter.param.characterTag == Character.CHARACTER_TAG.CHARACTER_PLAYER);
            BattleUI.ToggleEnemyParameter(selectCharacter != null && selectCharacter.param.characterTag == Character.CHARACTER_TAG.CHARACTER_ENEMY);

            // �p�����[�^�\�����X�V
            if (selectCharacter != null)
            {
                if ( selectCharacter.param.characterTag == Character.CHARACTER_TAG.CHARACTER_PLAYER )
                {
                    BattleUI.PlayerParameter.SetCharacter(selectCharacter);
                }
                else
                {
                    BattleUI.EnemyParameter.SetCharacter(selectCharacter);
                }
            }

            // �O�t���[���őI�������L�����ƈقȂ�ꍇ�̓��C���[�����ɖ߂�
            if (_prevCharacter != null && _prevCharacter != selectCharacter)
            {
                _prevCharacter.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Character"));
            }
        }

        // �I�����Ă���L�����N�^�[�̃��C���[���p�����[�^UI�\���̂��߂Ɉꎞ�I�ɕύX
        if (selectCharacter != null && _prevCharacter != selectCharacter)
        {
            selectCharacter.gameObject.SetLayerRecursively(LayerMask.NameToLayer("ParamRender"));
        }

        _prevCharacter = selectCharacter;
    }

    /// <summary>
    /// �Ώۂ̃L�����N�^�[�Q���S�ł��Ă��邩���m�F���܂�
    /// </summary>
    bool CheckCharacterAnnihilated(Character.CHARACTER_TAG characterTag)
    {
        bool isAnnihilated = true;

        switch (characterTag)
        {
            case CHARACTER_TAG.CHARACTER_PLAYER:
                foreach (Player player in _players)
                {
                    if (!player.IsDead()) { isAnnihilated = false; break; }
                }
                break;
            case CHARACTER_TAG.CHARACTER_ENEMY:
                foreach (Enemy enemy in _enemies)
                {
                    if (!enemy.IsDead()) { isAnnihilated = false; break; }
                }
                break;
            case CHARACTER_TAG.CHARACTER_OTHER:
                // TODO : �K�v�ɂȂ�Ύ���
                isAnnihilated = false;
                break;
        }

        return isAnnihilated;
    }

    /// <summary>
    /// �����A�s�픻����s���܂�
    /// </summary>
    /// <param name="diedCharacterKey">���S�����L�����N�^�[�̃n�b�V���L�[</param>
    /// <returns>�����A�s�폈���ɑJ�ڂ��邩�ۂ�</returns>
    bool CheckVictoryOrDefeat(CharacterHashtable.Key diedCharacterKey)
    {
        if (diedCharacterKey.characterTag != CHARACTER_TAG.CHARACTER_NONE)
        {
            // �X�e�[�W�Ƀ{�X���ݒ肳��Ă��邩�̃`�F�b�N
            if(_battleBossCharacterKey.characterTag != CHARACTER_TAG.CHARACTER_NONE)
            {
                if( diedCharacterKey == _battleBossCharacterKey )
                {
                    // �X�e�[�W�N���A�ɑJ��
                    StartStageClearAnim();

                    return true;
                }
            }

            if(_escortTargetCharacterKey.characterTag != CHARACTER_TAG.CHARACTER_NONE)
            {
                if (diedCharacterKey == _escortTargetCharacterKey)
                {
                    // �Q�[���I�[�o�[�ɑJ��
                    StartGameOverAnim();

                    return true;
                }
            }
            
            if (CheckCharacterAnnihilated(diedCharacterKey.characterTag))
            {
                if (diedCharacterKey.characterTag == CHARACTER_TAG.CHARACTER_ENEMY)
                {
                    // �X�e�[�W�N���A�ɑJ��
                    StartStageClearAnim();
                }
                else if (diedCharacterKey.characterTag == CHARACTER_TAG.CHARACTER_PLAYER)
                {
                    // �Q�[���I�[�o�[�ɑJ��
                    StartGameOverAnim();
                }

                return true;
            }
            else
            {
                ResetDiedCharacter();
            }
        }

        return false;
    }

    public IEnumerator Battle()
    {
        yield return null;
    }

    /// <summary>
    /// �v���C���[�����X�g�ƃn�b�V���ɓo�^���܂�
    /// </summary>
    /// <param name="player">�o�^����v���C���[</param>
    public void AddPlayerToList( Player player )
    {
        var param = player.param;
        CharacterHashtable.Key key = new CharacterHashtable.Key(param.characterTag, param.characterIndex);

        _players.Add( player );
        _characterHash.Add(key, player);
    }

    /// <summary>
    /// �G�����X�g�ƃn�b�V���ɓo�^���܂�
    /// </summary>
    /// <param name="enemy">�o�^����G</param>
    public void AddEnemyToList( Enemy enemy )
    {
        var param = enemy.param;
        CharacterHashtable.Key key = new CharacterHashtable.Key(param.characterTag, param.characterIndex);

        _enemies.Add( enemy );
        _characterHash.Add(key, enemy);
    }

    /// <summary>
    /// �Y���L�����N�^�[�����S�����ۂȂǂɃ��X�g����Ώۂ��폜���܂�
    /// </summary>
    /// <param name="player">�폜�Ώۂ̃v���C���[</param>
    public void RemovePlayerFromList(Player player)
    {
        _players.Remove(player);
        _characterHash.Remove(player);
    }

    /// <summary>
    /// �Y���L�����N�^�[�����S�����ۂȂǂɃ��X�g����Ώۂ��폜���܂�
    /// </summary>
    /// <param name="enemy">�폜�Ώۂ̓G</param>
    public void RemoveEnemyFromList( Enemy enemy )
    {
        _enemies.Remove(enemy);
        _characterHash.Remove(enemy);
    }

    /// <summary>
    /// �X�e�[�W�O���b�h�X�N���v�g��o�^���܂�
    /// </summary>
    /// <param name="script">�o�^����X�N���v�g</param>
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

        BattleUISystem.Instance.TogglePlayerParameter(true);
        BattleUISystem.Instance.ToggleEnemyParameter(true);
    }

    /// <summary>
    /// ���߂̐퓬�Ŏ��S�����L�����N�^�[�̃L�����N�^�[�^�O��ݒ肵�܂�
    /// </summary>
    /// <param name="tag">���S�����L�����N�^�[�̃L�����N�^�[�^�O</param>
    public void SetDiedCharacterKey( CharacterHashtable.Key key ) { _diedCharacterKey = key; }

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

        var param = AttackerCharacter.param;

        // �I�����Ă���^�O�A�y�уC���f�b�N�X���U���ΏۃL�������猳�ɖ߂�
        SelectCharacterInfo = new CharacterHashtable.Key(param.characterTag, param.characterIndex);

        AttackerCharacter.gameObject.SetLayerRecursively(LayerMask.NameToLayer("Character"));
        AttackerCharacter = null;
    }

    /// <summary>
    /// ���S�L�����N�^�[�̃L�����N�^�[�^�O�����Z�b�g���܂�
    /// </summary>
    public void ResetDiedCharacter()
    { 
        _diedCharacterKey.characterTag      = CHARACTER_TAG.CHARACTER_NONE;
        _diedCharacterKey.characterIndex    = -1;
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
    /// �n�b�V���e�[�u������w��̃^�O�ƃC���f�b�N�X���L�[�Ƃ���L�����N�^�[���擾���܂�
    /// </summary>
    /// <param name="tag">�L�����N�^�[�^�O</param>
    /// <param name="index">�L�����N�^�[�C���f�b�N�X</param>
    /// <returns>�w��̃L�[�ɑΉ�����L�����N�^�[</returns>
    public Character GetCharacterFromHashtable( CHARACTER_TAG tag, int index )
    {
        if (tag == CHARACTER_TAG.CHARACTER_NONE || index < 0) return null;
        CharacterHashtable.Key hashKey = new CharacterHashtable.Key( tag, index );

        return _characterHash.Get(hashKey) as Character;
    }

    /// <summary>
    /// �n�b�V���e�[�u������w��̃^�O�ƃC���f�b�N�X���L�[�Ƃ���L�����N�^�[���擾���܂�
    /// </summary>
    /// <param name="key">�n�b�V���L�[</param>
    /// <returns>�w��̃L�[�ɑΉ�����L�����N�^�[</returns>
    public Character GetCharacterFromHashtable(CharacterHashtable.Key key)
    {
        if (key.characterTag == CHARACTER_TAG.CHARACTER_NONE || key.characterIndex < 0) return null;

        return _characterHash.Get( key ) as Character;
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
    /// ���ݑI�����Ă���O���b�h��̃L�����N�^�[���擾���܂�
    /// </summary>
    /// <returns>�I�����Ă���O���b�h��̃L�����N�^�[</returns>
    public Character GetSelectCharacter()
    {
        StageGrid.GridInfo info;
        StageGrid.Instance.FetchCurrentGridInfo(out info);

        return GetCharacterFromHashtable(info.characterTag, info.charaIndex);
    }

    /// <summary>
    /// �v���C���[�����X�g���珇�ԂɎ擾���܂�
    /// </summary>
    /// <returns>�v���C���[�L�����N�^�[</returns>
    public IEnumerable<Player> GetPlayerEnumerable()
    {
        foreach (Player player in _players)
        {
            yield return player;
        }

        yield break;
    }

    /// <summary>
    /// �G�����X�g���珇�ԂɎ擾���܂�
    /// </summary>
    /// <returns>�G�L�����N�^�[</returns>
    public IEnumerable<Enemy> GetEnemyEnumerable()
    {
        foreach (Enemy enemy in _enemies)
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

        int targetDef   = (int)((target.param.Def + target.modifiedParam.Def) * target.skillModifiedParam.DefMagnification);
        int attackerAtk = (int)((attacker.param.Atk + attacker.modifiedParam.Atk) * attacker.skillModifiedParam.AtkMagnification);
        int changeHP    = (targetDef - attackerAtk);

        target.tmpParam.expectedChangeHP        = Mathf.Min(changeHP, 0);
        target.tmpParam.totalExpectedChangeHP   = Mathf.Min(changeHP * attacker.skillModifiedParam.AtkNum, 0);
    }

    /// <summary>
    /// �S�ẴL�����N�^�[�̈ꎞ�p�����[�^�����Z�b�g���܂�
    /// </summary>
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
    /// �w��̃L�����N�^�[�Q�̃A�N�V�����Q�[�W���񕜂����܂�
    /// </summary>
    /// <param name="tag">�L�����N�^�[�Q�̃^�O</param>
    public void RecoveryActionGaugeForGroup( CHARACTER_TAG tag )
    {
        switch( tag )
        {
            case CHARACTER_TAG.CHARACTER_PLAYER:
                foreach (Player player in _players)
                {
                    player.RecoveryActionGauge();
                }
                break;
            case CHARACTER_TAG.CHARACTER_ENEMY:
                foreach (Enemy enemy in _enemies)
                {
                    enemy.RecoveryActionGauge();
                }
                break;
            case CHARACTER_TAG.CHARACTER_OTHER:
                // TODO : OTHER���쐬����ǉ�
                break;
        }
    }

    /// <summary>
    /// �I����Ԃ��ǂ����𔻒肵�܂�
    /// </summary>
    /// <returns>true : �I��</returns>
    public bool isEnd()
    {
        return _phase == BattlePhase.BATTLE_END;
    }

    /// <summary>
    /// �X�e�[�W�N���A����UI�ƃA�j���[�V������\�����܂�
    /// </summary>
    public void StartStageClearAnim()
    {
        BattleUISystem.Instance.ToggleStageClearUI(true);
        BattleUISystem.Instance.StartStageClearAnim();
    }

    /// <summary>
    /// �Q�[���I�[�o�[����UI�ƃA�j���[�V������\�����܂�
    /// </summary>
    public void StartGameOverAnim()
    {
        BattleUISystem.Instance.ToggleGameOverUI(true);
        BattleUISystem.Instance.StartGameOverAnim();
    }
}