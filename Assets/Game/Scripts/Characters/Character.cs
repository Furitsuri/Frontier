using Frontier.Stage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static Frontier.Character;
using static Frontier.SkillsData;

namespace Frontier
{
    [SerializeField]
    public class Character : MonoBehaviour
    {
        public class Command
        {
            public enum COMMAND_TAG
            {
                MOVE = 0,
                ATTACK,
                WAIT,

                NUM,
            }

            public static bool IsExecutableCommandBase(Character character)
            {
                if (!character.tmpParam.IsExecutableCommand(COMMAND_TAG.WAIT)) return false;

                return true;
            }

            public static bool IsExecutableMoveCommand(Character character, StageController stageCtrl)
            {
                if (!IsExecutableCommandBase(character)) return false;

                return !character.tmpParam.isEndCommand[(int)COMMAND_TAG.MOVE];
            }

            public static bool IsExecutableAttackCommand(Character character, StageController stageCtrl)
            {
                if (!IsExecutableCommandBase(character)) return false;

                if( character.tmpParam.isEndCommand[(int)COMMAND_TAG.ATTACK] ) return false;

                // ���݃O���b�h����U���\�ȑΏۂ̋���O���b�h�����݂���΁A���s�\
                bool isExecutable = stageCtrl.RegistAttackAbleInfo(character.tmpParam.gridIndex, character.param.attackRange, character.param.characterTag);
           
                // ���s�s�ł���ꍇ�͓o�^�����U������S�ăN���A
                if( !isExecutable )
                {
                    stageCtrl.ClearAttackableInfo();
                }

                return isExecutable;
            }

            public static bool IsExecutableWaitCommand(Character character, StageController stageCtrl)
            {
                return IsExecutableCommandBase(character);
            }
        }

        public enum CHARACTER_TAG
        {
            NONE = -1,
            PLAYER,
            ENEMY,
            OTHER,

            NUM,
        }

        /// <summary>
        /// �ړ��^�C�v
        /// </summary>
        public enum MOVE_TYPE
        {
            NORMAL = 0,
            HORSE,
            FLY,
            HEAVY,

            NUM,
        }

        /// <summary>
        /// �ߐڍU���X�V�p�t�F�C�Y
        /// </summary>
        public enum CLOSED_ATTACK_PHASE
        {
            NONE = -1,

            CLOSINGE,
            ATTACK,
            DISTANCING,

            NUM,
        }

        /// <summary>
        /// �p���B�X�V�p�t�F�C�Y
        /// </summary>
        public enum PARRY_PHASE
        {
            NONE = -1,

            EXEC_PARRY,
            AFTER_ATTACK,

            NUM,
        }

        // �L�����N�^�[�̎��p�����[�^
        [System.Serializable]
        public struct Parameter
        {
            // �L�����N�^�[�^�C�v
            public CHARACTER_TAG characterTag;
            // �L�����N�^�[�ԍ�
            public int characterIndex;
            // �ő�HP
            public int MaxHP;
            // ����HP
            public int CurHP;
            // �U����
            public int Atk;
            // �h���
            public int Def;
            // �ړ������W
            public int moveRange;
            // �U�������W
            public int attackRange;
            // �A�N�V�����Q�[�W�ő�l
            public int maxActionGauge;
            // �A�N�V�����Q�[�W���ݒl
            public int curActionGauge;
            // �A�N�V�����Q�[�W�񕜒l
            public int recoveryActionGauge;
            // �A�N�V�����Q�[�W����l
            public int consumptionActionGauge;
            // �X�e�[�W�J�n���O���b�h���W(�C���f�b�N�X)
            public int initGridIndex;
            // �X�e�[�W�J�n������
            public Constants.Direction initDir;
            // �������Ă���X�L��
            public SkillsData.ID[] equipSkills;

            /// <summary>
            /// �w��̃X�L�����L�����ۂ���Ԃ��܂�
            /// </summary>
            /// <param name="index">�w��C���f�b�N�X</param>
            /// <returns>�L�����ۂ�</returns>
            public bool IsValidSkill(int index)
            {
                return SkillsData.ID.SKILL_NONE < equipSkills[index] && equipSkills[index] < SkillsData.ID.SKILL_NUM;
            }

            /// <summary>
            /// �A�N�V�����Q�[�W����ʂ����Z�b�g���܂�
            /// </summary>
            public void ResetConsumptionActionGauge()
            {
                consumptionActionGauge = 0;
            }
        }

        // �o�t�E�f�o�t�Ȃǂŏ�悹�����p�����[�^
        public struct ModifiedParameter
        {
            // �U����
            public int Atk;
            // �h���
            public int Def;
            // �ړ������W
            public int moveRange;
            // �A�N�V�����Q�[�W�񕜒l
            public int recoveryActionGauge;

            public void Reset()
            {
                Atk = 0; Def = 0; moveRange = 0; recoveryActionGauge = 0;
            }
        }

        // �X�L���ɂ���ď�悹�����p�����[�^
        public struct SkillModifiedParameter
        {
            public int AtkNum;
            public float AtkMagnification;
            public float DefMagnification;

            public void Reset()
            {
                AtkNum = 1; AtkMagnification = 1f; DefMagnification = 1f;
            }
        }

        // �퓬���̂ݎg�p����p�����[�^
        public struct TmpParameter
        {
            // �Y���R�}���h�̏I���t���O
            public bool[] isEndCommand;
            // �Y���X�L���̎g�p�t���O
            public bool[] isUseSkills;
            // ���݈ʒu�������O���b�h�C���f�b�N�X
            public int gridIndex;
            // 1��̍U���ɂ�����HP�̗\���ϓ���(������U���ɂ�����_���[�W���ʂ��l�����Ȃ�)
            public int expectedChangeHP;
            // �S�Ă̍U���ɂ�����HP�̗\���ϓ���(������U���ɂ�����_���[�W���ʂ��l������)
            public int totalExpectedChangeHP;

            /// <summary>
            /// �w��R�}���h�����s�\���ۂ��𔻒肵�܂�
            /// </summary>
            /// <param name="cmdTag">�w��R�}���h�̃^�O</param>
            /// <returns>���s��</returns>
            public bool IsExecutableCommand(Command.COMMAND_TAG cmdTag)
            {
                return !isEndCommand[(int)cmdTag];
            }

            /// <summary>
            /// �X�L���̎g�p�t���O�����Z�b�g���܂�
            /// </summary>
            public void ResetUseSkill()
            {
                for (int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i)
                {
                    isUseSkills[i] = false;
                }
            }

            /// <summary>
            /// �S�Ẵp�����[�^�����Z�b�g���܂�
            /// </summary>
            public void Reset()
            {
                for (int i = 0; i < (int)Command.COMMAND_TAG.NUM; ++i)
                {
                    isEndCommand[i] = false;
                }

                totalExpectedChangeHP = expectedChangeHP = 0;
            }
        }

        [System.Serializable]
        public struct CameraParameter
        {
            [Header("�U���V�[�P���X���J�����I�t�Z�b�g")]
            public Vector3 OffsetOnAtkSequence;
            [Header("�p�����[�^�\��UI�p�J�����I�t�Z�b�g(Y���W)")]
            public float UICameraLengthY;
            [Header("�p�����[�^�\��UI�p�J�����I�t�Z�b�g(Z���W)")]
            public float UICameraLengthZ;
            // UI�\���p�J�����^�[�Q�b�g(Y����)
            public float UICameraLookAtCorrectY;

            public CameraParameter(in Vector3  offset, float lengthY, float lengthZ, float lookAtCorrectY)
            {
                OffsetOnAtkSequence     = offset;
                UICameraLengthY         = lengthY;
                UICameraLengthZ         = lengthZ;
                UICameraLookAtCorrectY  = lookAtCorrectY;
            }
        }

        [SerializeField]
        private GameObject _bulletObject;

        private bool _isTransitNextPhaseCamera  = false;
        private bool _isOrderedRotation         = false;
        private bool _isAttacked                = false;
        private float _elapsedTime              = 0f;
        private readonly TimeScale _timeScale   = new TimeScale();
        private Quaternion _orderdRotation      = Quaternion.identity;
        private List<(Material material, Color originalColor)> _textureMaterialsAndColors   = new List<(Material, Color)>();
        private List<Command.COMMAND_TAG> _executableCommands                               = new List<Command.COMMAND_TAG>();
        // �U���V�[�P���X�ɂ�����c��U����
        protected int _atkRemainingNum          = 0;
        protected BattleManager _btlMgr         = null;
        protected StageController _stageCtrl    = null;
        protected Character _opponent           = null;
        protected Bullet _bullet                = null;
        protected CLOSED_ATTACK_PHASE _closingAttackPhase;
        protected PARRY_PHASE _parryPhase;
        public Parameter param;
        public TmpParameter tmpParam;
        public ModifiedParameter modifiedParam;
        public SkillModifiedParameter skillModifiedParam;
        public CameraParameter camParam;
        // ���S�m��t���O(�U���V�[�P���X�ɂ����Ďg�p)
        public bool IsDeclaredDead { get; set; } = false;
        // �p���B����
        public SkillParryController.JudgeResult ParryResult { get; set; } = SkillParryController.JudgeResult.NONE;
        public AnimationController AnimCtrl { get; } = new AnimationController();

        // �U���p�A�j���[�V�����^�O
        private static AnimDatas.ANIME_CONDITIONS_TAG[] AttackAnimTags = new AnimDatas.ANIME_CONDITIONS_TAG[]
        {
            AnimDatas.ANIME_CONDITIONS_TAG.SINGLE_ATTACK,
            AnimDatas.ANIME_CONDITIONS_TAG.DOUBLE_ATTACK,
            AnimDatas.ANIME_CONDITIONS_TAG.TRIPLE_ATTACK
        };

        private delegate bool IsExecutableCommand(Character character, StageController stageCtrl);
        private static IsExecutableCommand[] _executableCommandTables =
        {
            Command.IsExecutableMoveCommand,
            Command.IsExecutableAttackCommand,
            Command.IsExecutableWaitCommand,
        };

        #region PRIVATE_METHOD
        void Awake()
        {
            _timeScale.OnValueChange    = AnimCtrl.UpdateTimeScale;
            IsDeclaredDead              = false;
            param.equipSkills           = new SkillsData.ID[Constants.EQUIPABLE_SKILL_MAX_NUM];
            tmpParam.isEndCommand       = new bool[(int)Command.COMMAND_TAG.NUM];
            tmpParam.isUseSkills        = new bool[Constants.EQUIPABLE_SKILL_MAX_NUM];
            tmpParam.Reset();
            modifiedParam.Reset();
            skillModifiedParam.Reset();
            AnimCtrl.Init(GetComponent<Animator>());

            // �L�����N�^�[���f���̃}�e���A�����ݒ肳��Ă���Object���擾���A
            // Material�Ə�����Color�ݒ��ۑ�
            RegistMaterialsRecursively(this.transform, Constants.OBJECT_TAG_NAME_CHARA_SKIN_MESH);

            // �e�I�u�W�F�N�g���ݒ肳��Ă���ΐ���
            // �g�p���܂Ŕ�A�N�e�B�u�ɂ���
            if (_bulletObject != null)
            {
                GameObject bulletObject = Instantiate(_bulletObject);
                if (bulletObject != null)
                {
                    _bullet = bulletObject.GetComponent<Bullet>();
                    bulletObject.SetActive(false);
                }
            }
        }

        void Update()
        {
            // ������]����
            if (_isOrderedRotation)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, _orderdRotation, Constants.CHARACTER_ROT_SPEED * Time.deltaTime);

                float angleDiff = Quaternion.Angle(transform.rotation, _orderdRotation);
                if (Math.Abs(angleDiff) < Constants.CHARACTER_ROT_THRESHOLD)
                {
                    _isOrderedRotation = false;
                }
            }

            // �ړ��ƍU�����I�����Ă���΁A�s���s�ɑJ��
            var endCommand = tmpParam.isEndCommand;
            if (endCommand[(int)Command.COMMAND_TAG.MOVE] && endCommand[(int)Command.COMMAND_TAG.ATTACK])
            {
                BeImpossibleAction();
            }
        }

        void OnDestroy()
        {
            // �퓬���ԊǗ��N���X�̓o�^������
            _btlMgr.TimeScaleCtrl.Unregist(_timeScale);
        }

        /// <summary>
        /// �ċA��p���āA�w��̃^�O�œo�^����Ă���I�u�W�F�N�g�̃}�e���A����o�^���܂�
        /// �� �F�ύX�̍ۂɗp����
        /// </summary>
        /// <param name="parent">�I�u�W�F�N�g�̐e</param>
        /// <param name="tagName">��������^�O��</param>
        void RegistMaterialsRecursively( Transform parent, string tagName )
        {
            Transform children = parent.GetComponentInChildren<Transform>();
            foreach (Transform child in children)
            {
                if (child.CompareTag(tagName))
                {
                    // ���f���ɂ���āA�}�e���A����Mesh��SkinMesh�̗����̃p�^�[���ɓo�^����Ă���P�[�X�����邽�߁A
                    // �ǂ������������
                    var skinMeshRenderer = child.GetComponent<SkinnedMeshRenderer>();
                    if (skinMeshRenderer != null)
                    {
                        _textureMaterialsAndColors.Add((skinMeshRenderer.material, skinMeshRenderer.material.color));
                    }
                    var meshRenderer = child.GetComponent<MeshRenderer>();
                    if (meshRenderer != null)
                    {
                        _textureMaterialsAndColors.Add(( meshRenderer.material, meshRenderer.material.color));
                    }
                }

                RegistMaterialsRecursively(child, tagName);
            }
        }

        /// <summary>
        /// �w��̃p���B����N���X���C�x���g�I�������ۂɌĂяo���f���Q�[�g��ݒ肵�܂�
        /// </summary>
        /// <param name="parryCtrl">�p���B����N���X</param>
        void SubscribeParryEvent( SkillParryController parryCtrl )
        {
            parryCtrl.ProcessCompleted += ParryEventProcessCompleted;
        }

        /// <summary>
        /// �w��̃p���B����N���X���C�x���g�I�������ۂɌĂяo���f���Q�[�g�ݒ���������܂�
        /// </summary>
        /// <param name="parryCtrl">�p���B����N���X</param>
        void UnsubscribeParryEvent(SkillParryController parryCtrl)
        {
            parryCtrl.ProcessCompleted -= ParryEventProcessCompleted;
        }

        /// <summary>
        /// �p���B�C�x���g�I�����ɌĂяo�����f���Q�[�g
        /// </summary>
        /// <param name="sender">�Ăяo�����s���p���B�C�x���g�R���g���[��</param>
        /// <param name="e">�C�x���g�n���h���p�I�u�W�F�N�g(���̊֐��ł�empty)</param>
        void ParryEventProcessCompleted( object sender, SkillParryCtrlEventArgs e )
        {
            ParryResult = e.Result;

            SkillParryController parryCtrl = sender as SkillParryController;
            parryCtrl.EndParryEvent();

            UnsubscribeParryEvent(parryCtrl);
        }

        #endregion  // PRIVATE_METHOD

        #region VIRTUAL_PUBLIC_METHOD

        /// <summary>
        /// �������������s���܂�
        /// </summary>
        virtual public void Init( BattleManager btlMgr, StageController stgCtrl )
        {
            _btlMgr             = btlMgr;
            _stageCtrl          = stgCtrl;
            tmpParam.gridIndex  = param.initGridIndex;
            _elapsedTime        = 0f;

            // �퓬���ԊǗ��N���X�Ɏ��g�̎��ԊǗ��N���X��o�^
            _btlMgr.TimeScaleCtrl.Regist( _timeScale );
        }

        /// <summary>
        /// ���S�������s���܂�
        /// </summary>
        virtual public void Die() { }

        /// <summary>
        /// �ΐ푊��Ƀ_���[�W��^����C�x���g�𔭐������܂�
        /// �� �e�̒��e�ȊO�ł͋ߐڍU���A�j���[�V����������Ăяo�����݌v�ł�
        ///    �ߐڍU���L�����N�^�[�̍U���A�j���[�V�����̓K���ȃt���[���ł��̃��\�b�h�C�x���g��}�����Ă�������
        /// </summary>
        virtual public void AttackOpponentEvent()
        {
            if (_opponent == null)
            {
                Debug.Assert(false);
            }

            _isAttacked = true;
            _opponent.param.CurHP += _opponent.tmpParam.expectedChangeHP;

            //�@�_���[�W��0�̏ꍇ�̓��[�V���������Ȃ�
            if (_opponent.tmpParam.expectedChangeHP != 0)
            {
                if (_opponent.param.CurHP <= 0)
                {
                    _opponent.param.CurHP = 0;
                    _opponent.AnimCtrl.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.DIE);
                }
                // �K�[�h�X�L���g�p���͎��S���ȊO�̓_���[�W���[�V�������Đ����Ȃ�
                else if (!_opponent.IsSkillInUse(SkillsData.ID.SKILL_GUARD))
                {
                    _opponent.AnimCtrl.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.GET_HIT);
                }
            }

            // �_���[�WUI��\��
            BattleUISystem.Instance.SetDamageUIPosByCharaPos(_opponent, _opponent.tmpParam.expectedChangeHP);
            BattleUISystem.Instance.ToggleDamageUI(true);
        }

        /// <summary>
        /// �ΐ푊��̍U�����p���B(�e��)����C�x���g�𔭐������܂��@���U���A�j���[�V��������Ăяo����܂�
        /// </summary>
        virtual public void ParryOpponentEvent()
        {
            // NONE�ȊO�̌��ʂ��ʒm����Ă���͂�
            Debug.Assert(ParryResult != SkillParryController.JudgeResult.NONE);

            if (_opponent == null)
            {
                Debug.Assert(false);
            }

            if (ParryResult == SkillParryController.JudgeResult.FAILED)
            {
                return;
            }

            // ������(�W���X�g�܂�)�ɂ̓p���B����
            _opponent.ParryRecieveEvent();
        }

        /// <summary>
        /// �p���B���󂯂��ۂ̃C�x���g�𔭐������܂�
        /// </summary>
        virtual public void ParryRecieveEvent()
        {
            _timeScale.Reset();
            AnimCtrl.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.GET_HIT);
        }

        /// <summary>
        /// �e�𔭎˂��܂�
        /// MEMO : ���[�V��������C�x���g�t���O�����Ƃ��ČĂ΂�܂�
        /// </summary>
        virtual public void FireBullet()
        {
            if (_bullet == null || _opponent == null) return;

            _bullet.gameObject.SetActive(true);

            // �ˏo�n�_�A�ڕW�n�_�Ȃǂ�ݒ肵�Ēe�𔭎�
            var firingPoint = transform.position;
            firingPoint.y += camParam.UICameraLookAtCorrectY;
            _bullet.SetFiringPoint(firingPoint);
            var targetCoordinate = _opponent.transform.position;
            targetCoordinate.y += _opponent.camParam.UICameraLookAtCorrectY;
            _bullet.SetTargetCoordinate(targetCoordinate);
            var gridLength = _stageCtrl.CalcurateGridLength(tmpParam.gridIndex, _opponent.tmpParam.gridIndex);
            _bullet.SetFlightTimeFromGridLength(gridLength);
            _bullet.StartUpdateCoroutine(AttackOpponentEvent);

            // ���˂Ɠ����Ɏ��̃J�����ɑJ�ڂ�����
            _isTransitNextPhaseCamera = true;

            // ���̍U���ɂ���đ��肪�|����邩�ǂ����𔻒�
            _opponent.IsDeclaredDead = ( _opponent.param.CurHP + _opponent.tmpParam.expectedChangeHP ) <= 0;
            if( !_opponent.IsDeclaredDead && 0 < _atkRemainingNum )
            {
                --_atkRemainingNum;
                AnimCtrl.SetAnimator(AttackAnimTags[_atkRemainingNum]);
            }
        }

        /// <summary>
        /// �퓬�Ɏg�p����X�L����I�����܂�
        /// </summary>
        virtual public void SelectUseSkills(SituationType type)
        {
        }

        #endregion // VIRTUAL_PUBLIC_METHOD

        #region PUBLIC_METHOD

        /// <summary>
        /// �L�����N�^�[�̈ʒu��ݒ肵�܂�
        /// </summary>
        /// <param name="gridIndex">�}�b�v�O���b�h�̃C���f�b�N�X</param>
        /// <param name="dir">�L�����N�^�[�p�x</param>
        public void SetPosition(int gridIndex, in Vector3 pos, in Quaternion dir)
        {
            tmpParam.gridIndex = gridIndex;
            // var info = Stage.StageController.Instance.GetGridInfo(gridIndex);
            transform.position = pos;
            transform.rotation = dir;
        }

        /// <summary>
        /// �w��C���f�b�N�X�̃O���b�h�ɃL�����N�^�[�̌��������킹��悤�ɖ��߂𔭍s���܂�
        /// </summary>
        /// <param name="targetPos">���������킹��ʒu</param>
        public void RotateToPosition( in Vector3 targetPos )
        {
            var selfPos     = _stageCtrl.GetGridCharaStandPos( tmpParam.gridIndex );
            var direction   = targetPos - selfPos;
            direction.y     = 0f;

            _orderdRotation     = Quaternion.LookRotation(direction);
            _isOrderedRotation  = true;
        }

        /// <summary>
        /// �s���I�����ȂǁA�s���s�̏�Ԃɂ��܂�
        /// �L�����N�^�[���f���̐F��ύX���A�s���s�ł��邱�Ƃ������������܂߂܂�
        /// </summary>
        public void BeImpossibleAction()
        {
            for (int i = 0; i < (int)Command.COMMAND_TAG.NUM; ++i)
            {
                tmpParam.isEndCommand[i] = true;
            }

            // �s���I�����������߂Ƀ}�e���A���̐F�����O���[�ɕύX
            for (int i = 0; i < _textureMaterialsAndColors.Count; ++i)
            {
                _textureMaterialsAndColors[i].material.color = Color.gray;
            }
        }

        /// <summary>
        /// �s���ĊJ���ɍs���\��Ԃɂ��܂�
        /// �L�����N�^�[�̃��f���̐F�����ɖ߂��������܂߂܂�
        /// </summary>
        public void BePossibleAction()
        {
            tmpParam.Reset();

            // �}�e���A���̐F����ʏ�̐F���ɖ߂�
            for (int i = 0; i < _textureMaterialsAndColors.Count; ++i)
            {
                _textureMaterialsAndColors[i].material.color = _textureMaterialsAndColors[i].originalColor;
            }
        }

        /// <summary>
        /// �ߐڍU���V�[�P���X���J�n���܂�
        /// </summary>
        public void StartClosedAttackSequence()
        {
            _isAttacked         = false;
            _closingAttackPhase = CLOSED_ATTACK_PHASE.CLOSINGE;
            _elapsedTime        = 0f;

            AnimCtrl.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.MOVE, true);
        }

        /// <summary>
        /// ���u�U���V�[�P���X���J�n���܂�
        /// </summary>
        public void StartRangedAttackSequence()
        {
            _isAttacked         = false;
            _atkRemainingNum    = skillModifiedParam.AtkNum - 1;   // �U���񐔂�1����
            var attackAnimtag   = AttackAnimTags[_atkRemainingNum];

            AnimCtrl.SetAnimator(attackAnimtag);
        }

        /// <summary>
        /// �p���B�V�[�P���X���J�n���܂�
        /// </summary>
        public void StartParrySequence()
        {
            _parryPhase = PARRY_PHASE.EXEC_PARRY;
            _elapsedTime = 0f;

            AnimCtrl.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.PARRY);
            // �^�C���X�P�[����x�����A�p���B�������X���[���[�V�����Ō�����
            _timeScale.SetTimeScale(0.1f);
        }

        /// <summary>
        /// �p���B���菈�����J�n���܂�
        /// MEMO : ���[�V�����̃C�x���g�t���O����Ăяo���܂�
        /// </summary>
        public void StartParryJudgeEvent()
        {
            if (!_opponent.IsSkillInUse(SkillsData.ID.SKILL_PARRY)) return;
            
            SkillParryController parryCtrl = _btlMgr.SkillCtrl.ParryController;
            _opponent.SubscribeParryEvent(parryCtrl);
            parryCtrl.StartParryEvent(_opponent, this);
        }

        /// <summary>
        /// ���s�\�ȃR�}���h���X�V���܂�
        /// </summary>
        public void UpdateExecutableCommand(in StageController stageCtrl)
        {
            _executableCommands.Clear();

            for( int i = 0; i < (int)Command.COMMAND_TAG.NUM; ++i )
            {
                if (!_executableCommandTables[i](this, stageCtrl)) continue;

                _executableCommands.Add( (Command.COMMAND_TAG)i );
            }
        }

        /// <summary>
        /// �ߐڍU�����̗�����X�V���܂�
        /// </summary>
        /// <param name="departure">�ߐڍU���̊J�n�n�_</param>
        /// <param name="destination">�ߐڍU���̏I���n�_</param>
        /// <returns>�I������</returns>
        public bool UpdateClosedAttack(in Vector3 departure, in Vector3 destination)
        {
            var attackAnimtag = AttackAnimTags[skillModifiedParam.AtkNum - 1];

            if (GetBullet() != null) return false;

            float t = 0f;
            bool isReservedParry = _opponent.IsSkillInUse(SkillsData.ID.SKILL_PARRY);

            switch (_closingAttackPhase)
            {
                case CLOSED_ATTACK_PHASE.CLOSINGE:
                    _elapsedTime += Time.deltaTime;
                    t = Mathf.Clamp01(_elapsedTime / Constants.ATTACK_CLOSING_TIME);
                    t = Mathf.SmoothStep(0f, 1f, t);
                    gameObject.transform.position = Vector3.Lerp(departure, destination, t);
                    if (1.0f <= t)
                    {
                        _elapsedTime = 0f;
                        AnimCtrl.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.MOVE, false);
                        AnimCtrl.SetAnimator(attackAnimtag);

                        _closingAttackPhase = CLOSED_ATTACK_PHASE.ATTACK;
                    }
                    break;

                case CLOSED_ATTACK_PHASE.ATTACK:
                    if (IsEndAttackAnimSequence())
                    {
                        AnimCtrl.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.WAIT);

                        _closingAttackPhase = CLOSED_ATTACK_PHASE.DISTANCING;
                    }
                    break;
                case CLOSED_ATTACK_PHASE.DISTANCING:
                    // �U���O�̏ꏊ�ɖ߂�
                    _elapsedTime += Time.deltaTime;
                    t = Mathf.Clamp01(_elapsedTime / Constants.ATTACK_DISTANCING_TIME);
                    t = Mathf.SmoothStep(0f, 1f, t);
                    gameObject.transform.position = Vector3.Lerp(destination, departure, t);
                    if (1.0f <= t)
                    {
                        _elapsedTime = 0f;
                        _closingAttackPhase = CLOSED_ATTACK_PHASE.NONE;

                        return true;
                    }
                    break;
                default: break;
            }

            return false;
        }

        /// <summary>
        /// ���u�U�����̗�����X�V���܂�
        /// </summary>
        /// <param name="departure">���u�U���̊J�n�n�_</param>
        /// <param name="destination">���u�U���̏I���n�_</param>
        /// <returns>�I������</returns>
        public bool UpdateRangedAttack(in Vector3 departure, in Vector3 destination)
        {
            if (GetBullet() == null) return false;

            // ���u�U���͓���̃t���[���ŃJ�����Ώۂƃp�����[�^��ύX����
            if (IsTransitNextPhaseCamera())
            {
                _btlMgr.GetCameraController().TransitNextPhaseCameraParam(null, GetBullet().transform);
            }
            // �U���I�������ꍇ��Wait�ɐ؂�ւ�
            if (IsEndAttackAnimSequence())
            {
                AnimCtrl.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.WAIT);
            }

            // �ΐ푊�肪�U�����e�ς݁A���AWait��Ԃɐ؂�ւ��ς݂̏ꍇ�ɏI��
            return _isAttacked && AnimCtrl.IsPlayingAnimationOnConditionTag(AnimDatas.ANIME_CONDITIONS_TAG.WAIT);
        }

        /// <summary>
        /// �퓬�ɂ����āA�U�����������p���B���󂯂��ۂ̍s�����X�V���܂�
        /// MEMO : �p���B���󂯂����͊�{�I�ɍs�����Ȃ�����false��Ԃ��̂�
        /// </summary>
        /// <param name="departure">�U���J�n���W</param>
        /// <param name="destination">�U���ڕW���W</param>
        /// <returns>�I������</returns>
        public bool UpdateParryOnAttacker(in Vector3 departure, in Vector3 destination)
        {
            return false;
        }

        /// <summary>
        /// �퓬�ɂ����āA�U�����ꂽ�����p���B���s�����ۂ̍s�����X�V���܂�
        /// </summary>
        /// <param name="departure">�U���J�n���W</param>
        /// <param name="destination">�U���ڕW���W</param>
        /// <returns>�I������</returns>
        public bool UpdateParryOnTargeter(in Vector3 departure, in Vector3 destination)
        {
            bool isJustParry = false;

            switch( _parryPhase )
            {
                case PARRY_PHASE.EXEC_PARRY:
                    if (isJustParry)
                    {
                        AnimCtrl.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.SINGLE_ATTACK);

                        _parryPhase = PARRY_PHASE.AFTER_ATTACK;
                    }
                    else {
                        if (AnimCtrl.IsEndAnimationOnConditionTag(AnimDatas.ANIME_CONDITIONS_TAG.PARRY))
                        {
                            AnimCtrl.SetAnimator(AnimDatas.ANIME_CONDITIONS_TAG.WAIT);

                            return true;
                        }
                    }
                    break;
                case PARRY_PHASE.AFTER_ATTACK:
                    break;
            }

            return false;
        }

        /// <summary>
        /// �p���B�����̓s����ŃA�j���[�V�������~�����܂� �����[�V��������Ăяo����܂�
        /// </summary>
        public void StopAnimationOnParry()
        {
            if(!_btlMgr.SkillCtrl.ParryController.IsJudgeEnd())
            {
                _timeScale.Stop();
            }
        }

        /// <summary>
        /// �ΐ푊���ݒ肵�܂�
        /// </summary>
        /// <param name="opponent">�ΐ푊��</param>
        public void SetOpponentCharacter(Character opponent)
        {
            _opponent = opponent;
        }

        /// <summary>
        /// �U�����󂯂�ۂ̐ݒ���s���܂�
        /// </summary>
        public void SetReceiveAttackSetting()
        {
            ParryResult = SkillParryController.JudgeResult.NONE;
        }

        /// <summary>
        /// �ΐ푊��̐ݒ�����Z�b�g���܂�
        /// </summary>
        public void ResetOnEndOfAttackSequence()
        {
            // �ΐ푊��������Z�b�g
            _opponent = null;
            // �g�p�X�L���������Z�b�g
            tmpParam.ResetUseSkill();
        }

        /// <summary>
        /// �Q�[���I�u�W�F�N�g���폜���܂�
        /// </summary>
        public void Remove()
        {
            Destroy(gameObject);
            Destroy(this);
        }

        /// <summary>
        /// �A�N�V�����Q�[�W������܂�
        /// </summary>
        public void ConsumeActionGauge()
        {
            param.curActionGauge -= param.consumptionActionGauge;
            param.consumptionActionGauge = 0;

            for (int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i)
            {
                BattleUISystem.Instance.GetPlayerParamSkillBox(i).StopFlick();
            }
        }

        /// <summary>
        /// �A�N�V�����Q�[�W��recoveryActionGauge�̕������񕜂��܂�
        /// ��{�I�Ɏ��^�[���J�n���ɌĂт܂�
        /// </summary>
        public void RecoveryActionGauge()
        {
            param.curActionGauge = Mathf.Clamp(param.curActionGauge + param.recoveryActionGauge, 0, param.maxActionGauge);
        }

        /// <summary>
        /// ���s�\�ȃR�}���h�𒊏o���܂�
        /// </summary>
        /// <param name="executableCommands">���o��̈�����</param>
        public void FetchExecutableCommand(out List<Command.COMMAND_TAG> executableCommands, in StageController stageCtrl)
        {
            UpdateExecutableCommand(stageCtrl);

            executableCommands = _executableCommands;
        }

        public bool IsPlayer() { return param.characterTag == CHARACTER_TAG.PLAYER; }

        public bool IsEnemy() { return param.characterTag == CHARACTER_TAG.ENEMY; }

        public bool IsOther() { return param.characterTag == CHARACTER_TAG.OTHER; }

        /// <summary>
        /// �U���A�j���[�V�����̏I�������Ԃ��܂�
        /// </summary>
        /// <returns>�U���A�j���[�V�������I�����Ă��邩</returns>
        public bool IsEndAttackAnimSequence()
        {
            return AnimCtrl.IsEndAnimationOnStateName(AnimDatas.AtkEndStateName) ||  // �Ō�̍U����State���͕K��AtkEndStateName�ň�v������
                (_opponent.IsDeclaredDead && AnimCtrl.IsEndCurrentAnimation());                  // ������U�����ł��A�r���ő��肪���S���邱�Ƃ��m�񂳂��ꍇ�͍U�����I������
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsTransitNextPhaseCamera()
        {
            if(_isTransitNextPhaseCamera)
            {
                // true�̏ꍇ�͎���Ȍ�̔���̂��߂�false�ɖ߂�
                _isTransitNextPhaseCamera = false;
                return true;
            }

            return false;
        }

        /// <summary>
        /// ���S�����Ԃ��܂�
        /// </summary>
        /// <returns>���S���Ă��邩�ۂ�</returns>
        public bool IsDead()
        {
            return param.CurHP <= 0;
        }

        /// <summary>
        /// �w��̃X�L�����g�p�o�^����Ă��邩�𔻒肵�܂�
        /// </summary>
        /// <param name="skillID">�w��X�L��ID</param>
        /// <returns>�g�p�o�^����Ă��邩�ۂ�</returns>
        public bool IsSkillInUse(SkillsData.ID skillID)
        {
            for (int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i)
            {
                if (!tmpParam.isUseSkills[i]) continue;

                if (param.equipSkills[i] == skillID) return true;
            }

            return false;
        }

        /// <summary>
        /// �ݒ肳��Ă���e���擾���܂�
        /// </summary>
        /// <returns>Prefab�ɐݒ肳��Ă���e</returns>
        public Bullet GetBullet() { return _bullet; }

        #endregion // PUBLIC_METHOD
    }
}