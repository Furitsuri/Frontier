using Frontier.Battle;
using Frontier.Combat;
using Frontier.Entities;
using System.Collections.Generic;
using Zenject;

namespace Frontier.Sequences
{
    public class SkillActionSequence : ISequence
    {
        private enum Phase { SHOW_SKILL_NAME, EXECUTING }

        private readonly SkillActionBase _skillAction;
        private readonly SkillID _skillId;
        private readonly BattleRoutinePresenter _presenter;
        private readonly BattleRoutineController _btlRtnCtrl;
        private readonly Character _attacker;
        private readonly List<CharacterKey> _attackTargetCharaKeys;

        // スキルに参加したキャラクター(使用者と攻撃対象)。終了時の後片付けの対象
        private readonly List<Character> _participants = new List<Character>();

        private Phase _phase;
        private bool _skillStarted;

        [Inject]
        public SkillActionSequence( SkillActionBase skillAction, SkillID skillId, Character attacker, List<CharacterKey> attackTargetCharaKeys,
                                    BattleRoutinePresenter presenter, BattleRoutineController btlRtnCtrl )
        {
            _skillAction            = skillAction;
            _skillId                = skillId;
            _attacker               = attacker;
            _attackTargetCharaKeys  = attackTargetCharaKeys;
            _presenter              = presenter;
            _btlRtnCtrl             = btlRtnCtrl;
        }

        public void Start()
        {
            _phase        = Phase.SHOW_SKILL_NAME;
            _skillStarted = false;

            // 攻撃対象は死亡するとキーで取得出来なくなるため、ダメージを与える前のこの時点で参照を確保しておく
            _participants.Clear();
            CombatSequenceCleanup.CollectParticipants( _attacker, _attackTargetCharaKeys, _btlRtnCtrl.BtlCharaCdr, _participants );

            _skillAction.OnBeforeNameDisplay();
            _presenter.ShowCommandNameUI( _skillId, 0.85f, () => _phase = Phase.EXECUTING );
        }

        public bool Update()
        {
            if( _phase == Phase.SHOW_SKILL_NAME ) { return false; }

            if( !_skillStarted )
            {
                _skillStarted = true;
                _skillAction.Start();
            }

            return _skillAction.Update();
        }

        public void End()
        {
            _skillAction.End();

            // 対象へ向き直った結果、斜めを向いたままにならないよう、縦軸・横軸に沿う向きへ向き直らせる
            if( null != _attacker ) { _attacker.OrderRotateToNearestAxis(); }

            // 通常攻撃と同様に、スキルの効果(自己バフ等)・バフエフェクトは戦闘1回限りのため、ここで取り除く
            CombatSequenceCleanup.ResetParticipants( _participants );
        }
    }
}
