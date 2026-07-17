using Frontier.Entities;
using UnityEngine;

namespace Frontier
{
    /// <summary>
    /// 攻撃者→対象方向を基準にした固定オフセットでカメラを配置する、汎用のスキル用カメラ演出です
    /// (CooperativeSequenceCameraHandlerと同じ計算式)。特定のスキルに依存しない処理内容のため、
    /// この形のカメラ演出を使いたい任意のスキルから利用できます。
    ///
    /// 複数フェーズ(例: 遠景で両者を収める→被弾直前に寄る)にも対応しています。
    /// いつフェーズを切り替えるかはスキル側が TransitNextPhase() を呼ぶタイミングで決め、
    /// このクラスは「呼ばれた瞬間の実際のカメラ位置」を起点に、次フェーズのパラメータへ連続的にLerpします。
    /// </summary>
    public class TargetOffsetCameraProcess : ISkillCameraProcess
    {
        private readonly Character _attacker;
        private readonly Character _target;
        private readonly BattleCameraController.CameraParamData[] _phases;

        private BattleCameraSharedState _ctx;
        private int _phaseIndex;
        private float _elapsed;
        private Vector3 _fromPosition;
        private Vector3 _fromLookAt;
        private Vector3 _targetPosition;
        private Vector3 _lookAtPosition;

        /// <summary>
        /// phases は少なくとも1要素必要です。2要素目以降は TransitNextPhase() が呼ばれるたびに順番に適用されます。
        /// </summary>
        public TargetOffsetCameraProcess( Character attacker, Character target, BattleCameraController.CameraParamData[] phases )
        {
            _attacker = attacker;
            _target   = target;
            _phases   = phases;
        }

        public void Begin( BattleCameraSharedState sharedState, Vector3 fromPosition, Quaternion fromRotation )
        {
            _ctx        = sharedState;
            _phaseIndex = 0;
            StartPhase( fromPosition, sharedState.LookAtPosition );

            _ctx.UiSystem.BattleUi.SetActiveLeftParameterWindow( false );
            _ctx.UiSystem.BattleUi.SetActiveRightParameterWindow( false );
        }

        public void TransitNextPhase()
        {
            if( _phaseIndex >= _phases.Length - 1 ) { return; }

            ++_phaseIndex;
            StartPhase( _ctx.MainCamera.transform.position, _ctx.LookAtPosition );
        }

        public void Update()
        {
            float duration = _phases[_phaseIndex].Duration;
            _elapsed = Mathf.Clamp( _elapsed + DeltaTimeProvider.DeltaTime, 0f, duration );
            float t = duration > 0f ? _elapsed / duration : 1f;

            _ctx.MainCamera.transform.position = Vector3.Lerp( _fromPosition, _targetPosition, t );
            _ctx.LookAtPosition                = Vector3.Lerp( _fromLookAt, _lookAtPosition, t );
            _ctx.MainCamera.transform.LookAt( _ctx.LookAtPosition );
        }

        public void End()
        {
            _ctx.UiSystem.BattleUi.SetActiveLeftParameterWindow( true );
            _ctx.UiSystem.BattleUi.SetActiveRightParameterWindow( true );
        }

        private void StartPhase( Vector3 fromPosition, Vector3 fromLookAt )
        {
            _fromPosition = fromPosition;
            _fromLookAt   = fromLookAt;
            _elapsed      = 0f;

            var phase = _phases[_phaseIndex];
            _targetPosition = CalcCameraPosition( phase.XZDistance, phase.Height );
            _lookAtPosition = CalcLookAtPosition();
        }

        /// <summary>
        /// 攻撃者→対象のXZ前方を基準に左後方・Y上方のカメラ座標を返します(CooperativeSequenceCameraHandlerと同じ計算式)。
        /// </summary>
        private Vector3 CalcCameraPosition( float xzDistance, float height )
        {
            Vector3 atkPos    = _attacker.transform.position;
            Vector3 forwardXZ = _target.transform.position - atkPos;
            forwardXZ.y = 0f;
            if( forwardXZ.sqrMagnitude < 0.0001f ) { forwardXZ = Vector3.forward; }
            forwardXZ.Normalize();

            Vector3 rightXZ     = Vector3.Cross( forwardXZ, Vector3.up );
            Vector3 dirToCamera = ( -forwardXZ - rightXZ ).normalized;

            return atkPos + dirToCamera * xzDistance + Vector3.up * height;
        }

        private Vector3 CalcLookAtPosition()
        {
            return ( _attacker.transform.position + _target.transform.position ) * 0.5f;
        }
    }
}
