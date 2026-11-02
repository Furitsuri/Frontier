using Frontier.Entities;
using System;
using UnityEngine;

namespace Frontier
{
    /// <summary>
    /// 開始時点のカメラの向きを保ったまま、攻撃者と対象の中間点へ少しだけ寄る、汎用のスキル用カメラ演出です。
    /// 攻撃者→対象方向を基準にカメラを回り込ませる TargetOffsetCameraProcess とは異なり、カメラの角度を変えないため、
    /// プレイヤーが操作していないキャラクター(敵・第三勢力)のスキル使用時など、視点を大きく動かしたくない場面で用います。
    /// </summary>
    public class ApproachCameraProcess : ISkillCameraProcess
    {
        private readonly Character _attacker;
        private readonly Character _target;
        private readonly Func<Vector3, Vector3> _calcCameraPosition;
        private readonly float _duration;

        private BattleCameraSharedState _ctx;
        private float _elapsed;
        private Vector3 _fromPosition;
        private Vector3 _fromLookAt;
        private Vector3 _targetPosition;
        private Vector3 _lookAtPosition;

        /// <param name="calcCameraPosition">注視点を受け取り、寄った後のカメラ座標を返す処理(BattleCameraControllerのFOLLOWINGモードと同じ計算式を渡します)</param>
        /// <param name="duration">寄り切るまでにかける秒数</param>
        public ApproachCameraProcess( Character attacker, Character target, Func<Vector3, Vector3> calcCameraPosition, float duration )
        {
            _attacker           = attacker;
            _target             = target;
            _calcCameraPosition = calcCameraPosition;
            _duration           = duration;
        }

        public void Begin( BattleCameraSharedState sharedState, Vector3 fromPosition, Quaternion fromRotation )
        {
            _ctx          = sharedState;
            _elapsed      = 0f;
            _fromPosition = fromPosition;
            _fromLookAt   = sharedState.LookAtPosition;

            // 注視点を攻撃者と対象の中間点へ移し、カメラの角度はそのままに、攻撃対象選択中と同じ距離まで寄る
            _lookAtPosition = ( _attacker.transform.position + _target.transform.position ) * 0.5f;
            _targetPosition = _calcCameraPosition( _lookAtPosition );

            _ctx.Presenter?.SetActiveParameterWindows( false );
        }

        public void TransitNextPhase()
        {
            // 単一フェーズのみのため何もしない
        }

        public void Update()
        {
            _elapsed = Mathf.Clamp( _elapsed + DeltaTimeProvider.DeltaTime, 0f, _duration );
            float t  = _duration > 0f ? _elapsed / _duration : 1f;

            _ctx.MainCamera.transform.position = Vector3.Lerp( _fromPosition, _targetPosition, t );
            _ctx.LookAtPosition                = Vector3.Lerp( _fromLookAt, _lookAtPosition, t );
            _ctx.MainCamera.transform.LookAt( _ctx.LookAtPosition );
        }

        public void End()
        {
            _ctx.Presenter?.SetActiveParameterWindows( true );
        }
    }
}
