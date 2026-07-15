using Frontier.Battle;
using Frontier.Combat;
using Frontier.Entities;
using Frontier.UI;
using System.Collections.Generic;
using Zenject;
using static Constants;

namespace Frontier.Sequences
{
    /// <summary>
    /// 連携スキルシーケンス。
    /// コマンド名を表示した後、各エントリのスキルを COOPERATIVE_SKILL_STAGGER_INTERVAL 秒の
    /// 間隔で順番に発動し、全エントリが完了したら終了します。
    /// 各エントリの発動タイミングで BattleCameraController にカメラ遷移を通知します。
    /// </summary>
    public class CooperativeSkillSequence : ISequence
    {
        private enum Phase { SHOW_COMMAND_NAME, ACTIVATING, FINISHING }

        private const string COOPERATIVE_COMMAND_NAME     = "Cooperation Attack";
        private const float  COOPERATIVE_COMMAND_DURATION = 0.85f;

        [Inject] private IUiSystem               _uiSystem    = null;
        [Inject] private BattleRoutineController _btlRtnCtrl  = null;

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
            _nextStartIndex        = 0;
            _elapsedSinceLastStart = 0f;
            _finished              = new bool[_entries.Count];
            _phase                 = Phase.SHOW_COMMAND_NAME;

            // コマンド名表示前に全エントリのゴーストを非表示
            foreach( var entry in _entries )
            {
                entry.SkillAction.OnBeforeNameDisplay();
            }

            _uiSystem.BattleUi.CommandNameView.Show( COOPERATIVE_COMMAND_NAME, COOPERATIVE_COMMAND_DURATION, () => _phase = Phase.ACTIVATING );
        }

        public bool Update()
        {
            if( _phase == Phase.SHOW_COMMAND_NAME ) { return false; }

            // ACTIVATING に入った最初のフレームで先頭エントリを即時発動
            if( _phase == Phase.ACTIVATING && _nextStartIndex == 0 )
            {
                ActivateEntry( 0 );
            }

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

            // 連携に参加した全キャラクターの行動終了を、連携攻撃が完全に終わったこのタイミングでまとめて確定する
            foreach( var entry in _entries )
            {
                ( entry.Attacker as Player )?.FinalizeCommand( COMMAND_TAG.SKILL );
            }

            var lastEntry = _entries.Count > 0 ? _entries[_entries.Count - 1] : null;
            _btlRtnCtrl.GetBtlCameraCtrl.EndCooperativeSkillSequence( lastEntry?.Attacker );
        }

        private void ActivateEntry( int index )
        {
            var entry   = _entries[index];
            var camCtrl = _btlRtnCtrl.GetBtlCameraCtrl;

            if( index == 0 )
            {
                camCtrl.StartCooperativeSkillSequence( entry.Attacker, entry.Target );
            }
            else
            {
                camCtrl.TransitToNextCooperativeAttacker( entry.Attacker, entry.Target );
            }

            entry.SkillAction.Start();
            _nextStartIndex = index + 1;
        }
    }
}
