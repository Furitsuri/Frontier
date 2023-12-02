using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace Frontier
{
    public class SkillParryController : MonoBehaviour
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

        [Header("UI�X�N���v�g")]
        [SerializeField]
        private SkillParryUI _ui;

        [Header("�p���B���胊���O�k������")]
        [SerializeField]
        private float _shrinkTime = 3f;

        [Header("�L�����N�^�[�̃X���[���[�V�������[�g")]
        [SerializeField]
        private float _delayTimeScale = 0.1f;

        private ParryRingEffect _effect;
        private BattleManager _btlMgr = null;
        private Character _useParryCharacter = null;
        private Character _attackCharacter = null;
        private JudgeResult _judgeResult = JudgeResult.FAILED;
        private (float inner, float outer) _judgeRingSuccessRange;
        private (float inner, float outer) _judgeRingJustRange;
        private float _showUITime = 1.5f;
        // �p���B�C�x���g�I�����̃f���Q�[�g
        public event EventHandler<EventArgs> ProcessCompleted;
        // ���ʂ̎擾
        public JudgeResult Result => _judgeResult;

        public bool IsEndParryEvent => (_judgeResult != JudgeResult.NONE);

        // Update is called once per frame
        void Update()
        {
            // �L�[�������ꂽ�^�C�~���O�Ŕ���
            if (Input.GetKeyUp(KeyCode.Space))
            {
                float shrinkRadius = _effect.GetCurShrinkRingRadius();
                Debug.Log( shrinkRadius );

                // �����UI�ւ̕\��
                _ui.gameObject.SetActive(true);
                _judgeResult = JudgeResult.FAILED;
                if(_judgeRingJustRange.inner <= shrinkRadius && shrinkRadius <= _judgeRingJustRange.outer)
                {
                    _judgeResult = JudgeResult.JUST;
                }
                else if( _judgeRingSuccessRange.inner <= shrinkRadius && shrinkRadius <= _judgeRingSuccessRange.outer )
                {
                    _judgeResult = JudgeResult.SUCCESS;
                }
                _ui.ShowResult(_judgeResult);

                // �G�t�F�N�g��~
                _effect.StopShrink();
                // UI�ȊO�̕\�����̍X�V���ԃX�P�[�����~
                DelayBattleTimeScale(0f);
                // �p���B���ʂɂ��p�����[�^�ϓ����e�L�����N�^�[�ɓK��
                ApplyModifiedParamFromResult(_useParryCharacter, _attackCharacter, _judgeResult);
                // ���ʂƋ��ɃC�x���g�I�����Ăяo�����ɒʒm
                OnProcessCompleted(EventArgs.Empty);
            }

            // UI�\���I��
            if (_ui.IsShowEnd())
            {
                enabled = false;
                OnDestroy();
            }
        }

        void FixedUpdate()
        {
            // �t���[�����[�g�ɂ��Y����h������Fixed�ōX�V
            _effect.FixedUpdateEffect();    
        }

        void OnDestroy()
        {
            ResetBattleTimeScale();
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
            _effect.SetJudgeRingRange(_judgeRingSuccessRange);
        }

        /// <summary>
        /// �p���B����I�����ɌĂяo���C�x���g�n���h��
        /// </summary>
        /// <param name="e">�C�x���g�I�u�W�F�N�g</param>
        void OnProcessCompleted(EventArgs e )
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
                    attackChara.skillModifiedParam.AtkMagnification *= SkillsData.data[(int)SkillsData.ID.SKILL_PARRY].Param1;
                    break;
                case SkillParryController.JudgeResult.FAILED:
                    useParryChara.skillModifiedParam.DefMagnification *= SkillsData.data[(int)SkillsData.ID.SKILL_PARRY].Param2;
                    break;
                case SkillParryController.JudgeResult.JUST:
                    useParryChara.skillModifiedParam.AtkMagnification *= SkillsData.data[(int)SkillsData.ID.SKILL_PARRY].Param3;
                    attackChara.skillModifiedParam.AtkMagnification *= SkillsData.data[(int)SkillsData.ID.SKILL_PARRY].Param1;
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
        /// <param name="self">�p���B�����L�����N�^�[</param>
        /// <param name="opponent">�ΐ푊��</param>
        public void Init(BattleManager btlMgr)
        {
            _btlMgr = btlMgr;

            _ui.Init(_showUITime);
            _ui.gameObject.SetActive(false);

            // ���s�����܂ł͖�����
            gameObject.SetActive(false);
        }

        /// <summary>
        /// �p���B���菈�����J�n���܂�
        /// </summary>
        /// <param name="self">�p���B���s���L�����N�^�[</param>
        /// <param name="opponent">�ΐ푊��</param>
        public void StartParryEvent(Character useCharacter, Character opponent)
        {
            _useParryCharacter = useCharacter;
            _attackCharacter = opponent;

            gameObject.SetActive(true);
            // �p���B�G�t�F�N�g�̃V�F�[�_�[�����J�����ɕ`�悷�邽�߁A���C���J�����ɃA�^�b�`
            Camera.main.gameObject.AddComponent<ParryRingEffect>();
            _effect = Camera.main.gameObject.GetComponent<ParryRingEffect>();
            Debug.Assert(_effect != null);
            // MEMO : _effect�̓A�^�b�`�̃^�C�~���O�̓s����Init�ł͂Ȃ������ŏ�����
            _effect.Init(_shrinkTime);
            _effect.SetEnable(true);

            // �h�䑤�̖h��͂ƍU�����̍U���͂���p���B����͈͂��Z�o���Đݒ�
            int selfDef = (int)Mathf.Floor( (_useParryCharacter.param.Def + _useParryCharacter.modifiedParam.Def) * _useParryCharacter.skillModifiedParam.DefMagnification );
            int oppoAtk = (int)Mathf.Floor( (_attackCharacter.param.Atk + _attackCharacter.modifiedParam.Atk) * _attackCharacter.skillModifiedParam.AtkMagnification );
            CalcurateParryRingParam(selfDef, oppoAtk);

            // �p���B���̃X���[���[�V�������x��ݒ�
            DelayBattleTimeScale(_delayTimeScale);
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
            ResetBattleTimeScale();

            _effect.Destroy();

            gameObject.SetActive(false);
        }
    }
}