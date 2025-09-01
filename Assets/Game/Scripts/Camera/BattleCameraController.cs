using Frontier.Combat;
using Frontier.Entities;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using Frontier.Combat.Skill;

#pragma warning disable 0414

namespace Frontier
{
    public class BattleCameraController : Singleton<BattleCameraController>
    {
        /// <summary>
        /// カメラのモード
        /// </summary>
        public enum CameraMode
        {
            FOLLOWING = 0,      // 選択グリッド追跡状態
            CHARACTER_MOVE,     // キャラクター移動状態
            ATTACK_SEQUENCE,    // 戦闘状態

            NUM
        }

        /// <summary>
        /// 攻撃シーケンスにおけるカメラ処理フェイズ
        /// </summary>
        enum AttackSequenceCameraPhase
        {
            START = 0,      // 戦闘状態に移行開始～戦闘開始まで
            BATTLE_FIELD,   // 戦闘中～戦闘終了まで
            END,            // 戦闘終了後～ステージ状態に遷移まで

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
        private IUiSystem _uiSystem = null;
        // カメラ座標の基点となるトランスフォーム
        private Transform _cameraBaseTransform;
        // カメラの被写体座標の基点となるトランスフォーム
        private Transform _lookAtTransform;
        // カメラ座標に加算するオフセット値
        private Vector3 _cameraOffset;
        // キャラクター毎に設定されたカメラ座標に加算するオフセット値
        private Vector3 _characterCameraOffset;
        // 前状態(≒前フレーム)におけるカメラ座標
        private Vector3 _prevCameraPosition;
        // 被写体座標
        private Vector3 _lookAtPosition;
        // 前状態(≒前フレーム)における被写体座標
        private Vector3 _prevLookAtPosition;
        // カメラの移動目標座標
        private Vector3 _followingPosition;
        // 被写体座標とカメラ座標との差となるオフセット
        private Vector3 _offset;
        // カメラ移動遷移に用いるフェイズのインデックス値
        private int _cameraPhaseIndex       = 0;
        private float _followElapsedTime    = 0.0f;
        private float _fadeElapsedTime      = 0.0f;
        private float _length               = 0.0f;
        private float _roll                 = 0.0f;
        private float _pitch                = 0.0f;
        private float _yaw                  = 0.0f;

        [Inject]
        public void Construct( IUiSystem uiSystem )
        {
            _uiSystem = uiSystem;
        }

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
                    // MEMO : positionを決定してからLookAtを設定しないと、画面にかくつきが発生するため注意
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
        /// ユニットが移動する際のカメラ更新を行います
        /// </summary>
        private void UpdateCharacterMoveCamera()
        {

        }

        /// <summary>
        /// ユニット同士の対戦シーケンスにおけるカメラ更新を行います
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

                        // 指定レートを上回った際はモザイク処理を施す
                        if (_mosaicStartFadeRate <= fadeRate)
                        {
                            _mosaicEffectScript.ToggleEnable(true);

                            // _mosaicStartFadeRateの値に依存しない形でレート変化するように調整している
                            var blockSizeRate = 1.0f - Mathf.Clamp01(_mosaicBlockSizeMaxRate) * (fadeRate - _mosaicStartFadeRate) / (1f - _mosaicStartFadeRate);
                            _mosaicEffectScript.UpdateBlockSizeByRate(blockSizeRate);
                        }

                        if (_fadeDuration <= _fadeElapsedTime)
                        {
                            _mosaicEffectScript.ToggleEnable(false);
                            _mosaicEffectScript.ResetBlockSize();
                            // パラメータを表示
                            _uiSystem.BattleUi.TogglePlayerParameter(true);
                            _uiSystem.BattleUi.ToggleEnemyParameter(true);
                            // 戦闘フィールドに移行
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
                        // STARTの反対の処理
                        if (fadeRate < 1f - _mosaicStartFadeRate)
                        {
                            _mosaicEffectScript.ToggleEnable(true);

                            // _mosaicStartFadeRateの値に依存しない形でレート変化するように調整している
                            var blockSizeRate = 1.0f - Mathf.Clamp01(_mosaicBlockSizeMaxRate) * (1f - (fadeRate / (1f - _mosaicStartFadeRate)));
                            _mosaicEffectScript.UpdateBlockSizeByRate(blockSizeRate);
                        }

                        if (_fadeDuration <= _fadeElapsedTime)
                        {
                            _mosaicEffectScript.ToggleEnable(false);
                            _mosaicEffectScript.ResetBlockSize();
                            _uiSystem.BattleUi.TogglePlayerParameter(true);
                            _uiSystem.BattleUi.ToggleEnemyParameter(true);

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
        /// 選択カーソルに従うカメラ情報を設定します
        /// </summary>
        /// <param name="pos">カメラ対象座標</param>
        public void SetLookAtBasedOnSelectCursor(in Vector3 pos)
        {
            if (_mode == CameraMode.ATTACK_SEQUENCE) return;

            _prevCameraPosition = transform.position;
            _lookAtPosition = pos;
            _followingPosition = _lookAtPosition + _offset;
            _followElapsedTime = 0.0f;
        }

        /// <summary>
        /// バトル時のカメラデータを設定します
        /// </summary>
        /// <param name="closeDatas">近距離攻撃カメラデータ</param>
        /// <param name="rangedDatas">遠距離攻撃カメラデータ</param>
        public void SetCameraParamDatas(in List<CameraParamData[]> closeDatas, in List<CameraParamData[]> rangedDatas)
        {
            _closeAtkCameraParamDatas = closeDatas;
            _rangedAtkCameraParamDatas = rangedDatas;
        }

        /// <summary>
        /// 攻撃遷移開始時のカメラ設定を行います
        /// </summary>
        /// <param name="attacker">攻撃するキャラクター</param>
        /// <param name="target">攻撃対象キャラクター</param>
        public void StartAttackSequenceMode( Character attacker, Character target )
        {
            if (attacker == null || target == null) return;

            // 通常は攻撃キャラクターを視点にしたカメラワークとするが、
            // パリィが使用される場合は必ず被攻撃キャラクターを視点とする
            var cameraFromChara = attacker;
            var cameraToChara   = target;
            if (0 <= target.GetUsingSkillSlotIndexById(ID.SKILL_PARRY))
            {
                cameraFromChara = target;
                cameraToChara   = attacker;
            }

            // 攻撃キャラクターが近接タイプか遠隔タイプかによって参照するカメラデータを変更する
            List<CameraParamData[]> cameraParamDatas;
            if (attacker.GetBullet() == null) cameraParamDatas = _closeAtkCameraParamDatas;
            else cameraParamDatas = _rangedAtkCameraParamDatas;

            _mode                   = CameraMode.ATTACK_SEQUENCE;
            _atkCameraPhase         = AttackSequenceCameraPhase.START;
            _prevCameraPosition     = transform.position;
            _cameraBaseTransform    = cameraFromChara.transform;
            _lookAtTransform        = cameraToChara.transform;
            _characterCameraOffset  = cameraFromChara.Params.CameraParam.OffsetOnAtkSequence;

            // ランダムな値を用いて、カメラ移動のパターンデータから使用するデータを取得する
            int cameraIndex             = new System.Random().Next(0, cameraParamDatas.Count);
            _currentCameraParamDatas    = cameraParamDatas[cameraIndex];
            _cameraPhaseIndex           = 0;
            _length                     = _currentCameraParamDatas[_cameraPhaseIndex].Length;
            _roll                       = _currentCameraParamDatas[_cameraPhaseIndex].Roll;
            _pitch                      = _currentCameraParamDatas[_cameraPhaseIndex].Pitch;
            _yaw                        = _currentCameraParamDatas[_cameraPhaseIndex].Yaw;

            // カメラ基点キャラと被写体キャラ間の中心点に向かってカメラを近づける
            _lookAtPosition     = ( cameraFromChara.transform.position + cameraToChara.transform.position ) * 0.5f;
            _fadeElapsedTime    = 0f;

            // カメラの基点となる座標に加算するオフセット座標をパラメータを参照して計算する
            _cameraOffset = Methods.RotateVector(_cameraBaseTransform, _roll, _pitch, _yaw, _cameraBaseTransform.forward) * _length + _characterCameraOffset;

            // 一度パラメータを非表示
            _uiSystem.BattleUi.TogglePlayerParameter(false);
            _uiSystem.BattleUi.ToggleEnemyParameter(false);
        }

        /// <summary>
        /// 戦闘フィールドの設定にカメラの位置と視点を適合させます
        /// </summary>
        public void AdaptBattleFieldSetting()
        {
            _cameraOffset           = Methods.RotateVector(_cameraBaseTransform, _roll, _pitch, _yaw, _cameraBaseTransform.forward) * _length + _characterCameraOffset;
            _prevCameraPosition     = _mainCamera.transform.position = _cameraBaseTransform.position + _cameraOffset;
            _prevLookAtPosition     = _lookAtTransform.position;
            _mainCamera.transform.LookAt(_prevLookAtPosition);
        }

        /// <summary>
        /// 攻撃遷移終了時のカメラ設定を行います
        /// </summary>
        /// <param name="attacker">攻撃するキャラクター</param>
        public void EndAttackSequenceMode(Character attacker)
        {
            _atkCameraPhase     = AttackSequenceCameraPhase.END;
            _prevCameraPosition = _mainCamera.transform.position;
            _lookAtPosition     = attacker.transform.position;
            _followingPosition  = _lookAtPosition + _offset;
            _fadeElapsedTime    = 0f;
        }

        /// <summary>
        /// 次のカメラパラメータインデックス情報に遷移します
        /// </summary>
        /// <param name="nextBase">遷移先のカメラ位置対象</param>
        /// <param name="nextLookAt">遷移先のカメラ視線対象</param>
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
        /// 攻撃シーケンスに遷移したかを返します
        /// </summary>
        /// <returns>遷移したか否か</returns>
        public bool IsFadeAttack()
        {
            return _mode == CameraMode.ATTACK_SEQUENCE && _atkCameraPhase == AttackSequenceCameraPhase.BATTLE_FIELD;
        }

        /// <summary>
        /// フェードが終了したか否かを返します
        /// </summary>
        /// <returns>フェード終了したか</returns>
        public bool IsFadeEnd()
        {
            return _mode == CameraMode.FOLLOWING;
        }
    }
}