using UnityEngine;

namespace Frontier
{
    public class ParryResultEffect
    {
        private ParticleSystem[] _particles;
        private SkillParryController.JudgeResult _result;
        private ParticleSystem _playingParticle;

        /// <summary>
        /// 初期化します
        /// </summary>
        /// <param name="particles">パリィ成功、失敗、ジャスト時に再生する各パーティクル</param>
        public void Init(in ParticleSystem[] particles)
        {
            _particles  = particles;
            _result     = SkillParryController.JudgeResult.NONE;
        }

        /// <summary>
        /// 破棄します
        /// </summary>
        public void terminate()
        {
            _playingParticle = null;
            _result = SkillParryController.JudgeResult.NONE;
        }

        /// <summary>
        /// エフェクト再生を開始します
        /// </summary>
        /// <param name="result">パリィの結果</param>
        public void PlayEffect( SkillParryController.JudgeResult result )
        {
            _playingParticle = _particles[(int)result];
            Debug.Assert(_playingParticle != null);
            _playingParticle.Play();
        }

        /// <summary>
        /// エフェクト終了の判定を返します
        /// </summary>
        /// <returns>終了の正否</returns>
        public bool IsEndPlaying()
        {
            return _playingParticle != null && !_playingParticle.IsAlive();
        }
    }
}