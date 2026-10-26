using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Sequences
{
    public class SequenceHandler : FocusRoutineBase
    {
        bool _isReservedExitOnCurrentRoutine = false;
        List<ISequence> _sequences = new List<ISequence>();
        // シーケンス実行中も毎フレーム呼び出す処理。実行中は他のルーチンが一時停止されるため、
        // 停止中も継続させたい処理(パラメータUIのキャラクター映像の更新等)はここに登録する
        Action _updateWhileRunning = null;

        public void Regist( ISequence seq )
        {
            _sequences.Add( seq );

            ScheduleRun();  // 実行を予約
        }

        public int GetSequencesCount() { return _sequences.Count; }

        /// <summary>
        /// シーケンス実行中も毎フレーム呼び出す処理を登録します
        /// </summary>
        public void RegistUpdateWhileRunning( Action action )
        {
            _updateWhileRunning -= action;  // 二重登録の防止
            _updateWhileRunning += action;
        }

        /// <summary>
        /// RegistUpdateWhileRunningで登録した処理を解除します
        /// </summary>
        public void UnregistUpdateWhileRunning( Action action )
        {
            _updateWhileRunning -= action;
        }

        // Update is called once per frame
        public override void UpdateRoutine()
        {
            if( _sequences.Count <= 0 ) { return; }

            if( _sequences[0].Update() )
            {
                _isReservedExitOnCurrentRoutine = true;
            }

            _updateWhileRunning?.Invoke();
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