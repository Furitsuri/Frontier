using Frontier.Battle;
using Frontier.Entities;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier.Combat
{
    public class DashSlashSA : SkillActionBase
    {
        private enum DashSlashState
        {
            START,
            SLASHING,
            END
        }

        private DashSlashState _state;
        private bool _isAttackAnimEnded;
        private Vector3 _velocity;
        private Vector3 _goalPosition;
        private BattleCharacterCoordinator _btlCharaCdr;
        private List<Character> _targetCharacters = null;

        [Inject]
        public DashSlashSA( Character owner, List<CharacterKey> targetCharaKeys, BattleCharacterCoordinator btlCharaCdr ) : base( owner )
        {
            _velocity           = owner.GetTransformHandler.GetOrderedForward() * Constants.DASH_SLASH_INITIAL_SPEED;
            _targetCharacters   = new List<Character>();
            _btlCharaCdr        = btlCharaCdr;
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
            Debug.Log( "DashSlashSA: StartAction" );

            _isAttackAnimEnded = false;
            _owner.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.MOVE );
            _owner.GetTransformHandler.SetVelocityAndAcceleration( _velocity, Vector3.zero );
            SortTargetCharactersByDistance();

            // 最も遠いターゲット位置をゴールとする
            _goalPosition = _targetCharacters.Count > 0
                ? _targetCharacters[_targetCharacters.Count - 1].GetTransformHandler.GetPosition()
                : _owner.GetTransformHandler.GetPosition() + _owner.GetTransformHandler.GetOrderedForward() * Constants.DASH_SLASH_FALLBACK_GOAL_DISTANCE;

            _state = DashSlashState.START;
        }

        protected override void UpdateAction()
        {
            base.UpdateAction();

            switch( _state )
            {
                case DashSlashState.START:
                    if( _targetCharacters.Count > 0 && IsInAttackRange( _targetCharacters[0] ) )
                    {
                        _owner.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.SINGLE_ATTACK );
                        _owner.GetTransformHandler.SetVelocityAndAcceleration( _velocity * Constants.DASH_SLASH_SLASHING_SPEED_MULTIPLIER, Vector3.zero );
                        _state = DashSlashState.SLASHING;
                    }
                    break;
                case DashSlashState.SLASHING:
                    UpdateAttack2TargetCharacters();

                    if( !_isAttackAnimEnded )
                    {
                        _isAttackAnimEnded = _owner.AnimCtrl.IsEndAnimationOnConditionTag( AnimDatas.AnimeConditionsTag.SINGLE_ATTACK );
                    }

                    if( IsPassedGoalPosition() && _isAttackAnimEnded )
                    {
                        _owner.GetTransformHandler.ResetVelocityAcceleration();
                        _state = DashSlashState.END;
                    }
                    break;
                case DashSlashState.END:
                    EndAction();
                    break;
            }
        }

        private void SortTargetCharactersByDistance()
        {
            Vector3 ownerPos = _owner.GetTransformHandler.GetPosition();
            _targetCharacters.Sort( ( a, b ) =>
            {
                float distA = ( a.GetTransformHandler.GetPosition() - ownerPos ).XZ().sqrMagnitude;
                float distB = ( b.GetTransformHandler.GetPosition() - ownerPos ).XZ().sqrMagnitude;
                return distA.CompareTo( distB );
            } );
        }

        private bool IsInAttackRange( Character target )
        {
            Vector3 ownerPos  = _owner.GetTransformHandler.GetPosition();
            Vector3 targetPos = target.GetTransformHandler.GetPosition();
            return ( targetPos - ownerPos ).XZ().magnitude <= Constants.DASH_SLASH_ATTACK_TRIGGER_DISTANCE;
        }

        private bool IsPassedGoalPosition()
        {
            Vector3 ownerPos = _owner.GetTransformHandler.GetPosition();
            Vector3 toGoal   = ( _goalPosition - ownerPos ).XZ();
            Vector3 forward  = _owner.GetTransformHandler.GetOrderedForward().XZ();
            return Vector3.Dot( forward, toGoal ) <= 0f;
        }

        private bool UpdateAttack2TargetCharacters()
        {
            bool attacked = false;
            for( int i = _targetCharacters.Count - 1; i >= 0; --i )
            {
                if( IsInAttackRange( _targetCharacters[i] ) )
                {
                    _targetCharacters.RemoveAt( i );
                    attacked = true;
                }
            }
            return attacked;
        }
    }
}
