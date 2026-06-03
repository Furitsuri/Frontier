using Frontier.Entities;
using UnityEngine;

namespace Frontier
{
    /// <summary>
    /// 連携スキルシーケンスにおけるカメラ挙動を管理するハンドラです。
    /// BattleCameraController から IBattleCameraSequence として参照されます。
    /// </summary>
    public class CooperativeSequenceCameraHandler : IBattleCameraSequence
    {
        private readonly BattleCameraSharedState _ctx;
        private readonly float _xzDistance;
        private readonly float _height;
        private readonly float _lerpDuration;

        private float   _lerpElapsed;
        private Vector3 _targetCameraPosition;
        private Vector3 _prevLookAtPosition;
        private Vector3 _lookAtPosition;

        public CooperativeSequenceCameraHandler(
            BattleCameraSharedState sharedState,
            float xzDistance,
            float height,
            float lerpDuration )
        {
            _ctx         = sharedState;
            _xzDistance  = xzDistance;
            _height      = height;
            _lerpDuration = lerpDuration;
        }

        /// <summary>
        /// 連携スキルシーケンス開始時の初期化を行います（StartCooperativeSkillSequence に相当）。
        /// </summary>
        public void Begin( Character attacker, Character target )
        {
            _ctx.PrevCameraPosition = _ctx.MainCamera.transform.position;
            _prevLookAtPosition     = _ctx.LookAtPosition;
            _targetCameraPosition   = CalcCameraPosition( attacker, target );
            _lookAtPosition         = CalcLookAtPosition( attacker, target );
            _lerpElapsed            = 0f;

            _ctx.UiSystem.BattleUi.SetActiveLeftParameterWindow( false );
            _ctx.UiSystem.BattleUi.SetActiveRightParameterWindow( false );
        }

        /// <summary>
        /// 次の攻撃者へカメラを滑らかに遷移させます（TransitToNextCooperativeAttacker に相当）。
        /// </summary>
        public void TransitToNext( Character nextAttacker, Character target )
        {
            _ctx.PrevCameraPosition = _ctx.MainCamera.transform.position;
            _prevLookAtPosition     = _lookAtPosition;
            _targetCameraPosition   = CalcCameraPosition( nextAttacker, target );
            _lookAtPosition         = CalcLookAtPosition( nextAttacker, target );
            _lerpElapsed            = 0f;
        }

        /// <summary>
        /// シーケンス終了時の後処理を行います（EndCooperativeSkillSequence の一部に相当）。
        /// FollowingPosition の再計算は呼び出し元（BattleCameraController）が行います。
        /// </summary>
        public void Conclude( Character lastAttacker )
        {
            _ctx.LookAtPosition = lastAttacker != null
                ? lastAttacker.transform.position
                : _ctx.MainCamera.transform.position + _ctx.MainCamera.transform.forward;

            _ctx.UiSystem.BattleUi.SetActiveLeftParameterWindow( true );
            _ctx.UiSystem.BattleUi.SetActiveRightParameterWindow( true );
        }

        public void Update()
        {
            _lerpElapsed = Mathf.Clamp( _lerpElapsed + DeltaTimeProvider.DeltaTime, 0f, _lerpDuration );
            float t = _lerpDuration > 0f ? _lerpElapsed / _lerpDuration : 1f;

            _ctx.MainCamera.transform.position = Vector3.Lerp( _ctx.PrevCameraPosition, _targetCameraPosition, t );
            var lookAt = Vector3.Lerp( _prevLookAtPosition, _lookAtPosition, t );
            _ctx.MainCamera.transform.LookAt( lookAt );
        }

        /// <summary>
        /// 攻撃者→対象の XZ 前方を基準に左後方・Y 上方のカメラ座標を返します。
        /// </summary>
        private Vector3 CalcCameraPosition( Character attacker, Character target )
        {
            Vector3 atkPos    = attacker.transform.position;
            Vector3 forwardXZ = target.transform.position - atkPos;
            forwardXZ.y = 0f;
            if( forwardXZ.sqrMagnitude < 0.0001f ) { forwardXZ = Vector3.forward; }
            forwardXZ.Normalize();

            Vector3 rightXZ     = Vector3.Cross( forwardXZ, Vector3.up );
            Vector3 dirToCamera = ( -forwardXZ - rightXZ ).normalized;

            return atkPos + dirToCamera * _xzDistance + Vector3.up * _height;
        }

        private Vector3 CalcLookAtPosition( Character attacker, Character target )
        {
            return ( attacker.transform.position + target.transform.position ) * 0.5f;
        }
    }
}
