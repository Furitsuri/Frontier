using Frontier.Entities;
using UnityEngine;

namespace Frontier
{
    /// <summary>
    /// 攻撃者→対象方向を基準にした固定オフセットでカメラを配置する、汎用のスキル用カメラ演出です
    /// (CooperativeSequenceCameraHandlerと同じ計算式)。特定のスキルに依存しない処理内容のため、
    /// この形のカメラ演出を使いたい任意のスキルから利用できます。
    /// </summary>
    public class TargetOffsetCameraProcess : ISkillCameraProcess
    {
        private readonly Character _attacker;
        private readonly Character _target;
        private readonly float _xzDistance;
        private readonly float _height;
        private readonly float _duration;

        private BattleCameraSharedState _ctx;
        private float _elapsed;
        private Vector3 _fromPosition;
        private Vector3 _fromLookAt;
        private Vector3 _targetPosition;
        private Vector3 _lookAtPosition;

        public TargetOffsetCameraProcess( Character attacker, Character target, float xzDistance, float height, float duration )
        {
            _attacker   = attacker;
            _target     = target;
            _xzDistance = xzDistance;
            _height     = height;
            _duration   = duration;
        }

        public void Begin( BattleCameraSharedState sharedState, Vector3 fromPosition, Quaternion fromRotation )
        {
            _ctx          = sharedState;
            _fromPosition = fromPosition;
            _fromLookAt   = sharedState.LookAtPosition;
            _elapsed      = 0f;

            _targetPosition = CalcCameraPosition();
            _lookAtPosition = CalcLookAtPosition();

            _ctx.UiSystem.BattleUi.SetActiveLeftParameterWindow( false );
            _ctx.UiSystem.BattleUi.SetActiveRightParameterWindow( false );
        }

        public void Update()
        {
            _elapsed = Mathf.Clamp( _elapsed + DeltaTimeProvider.DeltaTime, 0f, _duration );
            float t = _duration > 0f ? _elapsed / _duration : 1f;

            _ctx.MainCamera.transform.position = Vector3.Lerp( _fromPosition, _targetPosition, t );
            _ctx.LookAtPosition                = Vector3.Lerp( _fromLookAt, _lookAtPosition, t );
            _ctx.MainCamera.transform.LookAt( _ctx.LookAtPosition );
        }

        public void End()
        {
            _ctx.UiSystem.BattleUi.SetActiveLeftParameterWindow( true );
            _ctx.UiSystem.BattleUi.SetActiveRightParameterWindow( true );
        }

        /// <summary>
        /// 攻撃者→対象のXZ前方を基準に左後方・Y上方のカメラ座標を返します(CooperativeSequenceCameraHandlerと同じ計算式)。
        /// </summary>
        private Vector3 CalcCameraPosition()
        {
            Vector3 atkPos    = _attacker.transform.position;
            Vector3 forwardXZ = _target.transform.position - atkPos;
            forwardXZ.y = 0f;
            if( forwardXZ.sqrMagnitude < 0.0001f ) { forwardXZ = Vector3.forward; }
            forwardXZ.Normalize();

            Vector3 rightXZ     = Vector3.Cross( forwardXZ, Vector3.up );
            Vector3 dirToCamera = ( -forwardXZ - rightXZ ).normalized;

            return atkPos + dirToCamera * _xzDistance + Vector3.up * _height;
        }

        private Vector3 CalcLookAtPosition()
        {
            return ( _attacker.transform.position + _target.transform.position ) * 0.5f;
        }
    }
}
