using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Sequences
{
    public class SequenceHandler : FocusRoutineBase
    {
        bool _isReservedExitOnCurrentRoutine = false;
        List<ISequence> _sequences = new List<ISequence>();

        public void Regist( ISequence seq )
        {
            _sequences.Add( seq );

            ScheduleRun();  // 実行を予約
        }

        public int GetSequencesCount() { return _sequences.Count; }

        // Update is called once per frame
        public override void UpdateRoutine()
        {
            if( _sequences.Count <= 0 ) { return; }

            if( _sequences[0].Update() )
            {
                _isReservedExitOnCurrentRoutine = true;
            }
        }

        public override void LateUpdateRoutine()
        {
            if( _isReservedExitOnCurrentRoutine )
            {
                _sequences[0].End();
                _sequences.RemoveAt( 0 );

                // 実行するシーケンスがなくなった場合は終了を予約
                if( _sequences.Count <= 0 )
                {
                    ScheduleExit();
                }
                else
                {
                    _isReservedExitOnCurrentRoutine = false;
                    _sequences[0].Start();
                }
            }
        }

        public override void ScheduleRun()
        {
            base.ScheduleRun();

            _isReservedExitOnCurrentRoutine = false;
        }

        public override void Run()
        {
            base.Run();

            _sequences[0].Start();
        }
    }
}