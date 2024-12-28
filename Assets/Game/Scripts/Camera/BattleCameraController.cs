using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable 0414

namespace Frontier
{
    public class BattleCameraController : Singleton<BattleCameraController>
    {
        /// <summary>
        /// �J�����̃��[�h
        /// </summary>
        public enum CameraMode
        {
            FOLLOWING = 0,      // �I���O���b�h�ǐՏ��
            CHARACTER_MOVE,     // �L�����N�^�[�ړ����
            ATTACK_SEQUENCE,    // �퓬���

            NUM
        }

        /// <summary>
        /// �U���V�[�P���X�ɂ�����J���������t�F�C�Y
        /// </summary>
        enum AttackSequenceCameraPhase
        {
            START = 0,      // �퓬��ԂɈڍs�J�n�`�퓬�J�n�܂�
            BATTLE_FIELD,   // �퓬���`�퓬�I���܂�
            END,            // �퓬�I����`�X�e�[�W��ԂɑJ�ڂ܂�

            NUM
        }

        [System.Serializable]
        public struct CameraParamData
        {
            public string Phase;
            public float Length;
            public float Roll;
            public float Pitch;
            public float Yaw;
        }

        [SerializeField]
        private float _offsetLength;

        [SerializeField]
        private float _followDuration = 1f;

        [SerializeField]
        private float _fadeDuration = 0.4f;

        [SerializeField]
        private float _atkCameraLerpDuration = 0.2f;

        [SerializeField]
        private float _mosaicStartFadeRate = 0.0f;

        [SerializeField]
        private float _mosaicBlockSizeMaxRate = 0.5f;

        private CameraMode _mode;
        private AttackSequenceCameraPhase _atkCameraPhase;
        private Camera _mainCamera;
        private List<CameraParamData[]> _closeAtkCameraParamDatas;
        private List<CameraParamData[]> _rangedAtkCameraParamDatas;
        private CameraParamData[] _currentCameraParamDatas;
        private CameraMosaicEffect _mosaicEffectScript;
        // �J�������W�̊�_�ƂȂ�g�����X�t�H�[��
        private Transform _cameraBaseTransform;
        // �J�����̔�ʑ̍��W�̊�_�ƂȂ�g�����X�t�H�[��
        private Transform _lookAtTransform;
        // �J�������W�ɉ��Z����I�t�Z�b�g�l
        private Vector3 _cameraOffset;
        // �L�����N�^�[���ɐݒ肳�ꂽ�J�������W�ɉ��Z����I�t�Z�b�g�l
        private Vector3 _characterCameraOffset;
        // �O���(���O�t���[��)�ɂ�����J�������W
        private Vector3 _prevCameraPosition;
        // ��ʑ̍��W
        private Vector3 _lookAtPosition;
        // �O���(���O�t���[��)�ɂ������ʑ̍��W
        private Vector3 _prevLookAtPosition;
        // �J�����̈ړ��ڕW���W
        private Vector3 _followingPosition;
        // ��ʑ̍��W�ƃJ�������W�Ƃ̍��ƂȂ�I�t�Z�b�g
        private Vector3 _offset;
        // �J�����ړ��J�ڂɗp����t�F�C�Y�̃C���f�b�N�X�l
        private int _cameraPhaseIndex = 0;
        private float _followElapsedTime = 0.0f;
        private float _fadeElapsedTime = 0.0f;
        private float _length = 0.0f;
        private float _roll = 0.0f;
        private float _pitch = 0.0f;
        private float _yaw = 0.0f;

        // Start is called before the first frame update
        override protected void OnStart()
        {
            _mainCamera             = Camera.main;
            _mosaicEffectScript     = GetComponent<CameraMosaicEffect>();
            _cameraBaseTransform    = null;
            _lookAtTransform        = null;
            _mode                   = CameraMode.FOLLOWING;
            _atkCameraPhase         = AttackSequenceCameraPhase.START;
            _prevCameraPosition     = transform.position;
            _lookAtPosition         = _mainCamera.transform.position + _mainCamera.transform.forward;
            _followingPosition      = _mainCamera.transform.position;
            _offset                 = _followingPosition - _mainCamera.transform.forward;
            _offsetLength           = _offset.magnitude;
        }

        // Update is called once per frame
        override protected void OnUpdate()
        {
            switch (_mode)
            {
                case CameraMode.FOLLOWING:
                    // MEMO : position�����肵�Ă���LookAt��ݒ肵�Ȃ��ƁA��ʂɂ��������������邽�ߒ���
                    _followElapsedTime = Mathf.Clamp(_followElapsedTime + Time.deltaTime, 0f, _followDuration);
                    _mainCamera.transform.position = Vector3.Lerp(_prevCameraPosition, _followingPosition, _followElapsedTime / _followDuration);
                    break;

                case CameraMode.CHARACTER_MOVE:
                    UpdateCharacterMoveCamera();
                    break;

                case CameraMode.ATTACK_SEQUENCE:
                    UpdateAttackSequenceCamera();
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// ���j�b�g���ړ�����ۂ̃J�����X�V���s���܂�
        /// </summary>
        private void UpdateCharacterMoveCamera()
        {

        }

        /// <summary>
        /// ���j�b�g���m�̑ΐ�V�[�P���X�ɂ�����J�����X�V���s���܂�
        /// </summary>
        private void UpdateAttackSequenceCamera()
        {
            switch (_atkCameraPhase)
            {
                case AttackSequenceCameraPhase.START:
                    {
                        _fadeElapsedTime                = Mathf.Clamp(_fadeElapsedTime + Time.deltaTime, 0f, _fadeDuration);
                        var fadeRate                    = _fadeElapsedTime / _fadeDuration;
                        var destCameraPos               = _cameraBaseTransform.position + _cameraOffset;
                        _mainCamera.transform.position  = Vector3.Lerp(_followingPosition, destCameraPos, fadeRate);
                        _mainCamera.transform.LookAt(_lookAtPosition);

                        // �w�背�[�g���������ۂ̓��U�C�N�������{��
                        if (_mosaicStartFadeRate <= fadeRate)
                        {
                            _mosaicEffectScript.ToggleEnable(true);

                            // _mosaicStartFadeRate�̒l�Ɉˑ����Ȃ��`�Ń��[�g�ω�����悤�ɒ������Ă���
                            var blockSizeRate = 1.0f - Mathf.Clamp01(_mosaicBlockSizeMaxRate) * (fadeRate - _mosaicStartFadeRate) / (1f - _mosaicStartFadeRate);
                            _mosaicEffectScript.UpdateBlockSizeByRate(blockSizeRate);
                        }

                        if (_fadeDuration <= _fadeElapsedTime)
                        {
                            _mosaicEffectScript.ToggleEnable(false);
                            _mosaicEffectScript.ResetBlockSize();
                            // �p�����[�^��\��
                            BattleUISystem.Instance.TogglePlayerParameter(true);
                            BattleUISystem.Instance.ToggleEnemyParameter(true);
                            // �퓬�t�B�[���h�Ɉڍs
                            _atkCameraPhase = AttackSequenceCameraPhase.BATTLE_FIELD;
                        }
                    }
                    break;

                case AttackSequenceCameraPhase.BATTLE_FIELD:
                    {
                        if (_cameraBaseTransform == null || _lookAtTransform == null)
                        {
                            Debug.Assert(false);
                            return;
                        }

                        _fadeElapsedTime = Mathf.Clamp(_fadeElapsedTime + Time.deltaTime, 0f, _atkCameraLerpDuration);
                        var lerpRate = _fadeElapsedTime / _atkCameraLerpDuration;
                        var nextCameraPosition = _cameraBaseTransform.position + _cameraOffset;
                        _mainCamera.transform.position = Vector3.Lerp(_prevCameraPosition, nextCameraPosition, lerpRate);
                        _lookAtPosition = Vector3.Lerp(_prevLookAtPosition, _lookAtTransform.position, lerpRate);
                        _mainCamera.transform.LookAt(_lookAtPosition);
                    }
                    break;

                case AttackSequenceCameraPhase.END:
                    {
                        _fadeElapsedTime = Mathf.Clamp(_fadeElapsedTime + Time.deltaTime, 0f, _fadeDuration);
                        var fadeRate = _fadeElapsedTime / _fadeDuration;
                        _mainCamera.transform.position = Vector3.Lerp(_prevCameraPosition, _followingPosition, fadeRate);
                        _mainCamera.transform.LookAt(_lookAtPosition);
                        // START�̔��΂̏���
                        if (fadeRate < 1f - _mosaicStartFadeRate)
                        {
                            _mosaicEffectScript.ToggleEnable(true);

                            // _mosaicStartFadeRate�̒l�Ɉˑ����Ȃ��`�Ń��[�g�ω�����悤�ɒ������Ă���
                            var blockSizeRate = 1.0f - Mathf.Clamp01(_mosaicBlockSizeMaxRate) * (1f - (fadeRate / (1f - _mosaicStartFadeRate)));
                            _mosaicEffectScript.UpdateBlockSizeByRate(blockSizeRate);
                        }

                        if (_fadeDuration <= _fadeElapsedTime)
                        {
                            _mosaicEffectScript.ToggleEnable(false);
                            _mosaicEffectScript.ResetBlockSize();
                            BattleUISystem.Instance.TogglePlayerParameter(true);
                            BattleUISystem.Instance.ToggleEnemyParameter(true);

                            _mainCamera.transform.position = _followingPosition;
                            _mainCamera.transform.LookAt(_lookAtPosition);

                            _mode = CameraMode.FOLLOWING;
                        }
                    }
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// �I���J�[�\���ɏ]���J��������ݒ肵�܂�
        /// </summary>
        /// <param name="pos">�J�����Ώۍ��W</param>
        public void SetLookAtBasedOnSelectCursor(in Vector3 pos)
        {
            if (_mode == CameraMode.ATTACK_SEQUENCE) return;

            _prevCameraPosition = transform.position;
            _lookAtPosition = pos;
            _followingPosition = _lookAtPosition + _offset;
            _followElapsedTime = 0.0f;
        }

        /// <summary>
        /// �o�g�����̃J�����f�[�^��ݒ肵�܂�
        /// </summary>
        /// <param name="closeDatas">�ߋ����U���J�����f�[�^</param>
        /// <param name="rangedDatas">�������U���J�����f�[�^</param>
        public void SetCameraParamDatas(in List<CameraParamData[]> closeDatas, in List<CameraParamData[]> rangedDatas)
        {
            _closeAtkCameraParamDatas = closeDatas;
            _rangedAtkCameraParamDatas = rangedDatas;
        }

        /// <summary>
        /// �U���J�ڊJ�n���̃J�����ݒ���s���܂�
        /// </summary>
        /// <param name="attacker">�U������L�����N�^�[</param>
        /// <param name="target">�U���ΏۃL�����N�^�[</param>
        public void StartAttackSequenceMode( Character attacker, Character target )
        {
            if (attacker == null || target == null) return;

            // �ʏ�͍U���L�����N�^�[�����_�ɂ����J�������[�N�Ƃ��邪�A
            // �p���B���g�p�����ꍇ�͕K����U���L�����N�^�[�����_�Ƃ���
            var cameraFromChara = attacker;
            var cameraToChara   = target;
            if (target.IsSkillInUse(SkillsData.ID.SKILL_PARRY))
            {
                cameraFromChara = target;
                cameraToChara   = attacker;
            }

            // �U���L�����N�^�[���ߐڃ^�C�v�����u�^�C�v���ɂ���ĎQ�Ƃ���J�����f�[�^��ύX����
            List<CameraParamData[]> cameraParamDatas;
            if (attacker.GetBullet() == null) cameraParamDatas = _closeAtkCameraParamDatas;
            else cameraParamDatas = _rangedAtkCameraParamDatas;

            _mode                   = CameraMode.ATTACK_SEQUENCE;
            _atkCameraPhase         = AttackSequenceCameraPhase.START;
            _prevCameraPosition     = transform.position;
            _cameraBaseTransform    = cameraFromChara.transform;
            _lookAtTransform        = cameraToChara.transform;
            _characterCameraOffset  = cameraFromChara.camParam.OffsetOnAtkSequence;

            // �����_���Ȓl��p���āA�J�����ړ��̃p�^�[���f�[�^����g�p����f�[�^���擾����
            int cameraIndex             = new System.Random().Next(0, cameraParamDatas.Count);
            _currentCameraParamDatas    = cameraParamDatas[cameraIndex];
            _cameraPhaseIndex           = 0;
            _length                     = _currentCameraParamDatas[_cameraPhaseIndex].Length;
            _roll                       = _currentCameraParamDatas[_cameraPhaseIndex].Roll;
            _pitch                      = _currentCameraParamDatas[_cameraPhaseIndex].Pitch;
            _yaw                        = _currentCameraParamDatas[_cameraPhaseIndex].Yaw;

            // �J������_�L�����Ɣ�ʑ̃L�����Ԃ̒��S�_�Ɍ������ăJ�������߂Â���
            _lookAtPosition     = ( cameraFromChara.transform.position + cameraToChara.transform.position ) * 0.5f;
            _fadeElapsedTime    = 0f;

            // �J�����̊�_�ƂȂ���W�ɉ��Z����I�t�Z�b�g���W���p�����[�^���Q�Ƃ��Čv�Z����
            _cameraOffset = Methods.RotateVector(_cameraBaseTransform, _roll, _pitch, _yaw, _cameraBaseTransform.forward) * _length + _characterCameraOffset;

            // ��x�p�����[�^���\��
            BattleUISystem.Instance.TogglePlayerParameter(false);
            BattleUISystem.Instance.ToggleEnemyParameter(false);
        }

        /// <summary>
        /// �퓬�t�B�[���h�̐ݒ�ɃJ�����̈ʒu�Ǝ��_��K�������܂�
        /// </summary>
        public void AdaptBattleFieldSetting()
        {
            _cameraOffset           = Methods.RotateVector(_cameraBaseTransform, _roll, _pitch, _yaw, _cameraBaseTransform.forward) * _length + _characterCameraOffset;
            _prevCameraPosition     = _mainCamera.transform.position = _cameraBaseTransform.position + _cameraOffset;
            _prevLookAtPosition     = _lookAtTransform.position;
            _mainCamera.transform.LookAt(_prevLookAtPosition);
        }

        /// <summary>
        /// �U���J�ڏI�����̃J�����ݒ���s���܂�
        /// </summary>
        /// <param name="attacker">�U������L�����N�^�[</param>
        public void EndAttackSequenceMode(Character attacker)
        {
            _atkCameraPhase     = AttackSequenceCameraPhase.END;
            _prevCameraPosition = _mainCamera.transform.position;
            _lookAtPosition     = attacker.transform.position;
            _followingPosition  = _lookAtPosition + _offset;
            _fadeElapsedTime    = 0f;
        }

        /// <summary>
        /// ���̃J�����p�����[�^�C���f�b�N�X���ɑJ�ڂ��܂�
        /// </summary>
        /// <param name="nextBase">�J�ڐ�̃J�����ʒu�Ώ�</param>
        /// <param name="nextLookAt">�J�ڐ�̃J���������Ώ�</param>
        public void TransitNextPhaseCameraParam(Transform nextBase = null, Transform nextLookAt = null)
        {
            _cameraPhaseIndex   = Mathf.Clamp(++_cameraPhaseIndex, 0, _currentCameraParamDatas.Length - 1);
            _length             = _currentCameraParamDatas[_cameraPhaseIndex].Length;
            _roll               = _currentCameraParamDatas[_cameraPhaseIndex].Roll;
            _pitch              = _currentCameraParamDatas[_cameraPhaseIndex].Pitch;
            _yaw                = _currentCameraParamDatas[_cameraPhaseIndex].Yaw;
            _fadeElapsedTime    = 0f;
            _cameraOffset       = Methods.RotateVector(_cameraBaseTransform, _roll, _pitch, _yaw, _cameraBaseTransform.forward) * _length + _characterCameraOffset;
            _prevCameraPosition = _mainCamera.transform.position;
            _prevLookAtPosition = _lookAtPosition;

            if (nextBase != null) _cameraBaseTransform = nextBase;
            if (nextLookAt != null) _lookAtTransform = nextLookAt;
        }

        /// <summary>
        /// �U���V�[�P���X�ɑJ�ڂ�������Ԃ��܂�
        /// </summary>
        /// <returns>�J�ڂ������ۂ�</returns>
        public bool IsFadeAttack()
        {
            return _mode == CameraMode.ATTACK_SEQUENCE && _atkCameraPhase == AttackSequenceCameraPhase.BATTLE_FIELD;
        }

        /// <summary>
        /// �t�F�[�h���I���������ۂ���Ԃ��܂�
        /// </summary>
        /// <returns>�t�F�[�h�I��������</returns>
        public bool IsFadeEnd()
        {
            return _mode == CameraMode.FOLLOWING;
        }
    }
}