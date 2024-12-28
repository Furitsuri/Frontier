using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

namespace Frontier
{
    /// <summary>
    /// �p���B�X�L���̏������s���܂�
    /// </summary>
    public class SkillParryController : Controller
    {
        /// <summary>
        /// �p���B����̎��
        /// </summary>
        public enum JudgeResult
        {
            NONE = -1,
            SUCCESS,    // ����
            FAILED,     // ���s
            JUST,       // �W���X�g����

            MAX,
        }

        [SerializeField]
        [Header("UI�X�N���v�g")]
        private SkillParryUI _ui;

        [SerializeField]
        [Header("�������G�t�F�N�g")]
        private ParticleSystem _successParticle = null;

        [SerializeField]
        [Header("���s���G�t�F�N�g")]
        private ParticleSystem _failureParticle = null;

        [SerializeField]
        [Header("�W���X�g�������G�t�F�N�g")]
        private ParticleSystem _justParticle = null;

        [SerializeField]
        [Header("�p���B���胊���O�k������")]
        private float _shrinkTime = 3f;

        [SerializeField]
        [Header("�L�����N�^�[�̃X���[���[�V�������[�g")]
        private float _delayTimeScale = 0.1f;

        [SerializeField]
        [Header("���s����Ɏ����J�ڂ���T�C�Y�{��")]
        private float _radiusRateAutoTransitionToFail = 0.75f;

        [SerializeField]
        [Header("���ʂ�\������b��")]
        private float _showUITime = 1.5f;

        private float _radiusThresholdOnFail    = 0f;
        private ParryRingEffect _ringEffect     = null;
        private ParryResultEffect _resultEffect = null;
        private BattleManager _btlMgr           = null;
        private Character _useParryCharacter    = null;
        private Character _attackCharacter      = null;
        private JudgeResult _judgeResult        = JudgeResult.NONE;
        private (float inner, float outer) _judgeRingSuccessRange   = (0f, 0f);
        private (float inner, float outer) _judgeRingJustRange      = (0f, 0f);

        // �p���B�C�x���g�I�����̃f���Q�[�g
        public event EventHandler<SkillParryCtrlEventArgs> ProcessCompleted;

        // Update is called once per frame
        void Update()
        {
            // �G�t�F�N�g�I���Ɠ����ɖ����ɐؑ�
            if ( _resultEffect.IsEndPlaying() )
            {
                _ui.terminate();
                _resultEffect.terminate();

                SkillParryCtrlEventArgs args = new SkillParryCtrlEventArgs();
                args.Result = _judgeResult;

                // ���ʂƋ��ɃC�x���g�I�����Ăяo�����ɒʒm
                OnProcessCompleted(args);

                // MonoBehavior�𖳌���
                gameObject.SetActive(false);
            }

            // ���ʂ����ɏo�Ă���ꍇ�͂����ŏI��
            if ( IsJudgeEnd() ) return;

            float shrinkRadius = _ringEffect.GetCurShrinkRingRadius();

            // �L�[�������ꂽ�^�C�~���O�Ŕ���
            if (Input.GetKeyUp(KeyCode.Space))
            {
                // ����
                _judgeResult = JudgeResult.FAILED;
                if (_judgeRingJustRange.inner <= shrinkRadius && shrinkRadius <= _judgeRingJustRange.outer)
                {
                    _judgeResult = JudgeResult.JUST;
                }
                else if (_judgeRingSuccessRange.inner <= shrinkRadius && shrinkRadius <= _judgeRingSuccessRange.outer)
                {
                    _judgeResult = JudgeResult.SUCCESS;
                }
            }
            else if(shrinkRadius < _radiusThresholdOnFail)
            {
                _judgeResult    = JudgeResult.FAILED;
            }

            if( IsJudgeEnd() )
            {
                _ui.gameObject.SetActive(true);
                _ui.ShowResult(_judgeResult);
                _resultEffect.PlayEffect(_judgeResult);

                // �k���G�t�F�N�g��~
                _ringEffect.StopShrink();

                // UI�ȊO�̕\�����̍X�V���ԃX�P�[�����~
                DelayBattleTimeScale(0f);

                // �p���B���ʂɂ��p�����[�^�ϓ����e�L�����N�^�[�ɓK��
                ApplyModifiedParamFromResult(_useParryCharacter, _attackCharacter, _judgeResult);
            }
        }

        void FixedUpdate()
        {
            // �t���[�����[�g�ɂ��Y����h������Fixed�ōX�V
            _ringEffect.FixedUpdateEffect();
        }

        /// <summary>
        /// UI��V�F�[�_�ȊO�̎��ԃX�P�[�������ɖ߂��܂�
        /// </summary>
        void ResetBattleTimeScale()
        {
            DelayBattleTimeScale(1f);
        }

        /// <summary>
        /// ���g�̖h��l�Ƒ���̍U���l�Ńp���B����̃����O�����W�����߂܂�
        /// </summary>
        /// <param name="selfCharaDef">�p���B�����L�����N�^�[�̖h��l</param>
        /// <param name="opponentCharaAtk">�ΐ푊��̍U���l</param>
        void CalcurateParryRingParam(int selfCharaDef, int opponentCharaAtk)
        {
            // TODO : ���������̌v�Z���Ń����O�͈͂��v�Z���Đݒ肷��B
            //        �k�����x�͕ύX���邩�͒�������̂��߁A��U�Œ�l
            _judgeRingSuccessRange = (0.4f, 0.6f);

            // �V�F�[�_�[�ɓK��
            _ringEffect.SetJudgeRingRange(_judgeRingSuccessRange);

            // ���s����Ɏ����J�ڂ��锼�a��臒l������
            // MEMO : �����͈͂̒����l�Ǝw��{���Ƃ̐ςƂ���
            _radiusThresholdOnFail = ((_judgeRingSuccessRange.inner + _judgeRingSuccessRange.outer) * 0.5f) * _radiusRateAutoTransitionToFail;
        }

        /// <summary>
        /// �p���B����I�����ɌĂяo���C�x���g�n���h��
        /// </summary>
        /// <param name="e">�C�x���g�I�u�W�F�N�g</param>
        void OnProcessCompleted( SkillParryCtrlEventArgs e )
        {
            ProcessCompleted ?.Invoke( this, e );
        }

        /// <summary>
        /// �p���B���ʂ���U���Ɩh��̌W�����e�L�����N�^�[�ɓK�������܂�
        /// </summary>
        /// <param name="useParryChara">�p���B�g�p�L�����N�^�[</param>
        /// <param name="attackChara">�U���L�����N�^�[</param>
        /// <param name="result">�p���B����</param>
        void ApplyModifiedParamFromResult(Character useParryChara, Character attackChara, JudgeResult result)
        {
            switch (result)
            {
                case SkillParryController.JudgeResult.SUCCESS:
                    attackChara.skillModifiedParam.AtkMagnification     *= SkillsData.data[(int)SkillsData.ID.SKILL_PARRY].Param1;
                    break;
                case SkillParryController.JudgeResult.FAILED:
                    useParryChara.skillModifiedParam.DefMagnification   *= SkillsData.data[(int)SkillsData.ID.SKILL_PARRY].Param2;
                    break;
                case SkillParryController.JudgeResult.JUST:
                    attackChara.skillModifiedParam.AtkMagnification     *= SkillsData.data[(int)SkillsData.ID.SKILL_PARRY].Param1;
                    useParryChara.skillModifiedParam.AtkMagnification   *= SkillsData.data[(int)SkillsData.ID.SKILL_PARRY].Param3;
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }

            _btlMgr.ApplyDamageExpect(attackChara, useParryChara);
        }

        /// <summary>
        /// ���������܂�
        /// </summary>
        /// <param name="btlMgr">�o�g���}�l�[�W��</param>
        public void Init(BattleManager btlMgr)
        {
            _btlMgr = btlMgr;

            _ui.Init(_showUITime);
            _ui.gameObject.SetActive(false);

            _resultEffect = new ParryResultEffect();
            ParticleSystem[] particles = new ParticleSystem[]
            {
                _successParticle,
                _failureParticle,
                _justParticle
            };
            _resultEffect.Init(particles);

            // ���s�����܂ł͖�����
            gameObject.SetActive(false);
        }

        /// <summary>
        /// �p���B���菈�����J�n���܂�
        /// </summary>
        /// <param name="useCharacter">�p���B���s���L�����N�^�[</param>
        /// <param name="opponent">�ΐ푊��</param>
        public void StartParryEvent(Character useCharacter, Character opponent)
        {
            gameObject.SetActive(true);
            _useParryCharacter  = useCharacter;
            _attackCharacter    = opponent;

            // �p���B�G�t�F�N�g�̃V�F�[�_�[�����J�����ɕ`�悷�邽�߁A���C���J�����ɃA�^�b�`
            Camera.main.gameObject.AddComponent<ParryRingEffect>();
            _ringEffect = Camera.main.gameObject.GetComponent<ParryRingEffect>();
            Debug.Assert(_ringEffect != null);

            // MEMO : _ringEffect, �y��_ui�̓A�^�b�`�̃^�C�~���O�̓s����Init�ł͂Ȃ������ŏ�����
            _ringEffect.Init(_shrinkTime);
            _ringEffect.SetEnable(true);
            _ui.Init(_showUITime);
            _ui.gameObject.SetActive(false);

            // �h�䑤�̖h��͂ƍU�����̍U���͂���p���B����͈͂��Z�o���Đݒ�
            int selfDef = (int)Mathf.Floor( (_useParryCharacter.param.Def + _useParryCharacter.modifiedParam.Def) * _useParryCharacter.skillModifiedParam.DefMagnification );
            int oppoAtk = (int)Mathf.Floor( (_attackCharacter.param.Atk + _attackCharacter.modifiedParam.Atk) * _attackCharacter.skillModifiedParam.AtkMagnification );
            CalcurateParryRingParam(selfDef, oppoAtk);

            // �p���B���̃L�����N�^�[�X���[���[�V�������x��ݒ�
            DelayBattleTimeScale(_delayTimeScale);

            // �p���B���[�V�����̊J�n
            _useParryCharacter.StartParrySequence();

            // ���ʂ�NONE�ɏ�����
            _judgeResult = JudgeResult.NONE;
        }

        /// <summary>
        /// UI��V�F�[�_�ȊO�̎��ԃX�P�[�����w��l�ɕύX���܂�
        /// </summary>
        /// <param name="timeScale">�x�点��X�P�[���l</param>
        public void DelayBattleTimeScale(float timeScale)
        {
            if (1f < timeScale) timeScale = 1f;

            // �^�C���X�P�[����ύX����ƁAUI�Ȃǂ̃A�j���[�V�������x�ɂ��e����^���Ă��܂����ߕۗ�(�V�F�[�_�[�G�t�F�N�g�͕�)
            _btlMgr.TimeScaleCtrl.SetTimeScale(timeScale);
        }

        /// <summary>
        /// �p���B���菈�����I�����܂�
        /// </summary>
        public void EndParryEvent()
        {
            // �^�C���X�P�[�������ɖ߂�
            ResetBattleTimeScale();

            _ringEffect.Destroy();
        }

        /// <summary>
        /// ���肪�I����������Ԃ��܂�
        /// </summary>
        /// <returns>���肪�I��������</returns>
        public bool IsJudgeEnd()
        {
            return _judgeResult != JudgeResult.NONE;
        }
    }

    /// <summary>
    /// Skill`ParryController�̌��ʒʒm�Ɏg�p���܂�
    /// </summary>
    public class SkillParryCtrlEventArgs : EventArgs
    {
        public SkillParryController.JudgeResult Result { get; set; }
    }
}