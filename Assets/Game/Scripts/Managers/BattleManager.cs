using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Frontier.Character;

namespace Frontier
{
    public class BattleManager : MonoBehaviour
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

        [Header("�X�L���R���g���[��")]
        [SerializeField]
        private SkillController _skillCtrl;

        [Header("�t�@�C���Ǎ��}�l�[�W��")]
        [SerializeField]
        private FileReadManager _fileReadMgr;

        private BattlePhase _phase;
        private BattleCameraController _battleCameraCtrl;
        private BattleTimeScaleController _battleTimeScaleCtrl = new();
        private StageController _stageCtrl;
        private PhaseManagerBase _currentPhaseManager;
        private PhaseManagerBase[] _phaseManagers = new PhaseManagerBase[((int)TurnType.NUM)];
        private List<Player> _players = new List<Player>(Constants.CHARACTER_MAX_NUM);
        private List<Enemy> _enemies = new List<Enemy>(Constants.CHARACTER_MAX_NUM);
        private List<Other> _others = new List<Other>(Constants.CHARACTER_MAX_NUM);
        private CharacterHashtable _characterHash = new CharacterHashtable();
        private CharacterHashtable.Key _diedCharacterKey;
        private CharacterHashtable.Key _battleBossCharacterKey;
        private CharacterHashtable.Key _escortTargetCharacterKey;
        private bool _transitNextPhase = false;
        private int _phaseManagerIndex = 0;
        private int _currentStageIndex = 0;
        // ���ݑI�𒆂̃L�����N�^�[�C���f�b�N�X
        public CharacterHashtable.Key SelectCharacterInfo { get; private set; } = new CharacterHashtable.Key(CHARACTER_TAG.NONE, -1);
        public BattleTimeScaleController TimeScaleCtrl => _battleTimeScaleCtrl;
        public SkillController SkillCtrl => _skillCtrl;

        void Awake()
        {
            var btlCameraObj = GameObject.FindWithTag("MainCamera");
            if ( btlCameraObj != null ) 
            {
                _battleCameraCtrl = btlCameraObj.GetComponent<BattleCameraController>();
            }
            _phaseManagers[(int)TurnType.PLAYER_TURN] = new PlayerPhaseManager();
            _phaseManagers[(int)TurnType.ENEMY_TURN] = new EnemyPhaseManager();
            _currentPhaseManager = _phaseManagers[(int)TurnType.PLAYER_TURN];

            _phase = BattlePhase.BATTLE_START;
            _diedCharacterKey = new CharacterHashtable.Key(CHARACTER_TAG.NONE, -1);

            // TODO : �X�e�[�W�̃t�@�C������ǂݍ���Őݒ肷��悤��
            _battleBossCharacterKey = new CharacterHashtable.Key(CHARACTER_TAG.NONE, -1);
            _escortTargetCharacterKey = new CharacterHashtable.Key(CHARACTER_TAG.NONE, -1);

            // �X�L���f�[�^�̓Ǎ�
            _fileReadMgr.SkillDataLord();
        }

        void Start()
        {
            Debug.Assert(_skillCtrl != null);

            _stageCtrl = ManagerProvider.Instance.GetService<StageController>();
            _stageCtrl.Init(this);
            _skillCtrl.Init(this);

            // FileReaderManager����json�t�@�C����Ǎ��݁A�e�v���C���[�A�G�ɐݒ肷�� ���f�o�b�O�V�[���͏��O
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (!Methods.IsDebugScene())
#endif
            {
                _fileReadMgr.PlayerLoad(_currentStageIndex, _stageCtrl.GetGridSize());
                _fileReadMgr.EnemyLord(_currentStageIndex, _stageCtrl.GetGridSize());
            }

            for (int i = 0; i < _phaseManagers.Length; ++i) _phaseManagers[i].Regist(this, _stageCtrl);
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
                int gridIndex = player.param.initGridIndex;
                // �v���C���[�̉�ʏ�̈ʒu��ݒ�
                player.transform.position = _stageCtrl.GetGridCharaStandPos(gridIndex);
                // ������ݒ�
                player.transform.rotation = rot[(int)player.param.initDir];
                // �Ή�����O���b�h�ɗ����Ă���v���C���[�̃C���f�b�N�X��ݒ�
                _stageCtrl.GetGridInfo(gridIndex).charaIndex = player.param.characterIndex;
            }

            // �e�G�l�~�[�L�����N�^�[�̈ʒu��ݒ�
            for (int i = 0; i < _enemies.Count; ++i)
            {
                Enemy enemy = _enemies[i];
                // �X�e�[�W�J�n���̃v���C���[�����ʒu(�C���f�b�N�X)���L���b�V��
                int gridIndex = enemy.param.initGridIndex;
                // �G�l�~�[�̉�ʏ�̈ʒu��ݒ�
                enemy.transform.position = _stageCtrl.GetGridCharaStandPos(gridIndex);
                // ������ݒ�
                enemy.transform.rotation = rot[(int)enemy.param.initDir];
                // �Ή�����O���b�h�ɗ����Ă���v���C���[�̃C���f�b�N�X��ݒ�
                _stageCtrl.GetGridInfo(gridIndex).charaIndex = enemy.param.characterIndex;
            }

            // �O���b�h�����X�V
            _stageCtrl.UpdateGridInfo();
        }

        void Update()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (!Methods.IsDebugScene())
#endif
            {
                if (GameManager.instance.IsInvoking())
                {
                    return;
                }
            }

            // ���݂̃O���b�h��ɑ��݂���L�����N�^�[�����X�V
            Stage.GridInfo info;
            _stageCtrl.FetchCurrentGridInfo(out info);
            _battleCameraCtrl.SetLookAtBasedOnSelectCursor(info.charaStandPos);

            SelectCharacterInfo = new CharacterHashtable.Key(info.characterTag, info.charaIndex);

            if (BattleUISystem.Instance.StageClear.isActiveAndEnabled) return;

            if (BattleUISystem.Instance.GameOver.isActiveAndEnabled) return;

            // �t�F�[�Y�}�l�[�W�����X�V
            _transitNextPhase = _currentPhaseManager.Update();
        }

        void LateUpdate()
        {
            if (BattleUISystem.Instance.StageClear.isActiveAndEnabled) return;

            if (BattleUISystem.Instance.GameOver.isActiveAndEnabled) return;

            // �����A�S�Ń`�F�b�N���s��
            if (CheckVictoryOrDefeat(_diedCharacterKey)) { return; }

            // �t�F�[�Y�ړ��̐���
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
        /// �Ώۂ̃L�����N�^�[�Q���S�ł��Ă��邩���m�F���܂�
        /// </summary>
        bool CheckCharacterAnnihilated(Character.CHARACTER_TAG characterTag)
        {
            bool isAnnihilated = true;

            switch (characterTag)
            {
                case CHARACTER_TAG.PLAYER:
                    foreach (Player player in _players)
                    {
                        if (!player.IsDead()) { isAnnihilated = false; break; }
                    }
                    break;
                case CHARACTER_TAG.ENEMY:
                    foreach (Enemy enemy in _enemies)
                    {
                        if (!enemy.IsDead()) { isAnnihilated = false; break; }
                    }
                    break;
                case CHARACTER_TAG.OTHER:
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
            if (diedCharacterKey.characterTag != CHARACTER_TAG.NONE)
            {
                // �X�e�[�W�Ƀ{�X���ݒ肳��Ă��邩�̃`�F�b�N
                if (_battleBossCharacterKey.characterTag != CHARACTER_TAG.NONE)
                {
                    if (diedCharacterKey == _battleBossCharacterKey)
                    {
                        // �X�e�[�W�N���A�ɑJ��
                        StartStageClearAnim();

                        return true;
                    }
                }

                if (_escortTargetCharacterKey.characterTag != CHARACTER_TAG.NONE)
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
                    if (diedCharacterKey.characterTag == CHARACTER_TAG.ENEMY)
                    {
                        // �X�e�[�W�N���A�ɑJ��
                        StartStageClearAnim();
                    }
                    else if (diedCharacterKey.characterTag == CHARACTER_TAG.PLAYER)
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
        public void AddPlayerToList(Player player)
        {
            var param = player.param;
            CharacterHashtable.Key key = new CharacterHashtable.Key(param.characterTag, param.characterIndex);

            _players.Add(player);
            _characterHash.Add(key, player);
        }

        /// <summary>
        /// �G�����X�g�ƃn�b�V���ɓo�^���܂�
        /// </summary>
        /// <param name="enemy">�o�^����G</param>
        public void AddEnemyToList(Enemy enemy)
        {
            var param = enemy.param;
            CharacterHashtable.Key key = new CharacterHashtable.Key(param.characterTag, param.characterIndex);

            _enemies.Add(enemy);
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
        public void RemoveEnemyFromList(Enemy enemy)
        {
            _enemies.Remove(enemy);
            _characterHash.Remove(enemy);
        }

        /// <summary>
        /// �X�e�[�W�O���b�h�X�N���v�g��o�^���܂�
        /// </summary>
        /// <param name="script">�o�^����X�N���v�g</param>
        public void registStageController(Stage.StageController script)
        {
            _stageCtrl = script;
        }

        /// <summary>
        /// ���߂̐퓬�Ŏ��S�����L�����N�^�[�̃L�����N�^�[�^�O��ݒ肵�܂�
        /// </summary>
        /// <param name="tag">���S�����L�����N�^�[�̃L�����N�^�[�^�O</param>
        public void SetDiedCharacterKey(CharacterHashtable.Key key) { _diedCharacterKey = key; }

        /// <summary>
        /// ���S�L�����N�^�[�̃L�����N�^�[�^�O�����Z�b�g���܂�
        /// </summary>
        public void ResetDiedCharacter()
        {
            _diedCharacterKey.characterTag = CHARACTER_TAG.NONE;
            _diedCharacterKey.characterIndex = -1;
        }

        /// <summary>
        /// �퓬�J�����R���g���[�����擾���܂�
        /// </summary>
        /// <returns>�퓬�J�����R���g���[��</returns>
        public BattleCameraController GetCameraController()
        {
            return _battleCameraCtrl;
        }

        /// <summary>
        /// �n�b�V���e�[�u������w��̃^�O�ƃC���f�b�N�X���L�[�Ƃ���L�����N�^�[���擾���܂�
        /// </summary>
        /// <param name="tag">�L�����N�^�[�^�O</param>
        /// <param name="index">�L�����N�^�[�C���f�b�N�X</param>
        /// <returns>�w��̃L�[�ɑΉ�����L�����N�^�[</returns>
        public Character GetCharacterFromHashtable(CHARACTER_TAG tag, int index)
        {
            if (tag == CHARACTER_TAG.NONE || index < 0) return null;
            CharacterHashtable.Key hashKey = new CharacterHashtable.Key(tag, index);

            return _characterHash.Get(hashKey) as Character;
        }

        /// <summary>
        /// �n�b�V���e�[�u������w��̃^�O�ƃC���f�b�N�X���L�[�Ƃ���L�����N�^�[���擾���܂�
        /// </summary>
        /// <param name="key">�n�b�V���L�[</param>
        /// <returns>�w��̃L�[�ɑΉ�����L�����N�^�[</returns>
        public Character GetCharacterFromHashtable(CharacterHashtable.Key key)
        {
            if (key.characterTag == CHARACTER_TAG.NONE || key.characterIndex < 0) return null;

            return _characterHash.Get(key) as Character;
        }

        /// <summary>
        /// �S�Ă̍s���\�L�����N�^�[�̍s�����I���������𔻒肵�܂�
        /// </summary>
        /// <returns>�S�Ă̍s���\�L�����N�^�[�̍s�����I��������</returns>
        public bool IsEndAllCharacterWaitCommand()
        {
            foreach (Player player in _players)
            {
                if (!player.tmpParam.isEndCommand[(int)Character.Command.COMMAND_TAG.WAIT])
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
            Stage.GridInfo info;
            _stageCtrl.FetchCurrentGridInfo(out info);

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
        /// �w�肳�ꂽ�L�����N�^�[�^�O�̑����j�b�g�����擾���܂�
        /// </summary>
        /// <param name="tag">�w�肷��L�����N�^�[�̃^�O</param>
        /// <returns>�w��^�O�̑����j�b�g��</returns>
        public int GetCharacterCount(CHARACTER_TAG tag)
        {
            switch (tag)
            {
                case CHARACTER_TAG.PLAYER: return _players.Count;
                case CHARACTER_TAG.ENEMY: return _enemies.Count;
                default: return _others.Count;
            }
        }

        /// <summary>
        /// �S�Ẵv���C���[�L�����N�^�[��ҋ@�ς݂ɕύX���܂�
        /// ��Ƀ^�[�����I��������ۂɎg�p���܂�
        /// </summary>
        public void ApplyAllPlayerWaitEnd()
        {
            foreach (Player player in _players)
            {
                player.tmpParam.isEndCommand[(int)Character.Command.COMMAND_TAG.WAIT] = true;
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

            int targetDef = (int)Mathf.Floor((target.param.Def + target.modifiedParam.Def) * target.skillModifiedParam.DefMagnification);
            int attackerAtk = (int)Mathf.Floor((attacker.param.Atk + attacker.modifiedParam.Atk) * attacker.skillModifiedParam.AtkMagnification);
            int changeHP = (targetDef - attackerAtk);

            target.tmpParam.expectedChangeHP = Mathf.Min(changeHP, 0);
            target.tmpParam.totalExpectedChangeHP = Mathf.Min(changeHP * attacker.skillModifiedParam.AtkNum, 0);
        }

        /// <summary>
        /// �S�ẴL�����N�^�[�̈ꎞ�p�����[�^�����Z�b�g���܂�
        /// </summary>
        public void ResetTmpParamAllCharacter()
        {
            foreach (Player player in _players)
            {
                player.BePossibleAction();
            }
            foreach (Enemy enemy in _enemies)
            {
                enemy.BePossibleAction();
            }
        }

        /// <summary>
        /// �_���[�W�\�������Z�b�g���܂�
        /// </summary>
        /// <param name="attacker">�U���L�����N�^�[</param>
        /// <param name="target">�W�I�L�����N�^�[</param>
        public void ResetDamageExpect(Character attacker, Character target)
        {
            if (target == null)
            {
                return;
            }

            target.tmpParam.expectedChangeHP = 0;
        }

        /// <summary>
        /// �w��̃L�����N�^�[�Q�̃A�N�V�����Q�[�W���񕜂����܂�
        /// </summary>
        /// <param name="tag">�L�����N�^�[�Q�̃^�O</param>
        public void RecoveryActionGaugeForGroup(CHARACTER_TAG tag)
        {
            switch (tag)
            {
                case CHARACTER_TAG.PLAYER:
                    foreach (Player player in _players)
                    {
                        player.RecoveryActionGauge();
                    }
                    break;
                case CHARACTER_TAG.ENEMY:
                    foreach (Enemy enemy in _enemies)
                    {
                        enemy.RecoveryActionGauge();
                    }
                    break;
                case CHARACTER_TAG.OTHER:
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
}