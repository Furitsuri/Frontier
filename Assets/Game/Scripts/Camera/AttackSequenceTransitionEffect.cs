using UnityEngine;

namespace Frontier
{
    /// <summary>
    /// 攻撃シーケンスにおける、ステージ⇔戦闘フィールド間の遷移演出(モザイクをかけながらのカメラフェード)を担います。
    /// 演出を行うかどうかはオプション(OptionSaveData.IsAttackCloseUpEnabled)で決まり、無効な場合はこのクラス自体が使われません。
    /// </summary>
    public class AttackSequenceTransitionEffect
    {
        private readonly BattleCameraSharedState _ctx;
        private readonly float _fadeDuration;
        private readonly float _mosaicStartFadeRate;
        private readonly float _mosaicBlockSizeMaxRate;

        private float _elapsedTime;

        public AttackSequenceTransitionEffect(
            BattleCameraSharedState sharedState,
            float fadeDuration,
            float mosaicStartFadeRate,
            float mosaicBlockSizeMaxRate )
        {
            _ctx                    = sharedState;
            _fadeDuration           = fadeDuration;
            _mosaicStartFadeRate    = mosaicStartFadeRate;
            _mosaicBlockSizeMaxRate = mosaicBlockSizeMaxRate;
        }

        /// <summary>
        /// 遷移演出の経過時間をリセットします。各遷移の開始時に呼び出してください。
        /// </summary>
        public void Begin()
        {
            _elapsedTime = 0f;
        }

        /// <summary>
        /// ステージから戦闘フィールドへの遷移演出を更新します。
        /// </summary>
        /// <param name="from">フェード開始時のカメラ位置</param>
        /// <param name="to">フェード終了時のカメラ位置</param>
        /// <returns>遷移が完了した場合は true</returns>
        public bool UpdateEnter( in Vector3 from, in Vector3 to )
        {
            var fadeRate = AdvanceAndMoveCamera( from, to );

            if( _mosaicStartFadeRate <= fadeRate )
            {
                var blockSizeRate = 1.0f - Mathf.Clamp01( _mosaicBlockSizeMaxRate ) * ( fadeRate - _mosaicStartFadeRate ) / ( 1f - _mosaicStartFadeRate );
                ApplyMosaic( blockSizeRate );
            }

            return CompleteIfFinished();
        }

        /// <summary>
        /// 戦闘フィールドからステージへの遷移演出を更新します。
        /// </summary>
        /// <param name="from">フェード開始時のカメラ位置</param>
        /// <param name="to">フェード終了時のカメラ位置</param>
        /// <returns>遷移が完了した場合は true</returns>
        public bool UpdateExit( in Vector3 from, in Vector3 to )
        {
            var fadeRate = AdvanceAndMoveCamera( from, to );

            if( fadeRate < 1f - _mosaicStartFadeRate )
            {
                var blockSizeRate = 1.0f - Mathf.Clamp01( _mosaicBlockSizeMaxRate ) * ( 1f - ( fadeRate / ( 1f - _mosaicStartFadeRate ) ) );
                ApplyMosaic( blockSizeRate );
            }

            return CompleteIfFinished();
        }

        private float AdvanceAndMoveCamera( in Vector3 from, in Vector3 to )
        {
            _elapsedTime = Mathf.Clamp( _elapsedTime + DeltaTimeProvider.DeltaTime, 0f, _fadeDuration );
            var fadeRate = _elapsedTime / _fadeDuration;
            _ctx.MainCamera.transform.position = Vector3.Lerp( from, to, fadeRate );
            _ctx.MainCamera.transform.LookAt( _ctx.LookAtPosition );

            return fadeRate;
        }

        private void ApplyMosaic( float blockSizeRate )
        {
            _ctx.MosaicEffect.ToggleEnable( true );
            _ctx.MosaicEffect.UpdateBlockSizeByRate( blockSizeRate );
        }

        private bool CompleteIfFinished()
        {
            if( _elapsedTime < _fadeDuration ) { return false; }

            _ctx.MosaicEffect.ToggleEnable( false );
            _ctx.MosaicEffect.ResetBlockSize();

            return true;
        }
    }
}
