using Frontier.Battle;
using Frontier.Entities;
using Frontier.Stage;
using Frontier.UI;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static UnityEngine.UI.GridLayoutGroup;

namespace Frontier.Combat
{
    public class DashSlashSA : SkillActionBase
    {
        private enum DashSlashState
        {
            START,
            WAIT_SLASH,
            SLASHING,
            WAIT_END,
            END
        }

        private IUiSystem _uiSystem                     = null;
        private BattleCharacterCoordinator _btlCharaCdr = null;
        private StageController _stageCtrl              = null;

        private DashSlashState _state;
        private bool _isAttackAnimEnded;
        private int _goalTileIndex = -1;
        private Vector3 _velocity;
        private Vector3 _goalPosition;
        private List<Character> _targetCharacters = null;

        [Inject]
        public DashSlashSA( Character owner, List<CharacterKey> targetCharaKeys, BattleRoutineController btlRtnCtrl, StageController stageCtrl, IUiSystem uiSystem ) : base( owner )
        {
            _targetCharacters   = new List<Character>();
            _btlCharaCdr        = btlRtnCtrl.BtlCharaCdr;
            _stageCtrl          = stageCtrl;
            _uiSystem           = uiSystem;

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
            SortTargetCharactersByDistance();

            // 全ターゲットのダメージ予測を計算
            foreach( var target in _targetCharacters )
            {
                _btlCharaCdr.ApplyDamageExpect( _owner, target );
            }

            _velocity = _owner.GetTransformHandler.GetOrderedForward() * Constants.DASH_SLASH_INITIAL_SPEED;

            // ゴーストの位置が移動目標地点
            var ghostObject = _owner.GetGhostObject();
            Debug.Assert( ghostObject != null );
            _goalTileIndex  = ghostObject.TileIndex;
            _goalPosition   = ghostObject.transform.position;
            _owner.CleanupGhost();

            _state = DashSlashState.START;
        }

        protected override void UpdateAction()
        {
            base.UpdateAction();

            switch( _state )
            {
                case DashSlashState.START:
                    _owner.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.DASH_ATTACK_INITIAL );
                    _state = DashSlashState.WAIT_SLASH;
                    break;
                case DashSlashState.WAIT_SLASH:
                    if( _owner.AnimCtrl.IsEndAnimationOnConditionTag( AnimDatas.AnimeConditionsTag.DASH_ATTACK_INITIAL ) )
                    {
                        _owner.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.DASH_ATTACK_LATTER );
                        _owner.GetTransformHandler.SetVelocityAndAcceleration( _velocity, Vector3.zero );
                        _state = DashSlashState.SLASHING;
                    }
                    break;
                case DashSlashState.SLASHING:
                    UpdateAttack2TargetCharacters();
                    UpdateSlashAnimEnd();

                    if( Methods.IsPassedPosition( _owner.GetTransformHandler.GetPosition(), _goalPosition, _velocity ) )
                    {
                        _owner.GetTransformHandler.ResetVelocityAcceleration();
                        _owner.BattleParams.TmpParam.CurrentTileIndex = _goalTileIndex;
                        _state = DashSlashState.WAIT_END;
                    }
                    break;
                case DashSlashState.WAIT_END:
                    UpdateSlashAnimEnd();
                    if( _isAttackAnimEnded )
                    {
                        _state = DashSlashState.END;
                    }
                    break;
                case DashSlashState.END:
                    EndAction();
                    break;
            }
        }

        protected override void EndAction()
        {
            base.EndAction();

            _stageCtrl.UnbindGridCursor();                          // アタッカーキャラクターの設定を解除
            _stageCtrl.ApplyGridCursor2CharacterTile( _owner );
            _stageCtrl.SetActiveGridCursor( true );                 // 選択グリッドを表示
            _stageCtrl.SetActiveTargetCursor( false );              // ターゲットカーソルを非表示
        }

        protected override bool IsFinished()
        {
            return _state == DashSlashState.END;
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

        private void UpdateSlashAnimEnd()
        {
            if( !_isAttackAnimEnded )
            {
                _isAttackAnimEnded = _owner.AnimCtrl.IsEndAnimationOnConditionTag( AnimDatas.AnimeConditionsTag.DASH_ATTACK_LATTER );
            }
        }

        private bool UpdateAttack2TargetCharacters()
        {
            bool attacked = false;
            for( int i = _targetCharacters.Count - 1; i >= 0; --i )
            {
                if( IsInAttackRange( _targetCharacters[i] ) )
                {
                    ApplyDamageToTarget( _targetCharacters[i] );
                    _targetCharacters.RemoveAt( i );
                    attacked = true;
                }
            }
            return attacked;
        }

        private void ApplyDamageToTarget( Character target )
        {
            int hpChange = target.BattleParams.TmpParam.ExpectedHpChange;
            target.GetStatusRef.CurHP += hpChange;

            if( hpChange != 0 )
            {
                if( target.GetStatusRef.CurHP <= 0 )
                {
                    target.GetStatusRef.CurHP = 0;
                    target.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.DIE );
                }
                else
                {
                    target.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.GET_HIT );
                }
            }

            _uiSystem.BattleUi.ShowDamageOnCharacter( target );
        }
    }
}
