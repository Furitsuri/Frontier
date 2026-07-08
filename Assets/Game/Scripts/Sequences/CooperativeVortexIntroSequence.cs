using Frontier.Battle;
using Frontier.Entities;
using Frontier.UI;
using System.Collections.Generic;
using Zenject;
using static Constants;

namespace Frontier.Sequences
{
    /// <summary>
    /// 連携攻撃の発動前に、参加キャラクターに対して順番に渦巻きエフェクトを表示する演出シーケンスです。
    /// 各キャラクターの表示時間が COOPERATIVE_VORTEX_NEXT_START_RATE(90%)まで経過すると、
    /// 続けて次のキャラクターの表示を開始します。最後のキャラクターの表示が完了すると終了します。
    /// </summary>
    public class CooperativeVortexIntroSequence : ISequence
    {
        [Inject] private IUiSystem               _uiSystem   = null;
        [Inject] private BattleRoutineController _btlRtnCtrl = null;

        private readonly List<Character> _participants;

        private int   _nextStartIndex;
        private float _elapsedSinceCurrentStart;

        public CooperativeVortexIntroSequence( List<Character> participants )
        {
            _participants = participants;
        }

        public void Start()
        {
            _nextStartIndex           = 0;
            _elapsedSinceCurrentStart = 0f;

            ClearMoveAssistDisplays();

            _btlRtnCtrl.GetBtlCameraCtrl.FitCharactersForCooperativeVortex( _participants );

            StartNext();
        }

        public bool Update()
        {
            _elapsedSinceCurrentStart += DeltaTimeProvider.DeltaTime;

            if( _nextStartIndex < _participants.Count &&
                _elapsedSinceCurrentStart >= COOPERATIVE_VORTEX_DURATION * COOPERATIVE_VORTEX_NEXT_START_RATE )
            {
                StartNext();
            }

            return _nextStartIndex >= _participants.Count && _elapsedSinceCurrentStart >= COOPERATIVE_VORTEX_DURATION;
        }

        public void End()
        {
        }

        /// <summary>
        /// 移動を伴うスキルで連携する場合に残っている、移動先のゴーストと移動経路の矢印表示を消します。
        /// </summary>
        private void ClearMoveAssistDisplays()
        {
            foreach( var chara in _participants )
            {
                chara.CleanupGhost();
                chara.BattleLogic.ActionRangeCtrl.ClearMoveDirectionArrows();
            }
        }

        private void StartNext()
        {
            float initialScale = COOPERATIVE_VORTEX_BASE_SCALE + _nextStartIndex * COOPERATIVE_VORTEX_SCALE_STEP;
            _uiSystem.BattleUi.ShowCooperativeVortexOnCharacter( _participants[_nextStartIndex], COOPERATIVE_VORTEX_DURATION, initialScale );
            ++_nextStartIndex;
            _elapsedSinceCurrentStart = 0f;
        }
    }
}
