using UnityEngine;

namespace Frontier
{
    public class ParryResultEffect
    {
        private ParticleSystem[] _particles;
        private SkillParryController.JudgeResult _result;
        private ParticleSystem _playingParticle;

        /// <summary>
        /// ���������܂�
        /// </summary>
        /// <param name="particles">�p���B�����A���s�A�W���X�g���ɍĐ�����e�p�[�e�B�N��</param>
        public void Init(in ParticleSystem[] particles)
        {
            _particles  = particles;
            _result     = SkillParryController.JudgeResult.NONE;
        }

        /// <summary>
        /// �j�����܂�
        /// </summary>
        public void terminate()
        {
            _playingParticle = null;
            _result = SkillParryController.JudgeResult.NONE;
        }

        /// <summary>
        /// �G�t�F�N�g�Đ����J�n���܂�
        /// </summary>
        /// <param name="result">�p���B�̌���</param>
        public void PlayEffect( SkillParryController.JudgeResult result )
        {
            _playingParticle = _particles[(int)result];
            Debug.Assert(_playingParticle != null);
            _playingParticle.Play();
        }

        /// <summary>
        /// �G�t�F�N�g�I���̔����Ԃ��܂�
        /// </summary>
        /// <returns>�I���̐���</returns>
        public bool IsEndPlaying()
        {
            return _playingParticle != null && !_playingParticle.IsAlive();
        }
    }
}