using Frontier.Battle;
using Frontier.Entities;
using Frontier.Stage;
using Frontier.UI;
using System.Collections.Generic;
using Zenject;

namespace Frontier.Combat
{
    public class PartOfRangeSABase : SkillActionBase
    {
        private enum PartOfRangeSAState
        {
            START,
            WAIT_ATTACK,
            APPLY_DAMAGE,
            END
        }

        private BattleCharacterCoordinator _btlCharaCdr = null;
        private StageController _stageCtrl              = null;

        private PartOfRangeSAState _state;
        private List<Character> _targetCharacters = null;

        [Inject]
        public PartOfRangeSABase( Character owner, List<CharacterKey> targetCharaKeys, BattleRoutineController btlRtnCtrl, StageController stageCtrl, IUiSystem uiSystem ) : base( owner, uiSystem, btlRtnCtrl.GetBtlCameraCtrl )
        {
            _targetCharacters   = new List<Character>();
            _btlCharaCdr        = btlRtnCtrl.BtlCharaCdr;
            _stageCtrl          = stageCtrl;

            foreach( var key in targetCharaKeys )
            {
                var targetCharacter = _btlCharaCdr.GetCharacter( key );
                if( null != targetCharacter )
                {
                    _targetCharacters.Add( targetCharacter );
                }
            }
        }

        protected override void StartAction()
        {
            base.StartAction();

            foreach( var target in _targetCharacters )
            {
                _btlCharaCdr.ApplyDamageExpect( _owner, target );
            }

            _state = PartOfRangeSAState.START;
        }

        protected override void UpdateAction()
        {
            base.UpdateAction();

            switch( _state )
            {
                case PartOfRangeSAState.START:
                    _owner.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.DOUBLE_ATTACK );
                    _state = PartOfRangeSAState.WAIT_ATTACK;
                    break;
                case PartOfRangeSAState.WAIT_ATTACK:
                    if( _owner.AnimCtrl.IsEndAnimationOnConditionTag( AnimDatas.AnimeConditionsTag.DOUBLE_ATTACK ) )
                    {
                        _state = PartOfRangeSAState.APPLY_DAMAGE;
                    }
                    break;
                case PartOfRangeSAState.APPLY_DAMAGE:
                    foreach( var target in _targetCharacters )
                    {
                        ApplyDamageToTarget( target );
                    }
                    _state = PartOfRangeSAState.END;
                    break;
                case PartOfRangeSAState.END:
                    break;
            }
        }

        protected override void EndAction()
        {
            base.EndAction();

            _stageCtrl.UnbindGridCursor();
            _stageCtrl.ApplyGridCursor2CharacterTile( _owner );
            _stageCtrl.SetActiveGridCursor( true );
            _stageCtrl.SetActiveTargetCursor( false );
        }

        protected override bool IsFinished()
        {
            return _state == PartOfRangeSAState.END;
        }
    }
}