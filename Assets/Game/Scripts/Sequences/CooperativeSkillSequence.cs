using System.Collections.Generic;
using static Constants;

namespace Frontier.Sequences
{
    /// <summary>
    /// 連携スキルシーケンス。
    /// 各エントリのスキルを COOPERATIVE_SKILL_STAGGER_INTERVAL 秒の間隔で順番に発動し、
    /// 全エントリの発動が完了したら終了します。
    /// </summary>
    public class CooperativeSkillSequence : ISequence
    {
        private enum Phase { ACTIVATING, FINISHING }

        private readonly List<CooperativeSkillEntry> _entries;

        private Phase  _phase;
        private int    _nextStartIndex;
        private float  _elapsedSinceLastStart;
        private bool[] _finished;

        public CooperativeSkillSequence( List<CooperativeSkillEntry> entries )
        {
            _entries = entries;
        }

        public void Start()
        {
            _phase                 = Phase.ACTIVATING;
            _nextStartIndex        = 0;
            _elapsedSinceLastStart = 0f;
            _finished              = new bool[_entries.Count];

            ActivateEntry( 0 );
        }

        public bool Update()
        {
            for( int i = 0; i < _nextStartIndex; ++i )
            {
                if( _finished[i] ) { continue; }
                _finished[i] = _entries[i].SkillAction.Update();
            }

            if( _phase == Phase.ACTIVATING )
            {
                if( _nextStartIndex < _entries.Count )
                {
                    _elapsedSinceLastStart += DeltaTimeProvider.DeltaTime;
                    if( _elapsedSinceLastStart >= COOPERATIVE_SKILL_STAGGER_INTERVAL )
                    {
                        ActivateEntry( _nextStartIndex );
                        _elapsedSinceLastStart = 0f;
                    }
                }
                else
                {
                    _phase = Phase.FINISHING;
                }
            }

            if( _phase == Phase.FINISHING )
            {
                for( int i = 0; i < _finished.Length; ++i )
                {
                    if( !_finished[i] ) { return false; }
                }
                return true;
            }

            return false;
        }

        public void End()
        {
            foreach( var entry in _entries )
            {
                entry.SkillAction.End();
            }
        }

        private void ActivateEntry( int index )
        {
            var entry = _entries[index];
            entry.SkillAction.OnBeforeNameDisplay();
            entry.SkillAction.Start();
            _nextStartIndex = index + 1;
        }
    }
}
