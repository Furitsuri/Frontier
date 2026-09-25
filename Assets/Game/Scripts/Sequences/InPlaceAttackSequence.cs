using Frontier.Entities;
using UnityEngine;

namespace Frontier.Sequences
{
    /// <summary>
    /// 操作中のステージ上でそのまま攻撃を完結させる攻撃シーケンスです。
    /// カメラの遷移や戦闘フィールドへの配置は行わず、近接攻撃でも相手へ駆け寄らずにその場で攻撃モーションを再生します。
    /// 攻撃終了後は、攻撃前の向きへ滑らかに戻します。
    /// 対象選択中のカメラズームはシーケンス終了まで維持します。
    /// </summary>
    public class InPlaceAttackSequence : CharacterAttackSequenceBase
    {
        private Quaternion _atkCharaFacingRot = Quaternion.identity;
        private Quaternion _tgtCharaFacingRot = Quaternion.identity;

        public InPlaceAttackSequence( Character attackChara, Character targetChara ) : base( attackChara, targetChara ) { }

        protected override COMBAT_ANIMATION_TYPE ClosedAnimationType => COMBAT_ANIMATION_TYPE.CLOSED_IN_PLACE;

        // カメラの切り替えや余韻の演出が無いため、攻撃前後の待ちは設けない
        protected override float WaitAttackTime => 0f;
        protected override float WaitEndTime    => 0f;

        protected override void OnStart() { }

        protected override bool IsReadyToAttack( float rotateRate ) => 1f <= rotateRate;

        protected override void OnFinishAttack()
        {
            // 攻撃中に向き合った状態から、攻撃前の向きへ戻す際の起点を記録する
            _atkCharaFacingRot = _attackCharacter.GetRotation();
            _tgtCharaFacingRot = _targetCharacter.GetRotation();
        }

        protected override bool UpdateFinishing()
        {
            _elapsedTime += DeltaTimeProvider.DeltaTime;
            float t = Mathf.Clamp01( _elapsedTime / Constants.ATTACK_ROTATIION_TIME );
            t = Mathf.SmoothStep( 0f, 1f, t );

            _attackCharacter.SetRotation( Quaternion.Lerp( _atkCharaFacingRot, _atkCharaInitialRot, t ) );
            _targetCharacter.SetRotation( Quaternion.Lerp( _tgtCharaFacingRot, _tgtCharaInitialRot, t ) );

            if( t < 1f ) { return false; }

            // 対象選択中から維持してきたカメラズームを、シーケンスの終了に合わせて解除する
            _btlRtnCtrl.GetBtlCameraCtrl.SetTargetSelectZoomActive( false );

            return true;
        }
    }
}
