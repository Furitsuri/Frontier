using Frontier.Combat;
using Frontier.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    /// <summary>
    /// 1 対 1 の戦闘シーケンスにおけるカメラ挙動を管理するハンドラです。
    /// BattleCameraController から IBattleCameraSequence として参照されます。
    /// </summary>
    public class AttackSequenceCameraHandler : IBattleCameraSequence
    {
        private enum Phase
        {
            START,          // 戦闘フィールド移行開始〜戦闘開始
            BATTLE_FIELD,   // 戦闘中〜戦闘終了
            END,            // 戦闘終了〜ステージ復帰
        }

        private readonly BattleCameraSharedState _ctx;
        private readonly Action                  _onFinished;
        private readonly AttackSequenceTransitionEffect _transitionEffect;
        private readonly float _atkCameraLerpDuration;

        private readonly List<BattleCameraController.CameraParamData[]> _closeParamDatas;
        private readonly List<BattleCameraController.CameraParamData[]> _rangedParamDatas;

        private Phase     _phase;
        private Transform _cameraBaseTransform;
        private Transform _lookAtTransform;
        private Vector3   _cameraOffset;
        private Vector3   _characterCameraOffset;
        private BattleCameraController.CameraParamData[] _currentParamDatas;
        private int   _cameraPhaseIndex;
        private float _length;
        private float _roll;
        private float _pitch;
        private float _yaw;
        private float _fadeElapsedTime;

        public bool IsInBattleFieldPhase => _phase == Phase.BATTLE_FIELD;

        public AttackSequenceCameraHandler(
            BattleCameraSharedState                          sharedState,
            Action                                           onFinished,
            List<BattleCameraController.CameraParamData[]>  closeParamDatas,
            List<BattleCameraController.CameraParamData[]>  rangedParamDatas,
            AttackSequenceTransitionEffect                   transitionEffect,
            float atkCameraLerpDuration )
        {
            _ctx                  = sharedState;
            _onFinished           = onFinished;
            _closeParamDatas      = closeParamDatas;
            _rangedParamDatas     = rangedParamDatas;
            _transitionEffect     = transitionEffect;
            _atkCameraLerpDuration = atkCameraLerpDuration;
        }

        /// <summary>
        /// 攻撃シーケンス開始時の初期化を行います（StartAttackSequenceMode に相当）。
        /// </summary>
        public void Init( Character attacker, Character target )
        {
            // パリィ使用時は被攻撃者を基点にする
            var cameraFromChara = attacker;
            var cameraToChara   = target;
            if( 0 <= target.BattleLogic.GetUsingSkillSlotIndexById( SkillID.PARRY ) )
            {
                cameraFromChara = target;
                cameraToChara   = attacker;
            }

            var cameraParamDatas = attacker.GetBullet() == null ? _closeParamDatas : _rangedParamDatas;

            _phase                 = Phase.START;
            _cameraBaseTransform   = cameraFromChara.transform;
            _lookAtTransform       = cameraToChara.transform;
            _characterCameraOffset = cameraFromChara.CameraParam.OffsetOnAtkSequence;

            int cameraIndex    = new System.Random().Next( 0, cameraParamDatas.Count );
            _currentParamDatas = cameraParamDatas[cameraIndex];
            _cameraPhaseIndex  = 0;
            _length            = _currentParamDatas[_cameraPhaseIndex].Length;
            _roll              = _currentParamDatas[_cameraPhaseIndex].Roll;
            _pitch             = _currentParamDatas[_cameraPhaseIndex].Pitch;
            _yaw               = _currentParamDatas[_cameraPhaseIndex].Yaw;

            _ctx.PrevCameraPosition = _ctx.MainCamera.transform.position;
            _ctx.LookAtPosition     = ( cameraFromChara.transform.position + cameraToChara.transform.position ) * 0.5f;
            _fadeElapsedTime        = 0f;
            _transitionEffect.Begin();

            _cameraOffset =Methods.RotateVector( _cameraBaseTransform, _pitch, _yaw, _roll, _cameraBaseTransform.forward ) * _length + _characterCameraOffset;

            _ctx.UiSystem.BattleUi.SetActiveLeftParameterWindow( false );
            _ctx.UiSystem.BattleUi.SetActiveRightParameterWindow( false );
        }

        /// <summary>
        /// 戦闘フィールドの設定にカメラの位置と視点を適合させます（AdaptBattleFieldSetting に相当）。
        /// </summary>
        public void AdaptBattleFieldSetting()
        {
            _cameraOffset           = Methods.RotateVector( _cameraBaseTransform, _pitch, _yaw, _roll, _cameraBaseTransform.forward ) * _length + _characterCameraOffset;
            _ctx.PrevCameraPosition = _ctx.MainCamera.transform.position = _cameraBaseTransform.position + _cameraOffset;
            _ctx.PrevLookAtPosition = _lookAtTransform.position;
            _ctx.MainCamera.transform.LookAt( _ctx.PrevLookAtPosition );
        }

        /// <summary>
        /// END フェーズを開始します（EndAttackSequenceMode 呼び出し後に使用）。
        /// </summary>
        public void BeginEndPhase()
        {
            _phase                  = Phase.END;
            _ctx.PrevCameraPosition = _ctx.MainCamera.transform.position;
            _fadeElapsedTime        = 0f;
            _transitionEffect.Begin();
        }

        /// <summary>
        /// 次のカメラパラメータに遷移します（TransitNextPhaseCameraParam に相当）。
        /// </summary>
        public void TransitNextPhase( Transform nextBase = null, Transform nextLookAt = null )
        {
            _cameraPhaseIndex = Mathf.Clamp( ++_cameraPhaseIndex, 0, _currentParamDatas.Length - 1 );
            _length           = _currentParamDatas[_cameraPhaseIndex].Length;
            _roll             = _currentParamDatas[_cameraPhaseIndex].Roll;
            _pitch            = _currentParamDatas[_cameraPhaseIndex].Pitch;
            _yaw              = _currentParamDatas[_cameraPhaseIndex].Yaw;
            _fadeElapsedTime  = 0f;
            _cameraOffset     = Methods.RotateVector( _cameraBaseTransform, _pitch, _yaw, _roll, _cameraBaseTransform.forward ) * _length + _characterCameraOffset;

            _ctx.PrevCameraPosition = _ctx.MainCamera.transform.position;
            _ctx.PrevLookAtPosition = _ctx.LookAtPosition;

            if( nextBase   != null ) { _cameraBaseTransform = nextBase; }
            if( nextLookAt != null ) { _lookAtTransform     = nextLookAt; }
        }

        public void Update()
        {
            switch( _phase )
            {
                case Phase.START:
                {
                    var destCameraPos = _cameraBaseTransform.position + _cameraOffset;
                    if( _transitionEffect.UpdateEnter( _ctx.FollowingPosition, destCameraPos ) )
                    {
                        _ctx.UiSystem.BattleUi.SetActiveLeftParameterWindow( true );
                        _ctx.UiSystem.BattleUi.SetActiveRightParameterWindow( true );
                        _phase = Phase.BATTLE_FIELD;
                    }
                }
                break;

                case Phase.BATTLE_FIELD:
                {
                    if( _cameraBaseTransform == null || _lookAtTransform == null )
                    {
                        Debug.Assert( false );
                        return;
                    }

                    _fadeElapsedTime = Mathf.Clamp( _fadeElapsedTime + DeltaTimeProvider.DeltaTime, 0f, _atkCameraLerpDuration );
                    var lerpRate           = _fadeElapsedTime / _atkCameraLerpDuration;
                    var nextCameraPosition = _cameraBaseTransform.position + _cameraOffset;
                    _ctx.MainCamera.transform.position = Vector3.Lerp( _ctx.PrevCameraPosition, nextCameraPosition, lerpRate );
                    _ctx.LookAtPosition                = Vector3.Lerp( _ctx.PrevLookAtPosition, _lookAtTransform.position, lerpRate );
                    _ctx.MainCamera.transform.LookAt( _ctx.LookAtPosition );
                }
                break;

                case Phase.END:
                {
                    if( _transitionEffect.UpdateExit( _ctx.PrevCameraPosition, _ctx.FollowingPosition ) )
                    {
                        _ctx.UiSystem.BattleUi.SetActiveLeftParameterWindow( true );
                        _ctx.UiSystem.BattleUi.SetActiveRightParameterWindow( true );

                        _ctx.MainCamera.transform.position = _ctx.FollowingPosition;
                        _ctx.MainCamera.transform.LookAt( _ctx.LookAtPosition );

                        _onFinished?.Invoke();
                    }
                }
                break;
            }
        }
    }
}
