using Frontier.Entities;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier
{
    public class BattleCameraController : MonoBehaviour
    {
        private enum CameraDirection { LEFT, RIGHT }

        [System.Serializable]
        public struct CameraParamData
        {
            public string Phase;
            public float Length;
            public float Roll;
            public float Pitch;
            public float Yaw;
        }

        // --- 連携スキルシーケンス用カメラパラメータ ---
        [Header("連携スキルシーケンス用カメラパラメータ")]
        [SerializeField] private float _coopCameraXZDistance   = 5.0f;
        [SerializeField] private float _coopCameraHeight       = 4.0f;
        [SerializeField] private float _coopCameraLerpDuration = 0.5f;

        // --- 連携演出(渦巻きエフェクト)用カメラパラメータ ---
        [Header("連携演出(渦巻きエフェクト)用カメラパラメータ")]
        [SerializeField] private float _vortexIntroZoomOutFactor = 2.0f;   // キャラクター間の広がりに対するカメラを引く量の係数

        // --- 攻撃シーケンス用カメラパラメータ ---
        [Header("攻撃シーケンス用カメラパラメータ")]
        [SerializeField] private float _fadeDuration            = 0.4f;
        [SerializeField] private float _atkCameraLerpDuration   = 0.2f;
        [SerializeField] private float _mosaicStartFadeRate     = 0.0f;
        [SerializeField] private float _mosaicBlockSizeMaxRate  = 0.5f;

        // --- FOLLOWING モード用パラメータ ---
        [Header("XZ平面上のカメラの移動をスライドで行わせる場合はチェックを入れてください")]
        [SerializeField] private bool  _cameraXZSlide = false;
        [ShowIf( nameof( _cameraXZSlide ) )]
        [SerializeField] private float _inputThreshold = 0f;

        [Space(5)]
        [SerializeField] private float _inputCoefficientOnCameraSlide = 3f;
        [SerializeField] private float _angleYZMin                    = 30f;
        [SerializeField] private float _angleYZMax                    = 80f;
        [SerializeField] private float _followDuration                = 1f;

        [Inject] private IUiSystem   _uiSystem = null;
        [Inject] private InputFacade _inputFcd  = null;

        // --- FOLLOWING モード専用フィールド ---
        private bool  _cameraSliding      = false;
        private float _initialValueAngleXZ = 0f;
        private float _followElapsedTime   = 0.0f;
        private float _offsetLength        = 0.0f;
        private float _angleXZ             = 0.0f;
        private float _angleYZ             = 0.0f;
        private float _startAngleXZ        = 0.0f;
        private float _goalAngleXZ         = 0.0f;

        // --- 攻撃シーケンス用カメラデータ（BattleFileLoader から設定）---
        private List<CameraParamData[]> _closeAtkCameraParamDatas;
        private List<CameraParamData[]> _rangedAtkCameraParamDatas;

        // --- ハンドラ / 共有状態 ---
        private BattleCameraSharedState         _sharedState;
        private IBattleCameraSequence           _activeSequence;
        private AttackSequenceCameraHandler     _attackSeqHandler;
        private CooperativeSequenceCameraHandler _coopSeqHandler;

        public float InitialAngleXZ => _initialValueAngleXZ;
        public float AngleXZ        => _angleXZ;

        void Update()
        {
            if( _activeSequence != null )
            {
                _activeSequence.Update();
                return;
            }

            // FOLLOWING モード
            if( _cameraSliding )
            {
                _followElapsedTime = Mathf.Clamp( _followElapsedTime + DeltaTimeProvider.DeltaTime, 0f, 0.3f );
                _angleXZ = Mathf.LerpAngle( _startAngleXZ, _goalAngleXZ, _followElapsedTime / 0.3f );

                _sharedState.FollowingPosition =
                    _sharedState.PrevCameraPosition =
                    _sharedState.MainCamera.transform.position =
                        Quaternion.Euler( _angleYZ, _angleXZ, 0 ) * Vector3.back * _offsetLength + _sharedState.LookAtPosition;
                _sharedState.MainCamera.transform.rotation = Quaternion.Euler( _angleYZ, _angleXZ, 0f );

                _cameraSliding = !( Mathf.Abs( _goalAngleXZ - _angleXZ ) <= 0f );
            }
            else
            {
                // MEMO : position を決定してから LookAt を設定しないと画面にかくつきが発生する
                _followElapsedTime = Mathf.Clamp( _followElapsedTime + DeltaTimeProvider.DeltaTime, 0f, _followDuration );
                _sharedState.MainCamera.transform.position = Vector3.Lerp( _sharedState.PrevCameraPosition, _sharedState.FollowingPosition, _followElapsedTime / _followDuration );
                _sharedState.MainCamera.transform.rotation = Quaternion.Euler( _angleYZ, _angleXZ, 0f );
            }
        }

        public void Setup( bool createMosaicEff )
        {
            _sharedState = new BattleCameraSharedState
            {
                MainCamera = Camera.main,
                UiSystem   = _uiSystem,
            };

            if( createMosaicEff )
            {
                LazyInject.GetOrCreate( ref _sharedState.MosaicEffect, () => _sharedState.MainCamera.GetComponent<CameraMosaicEffect>() );
            }
        }

        public void Init()
        {
            _sharedState.PrevCameraPosition = _sharedState.MainCamera.transform.position;
            _sharedState.LookAtPosition     = _sharedState.MainCamera.transform.position + _sharedState.MainCamera.transform.forward;
            _sharedState.FollowingPosition  = _sharedState.MainCamera.transform.position;

            var offset           = _sharedState.FollowingPosition - _sharedState.MainCamera.transform.forward;
            _offsetLength        = offset.magnitude;
            _initialValueAngleXZ = _angleXZ = Vector3.Angle( Vector3.back, new Vector3( offset.x, 0, offset.z ) );
            _angleYZ             = Vector3.Angle( Vector3.back, new Vector3( 0, offset.y, offset.z ) );

            _activeSequence   = null;
            _attackSeqHandler = null;
            _coopSeqHandler   = null;

            RegisterInputCodes();
        }

        /// <summary>選択カーソルに従うカメラ情報を設定します。</summary>
        public void SetLookAtBasedOnSelectCursor( in Vector3 pos )
        {
            if( _activeSequence != null ) { return; }
            if( _cameraSliding )          { return; }

            _sharedState.PrevCameraPosition = _sharedState.MainCamera.transform.position;
            _sharedState.LookAtPosition     = pos;
            _sharedState.FollowingPosition  = Quaternion.Euler( _angleYZ, _angleXZ, 0 ) * Vector3.back * _offsetLength + _sharedState.LookAtPosition;
            _followElapsedTime              = 0.0f;
        }

        /// <summary>バトル時のカメラデータを設定します。</summary>
        public void SetCameraParamDatas( in List<CameraParamData[]> closeDatas, in List<CameraParamData[]> rangedDatas )
        {
            _closeAtkCameraParamDatas  = closeDatas;
            _rangedAtkCameraParamDatas = rangedDatas;
        }

        // -----------------------------------------------------------------------
        // 攻撃シーケンス API（CharacterAttackSequence から呼ばれる）
        // -----------------------------------------------------------------------

        public void StartAttackSequenceMode( Character attacker, Character target )
        {
            if( attacker == null || target == null ) { return; }

            _attackSeqHandler ??= new AttackSequenceCameraHandler(
                _sharedState, OnSequenceFinished,
                _closeAtkCameraParamDatas, _rangedAtkCameraParamDatas,
                _fadeDuration, _atkCameraLerpDuration, _mosaicStartFadeRate, _mosaicBlockSizeMaxRate );

            _attackSeqHandler.Init( attacker, target );
            _activeSequence = _attackSeqHandler;
        }

        public void AdaptBattleFieldSetting()          => _attackSeqHandler.AdaptBattleFieldSetting();

        public void EndAttackSequenceMode( Character attacker )
        {
            _sharedState.LookAtPosition    = attacker.transform.position;
            _sharedState.FollowingPosition = Quaternion.Euler( _angleYZ, _angleXZ, 0 ) * Vector3.back * _offsetLength + _sharedState.LookAtPosition;
            _attackSeqHandler.BeginEndPhase();
        }

        public void TransitNextPhaseCameraParam( Transform nextBase = null, Transform nextLookAt = null )
            => _attackSeqHandler.TransitNextPhase( nextBase, nextLookAt );

        /// <summary>攻撃フィールドのフェードインが完了したかを返します。</summary>
        public bool IsFadeAttack() => _attackSeqHandler?.IsInBattleFieldPhase ?? false;

        /// <summary>フェードアウトが完了し FOLLOWING モードに戻ったかを返します。</summary>
        public bool IsFadeEnd()    => _activeSequence == null;

        // -----------------------------------------------------------------------
        // 連携演出(渦巻きエフェクト) API（CooperativeVortexIntroSequence から呼ばれる）
        // -----------------------------------------------------------------------

        /// <summary>
        /// 連携演出(渦巻きエフェクト)の開始前に、参加キャラクター全員がカメラに収まるようカメラを引きます。
        /// </summary>
        public void FitCharactersForCooperativeVortex( List<Character> characters )
        {
            if( characters == null || characters.Count == 0 ) { return; }

            Vector3 center = Vector3.zero;
            foreach( var chara in characters ) { center += chara.transform.position; }
            center /= characters.Count;

            float maxDistFromCenter = 0f;
            foreach( var chara in characters )
            {
                float dist = Vector3.Distance( chara.transform.position, center );
                if( dist > maxDistFromCenter ) { maxDistFromCenter = dist; }
            }

            float requiredOffset = Mathf.Max( _offsetLength, maxDistFromCenter * _vortexIntroZoomOutFactor );

            _sharedState.PrevCameraPosition = _sharedState.MainCamera.transform.position;
            _sharedState.LookAtPosition     = center;
            _sharedState.FollowingPosition  = Quaternion.Euler( _angleYZ, _angleXZ, 0 ) * Vector3.back * requiredOffset + center;
            _followElapsedTime              = 0f;
        }

        // -----------------------------------------------------------------------
        // 連携スキルシーケンス API（CooperativeSkillSequence から呼ばれる）
        // -----------------------------------------------------------------------

        public void StartCooperativeSkillSequence( Character attacker, Character target )
        {
            if( attacker == null || target == null ) { return; }

            _coopSeqHandler ??= new CooperativeSequenceCameraHandler(
                _sharedState, _coopCameraXZDistance, _coopCameraHeight, _coopCameraLerpDuration );

            _coopSeqHandler.Begin( attacker, target );
            _activeSequence = _coopSeqHandler;
        }

        public void TransitToNextCooperativeAttacker( Character nextAttacker, Character target )
            => _coopSeqHandler.TransitToNext( nextAttacker, target );

        public void EndCooperativeSkillSequence( Character lastAttacker )
        {
            _coopSeqHandler.Conclude( lastAttacker );

            _sharedState.FollowingPosition  = Quaternion.Euler( _angleYZ, _angleXZ, 0 ) * Vector3.back * _offsetLength + _sharedState.LookAtPosition;
            _sharedState.PrevCameraPosition = _sharedState.MainCamera.transform.position;
            _followElapsedTime              = 0f;
            _activeSequence                 = null;
        }

        // -----------------------------------------------------------------------
        // 内部処理
        // -----------------------------------------------------------------------

        private void OnSequenceFinished()
        {
            _sharedState.PrevCameraPosition = _sharedState.MainCamera.transform.position;
            _followElapsedTime = _followDuration;
            _activeSequence = null;
        }

        private void StartSlide( CameraDirection dir )
        {
            _cameraSliding = true;
            _angleXZ       = ( _angleXZ + 360f ) % 360f;
            _startAngleXZ  = _angleXZ;
            _goalAngleXZ   = ( dir == CameraDirection.LEFT ) ? _angleXZ - 90f : _angleXZ + 90f;
        }

        private void RegisterInputCodes()
        {
            int hashCode = Hash.GetStableHash( Constants.INPUT_CAMERA_STRING );
            _inputFcd.RegisterInputCodes( ( new GuideIcon[] { GuideIcon.POINTER_MOVE, GuideIcon.POINTER_RIGHT }, "CAMERA\nMOVE", CanAcceptCamera, new AcceptContextInput( AcceptCameraInput ), 0.0f, hashCode ) );
        }

        private bool CanAcceptCamera()
        {
            return _activeSequence == null && !_cameraSliding;
        }

        private bool AcceptCameraInput( InputContext context )
        {
            if( !context.GetButton( GameButton.PointerRight ) ) { return false; }
            if( context.Stick.SqrMagnitude() <= 0f )            { return false; }

            if( _cameraXZSlide )
            {
                if( _inputThreshold <= Mathf.Abs( context.Stick.x ) )
                {
                    StartSlide( context.Stick.x < 0 ? CameraDirection.LEFT : CameraDirection.RIGHT );
                }
            }
            else
            {
                _angleXZ += context.Stick.x * _inputCoefficientOnCameraSlide;
            }

            _angleYZ = Mathf.Clamp( _angleYZ - context.Stick.y * _inputCoefficientOnCameraSlide, _angleYZMin, _angleYZMax );
            _sharedState.FollowingPosition = _sharedState.PrevCameraPosition =
                Quaternion.Euler( _angleYZ, _angleXZ, 0 ) * Vector3.back * _offsetLength + _sharedState.LookAtPosition;

            return true;
        }
    }
}
