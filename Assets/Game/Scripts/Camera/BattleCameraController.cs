using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class BattleCameraController : Singleton<BattleCameraController>
{
    public enum CameraMode
    {
        FOLLOWING = 0,
        CHARACTER_MOVE,
        ATTACK_SEQUENCE,

        NUM
    }

    enum AttackSequenceCameraPhase
    {
        START = 0,
        FADE_ATTACK,
        END,

        NUM
    }

    enum AttackType
    {
        ALLY_CLOSE_ATTACK,
        ALLY_RANGED_ATTACK,
        OPPONENT_CLOSE_ATTACK,
        OPPONENT_RANGED_ATTACK,

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
    private CameraMosaicEffect _mosaicEffect;
    private Transform _baseTransform;
    private Transform _lookAtTransform;
    private Vector3 _baseDir;
    private Vector3 _prevCameraPosition;
    private Vector3 _lookAtPosition;
    private Vector3 _prevLookAtPosition;
    private Vector3 _followingPosition;
    private Vector3 _offset;
    private int _cameraPhaseIndex       = 0;
    private float _followElapsedTime    = 0.0f;
    private float _fadeElapsedTime      = 0.0f;
    private float _length               = 0.0f;
    private float _roll                 = 0.0f;
    private float _pitch                = 0.0f;
    private float _yaw                  = 0.0f;

    // Start is called before the first frame update
    override protected void OnStart()
    {
        _mainCamera                 = Camera.main;
        _mosaicEffect               = GetComponent<CameraMosaicEffect>();
        _baseTransform              = null;
        _lookAtTransform            = null;
        _mode                       = CameraMode.FOLLOWING;
        _atkCameraPhase             = AttackSequenceCameraPhase.START;
        _baseDir                    = Vector3.zero;
        _prevCameraPosition         = transform.position;
        _lookAtPosition             = _mainCamera.transform.position + _mainCamera.transform.forward;
        _followingPosition          = _mainCamera.transform.position;
        _offset                     = _followingPosition - _mainCamera.transform.forward;
        _offsetLength               = _offset.magnitude;

        // カメラパラメータファイルをロード
        FileReadManager.Instance.CameraParamLord();
    }

    // Update is called once per frame
    override protected void OnUpdate()
    {
        switch( _mode )
        {
            case CameraMode.FOLLOWING:
                // MEMO : positionを決定してからLookAtを設定しないと、Unityの仕様なのか、画面におかしなかくつきが発生するため注意
                _followElapsedTime = Mathf.Clamp( _followElapsedTime + Time.deltaTime, 0f, _followDuration);
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

    private void UpdateCharacterMoveCamera()
    {

    }

    /// <summary>
    /// 
    /// </summary>
    private void UpdateAttackSequenceCamera()
    {
        switch (_atkCameraPhase)
        {
            case AttackSequenceCameraPhase.START:
                {
                    _fadeElapsedTime = Mathf.Clamp(_fadeElapsedTime + Time.deltaTime, 0f, _fadeDuration);
                    var fadeRate = _fadeElapsedTime / _fadeDuration;

                    Vector3 offset = Quaternion.AngleAxis(_pitch, Vector3.right) * Quaternion.AngleAxis(_yaw, Vector3.up) * _baseDir * _length;
                    var nextCameraPos = _baseTransform.position + new Vector3(0f, 0.5f, 0f) + offset;
                    _mainCamera.transform.position = Vector3.Lerp(_followingPosition, nextCameraPos, fadeRate);
                    _mainCamera.transform.LookAt(_lookAtPosition);

                    // 指定レートを上回った際はモザイク処理を施す
                    if (_mosaicStartFadeRate <= fadeRate)
                    {
                        _mosaicEffect.ToggleEnable(true);

                        // _mosaicStartFadeRateの値に依存しない形でレート変化するように調整している
                        var blockSizeRate = 1.0f - Mathf.Clamp01(_mosaicBlockSizeMaxRate) * (fadeRate - _mosaicStartFadeRate) / (1f - _mosaicStartFadeRate);
                        _mosaicEffect.UpdateBlockSizeByRate(blockSizeRate);
                    }

                    if (_fadeDuration <= _fadeElapsedTime)
                    {
                        _mosaicEffect.ToggleEnable(false);
                        _mosaicEffect.ResetBlockSize();
                        BattleUISystem.Instance.TogglePlayerParameter(true);
                        BattleUISystem.Instance.ToggleEnemyParameter(true);

                        _atkCameraPhase = AttackSequenceCameraPhase.FADE_ATTACK;
                    }
                }
                break;

            case AttackSequenceCameraPhase.FADE_ATTACK:
                if (_baseTransform == null || _lookAtTransform == null)
                {
                    return;
                }

                UpdateCameraBasedOnAttackType();

                break;

            case AttackSequenceCameraPhase.END:
                {
                    _fadeElapsedTime = Mathf.Clamp(_fadeElapsedTime + Time.deltaTime, 0f, _fadeDuration);
                    var fadeRate = _fadeElapsedTime / _fadeDuration;
                    _mainCamera.transform.position = Vector3.Lerp(_prevCameraPosition, _followingPosition, fadeRate);
                    _mainCamera.transform.LookAt(_lookAtPosition);
                    // STARTの反対
                    if (fadeRate < 1f - _mosaicStartFadeRate)
                    {
                        _mosaicEffect.ToggleEnable(true);

                        // _mosaicStartFadeRateの値に依存しない形でレート変化するように調整している
                        var blockSizeRate = 1.0f - Mathf.Clamp01(_mosaicBlockSizeMaxRate) * (1f - ( fadeRate / (1f - _mosaicStartFadeRate)));
                        _mosaicEffect.UpdateBlockSizeByRate(blockSizeRate);
                    }

                    if (_fadeDuration <= _fadeElapsedTime)
                    {
                        _mosaicEffect.ToggleEnable(false);
                        _mosaicEffect.ResetBlockSize();
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
    /// 攻撃中のカメラ更新処理を行います
    /// </summary>
    private void UpdateCameraBasedOnAttackType()
    {
        _fadeElapsedTime            = Mathf.Clamp(_fadeElapsedTime + Time.deltaTime, 0f, _atkCameraLerpDuration);
        var lerpRate                = _fadeElapsedTime / _atkCameraLerpDuration;
        var cameraTransform         = _mainCamera.transform;
        Vector3 offset              = Quaternion.AngleAxis(_pitch, Vector3.right) * Quaternion.AngleAxis(_yaw, Vector3.up) * _baseDir * _length;
        var nextCameraPosition      = _baseTransform.position + new Vector3(0f, 0.5f, 0f) + offset;
        cameraTransform.position    = Vector3.Lerp(_prevCameraPosition, nextCameraPosition, lerpRate);
        _lookAtPosition             = Vector3.Lerp(_prevLookAtPosition, _lookAtTransform.position, lerpRate);
        cameraTransform.LookAt(_lookAtPosition);
    }

    /// <summary>
    /// 選択カーソルに従うカメラ情報を設定します
    /// </summary>
    /// <param name="pos"></param>
    public void SetLookAtBasedOnSelectCursor( in Vector3 pos )
    {
        _prevCameraPosition       = transform.position;
        _lookAtPosition     = pos;
        _followingPosition  = _lookAtPosition + _offset;
        _followElapsedTime  = 0.0f;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="length"></param>
    public void SetOffsetLength( float length )
    {   
        _offsetLength       = length;
        _offset             = _offset.normalized * _offsetLength;
        _followingPosition  = _lookAtPosition + _offset;
        _followElapsedTime  = 0.0f;
    }

    /// <summary>
    /// 
    /// </summary>
    public void SetCameraParamDatas(in List<CameraParamData[]> closeDatas, in List<CameraParamData[]> rangedDatas)
    {
        _closeAtkCameraParamDatas   = closeDatas;
        _rangedAtkCameraParamDatas  = rangedDatas;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="attacker"></param>
    /// <param name="target"></param>
    public void StartAttackSequenceMode( Character attacker, Character target )
    {
        if (attacker == null || target == null) return;

        List<CameraParamData[]> cameraParamDatas;
        if (attacker.GetBullet() == null) cameraParamDatas = _closeAtkCameraParamDatas;
        else cameraParamDatas = _rangedAtkCameraParamDatas;

        _mode                       = CameraMode.ATTACK_SEQUENCE;
        _atkCameraPhase             = AttackSequenceCameraPhase.START;
        _prevCameraPosition         = transform.position;
        int cameraIndex             = new System.Random().Next(0, cameraParamDatas.Count);
        _currentCameraParamDatas    = cameraParamDatas[cameraIndex];
        _cameraPhaseIndex           = 0;
        _length                     = _currentCameraParamDatas[_cameraPhaseIndex].Length;
        _pitch                      = _currentCameraParamDatas[_cameraPhaseIndex].Pitch;
        _yaw                        = _currentCameraParamDatas[_cameraPhaseIndex].Yaw;
        _baseTransform              = Methods.CompareAllyCharacter(attacker, target).transform;
        _lookAtTransform            = target.transform;
        _baseDir                    = _baseTransform.forward;
        _lookAtPosition             = _lookAtTransform.position;
        _fadeElapsedTime            = 0f;

        // 一度パラメータを非表示
        BattleUISystem.Instance.TogglePlayerParameter(false);
        BattleUISystem.Instance.ToggleEnemyParameter(false);
    }

    /// <summary>
    /// 
    /// </summary>
    public void EndAttackSequenceMode(Character attacker)
    {
        _mode               = CameraMode.ATTACK_SEQUENCE;
        _atkCameraPhase     = AttackSequenceCameraPhase.END;
        _prevCameraPosition = _mainCamera.transform.position;
        _lookAtPosition     = attacker.transform.position;
        _followingPosition  = _lookAtPosition + _offset;
        _fadeElapsedTime    = 0f;
    }

    /// <summary>
    /// 次のカメラパラメータインデックス情報に遷移します
    /// </summary>
    public void TransitNextPhaseCameraParam( Transform nextBase = null, Transform nextLookAt = null )
    {
        _cameraPhaseIndex   = Mathf.Clamp(++_cameraPhaseIndex, 0, _currentCameraParamDatas.Length - 1);        
        _length             = _currentCameraParamDatas[_cameraPhaseIndex].Length;
        _pitch              = _currentCameraParamDatas[_cameraPhaseIndex].Pitch;
        _yaw                = _currentCameraParamDatas[_cameraPhaseIndex].Yaw;
        _fadeElapsedTime    = 0f;
        _prevCameraPosition = _mainCamera.transform.position;
        _prevLookAtPosition = _lookAtPosition;

        if ( nextBase != null ) _baseTransform = nextBase;
        if ( nextLookAt != null ) _lookAtTransform = nextLookAt;
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bool IsFadeAttack()
    {
        return _mode == CameraMode.ATTACK_SEQUENCE && _atkCameraPhase == AttackSequenceCameraPhase.FADE_ATTACK;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bool IsFadeEnd()
    {
        return _mode == CameraMode.FOLLOWING;
    }
}