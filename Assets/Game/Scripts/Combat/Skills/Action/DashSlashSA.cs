using Frontier.Battle;
using Frontier.Entities;
using System.Buffers;
using System.Collections;
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
            SLASHING,
            END
        }

        private DashSlashState _state;
        private Vector3 _velocity;
        private BattleCharacterCoordinator _btlCharaCdr;
        private List<Character> _targetCharacters = null;

        [Inject]
        public DashSlashSA( Character owner, List<CharacterKey> targetCharaKeys, BattleCharacterCoordinator btlCharaCdr ) : base( owner )
        {
            _velocity           = owner.GetTransformHandler.GetOrderedForward() * 10f;
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

            _owner.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.MOVE );
            _owner.GetTransformHandler.SetVelocityAndAcceleration( _velocity, new Vector3( 0f, 0f, 0f ) );
            SortTargetCharactersByDistance();

            _state = DashSlashState.START;
        }

        protected override void UpdateAction()
        {
            base.UpdateAction();
            
            switch( _state )
            {
                case DashSlashState.START:
                    // 最も近い位置の_targetCharactersとの距離が一定以下になったら、SLASHINGに遷移する
                    if( IsInAttackRange( _targetCharacters[0] ) )
                    {
                        _owner.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.SINGLE_ATTACK );
                        // 加速させる
                        _owner.GetTransformHandler.SetVelocityAndAcceleration( _velocity, new Vector3( 0f, 0f, 0f ) );
                        _state = DashSlashState.SLASHING;
                    }
                    break;
                case DashSlashState.SLASHING:
                    UpdateAttack2TargetCharacters();

                    // 目標タイルに到達したら、ENDに遷移する
                    if( true )
                    {
                        _state = DashSlashState.END;
                    }
                    break;
                case DashSlashState.END:
                    EndAction();
                    break;
            }
        }

        private void ApplyDamage( Character target )
        {
            // ダメージを与える
            // _btlCharaCdr.ApplyDamage( target, damage );

            // ダメージを与えた後、targetを_targetCharactersから削除する
            _targetCharacters.Remove( target ); 
        }

        private void SortTargetCharactersByDistance()
        {
            // _targetCharactersを_ownerとの距離が近い順にソートする
        }

        private bool IsInAttackRange( Character target )
        {
            // _targetとの距離が一定以下かどうかを判定する
            return true;
        }

        private bool UpdateAttack2TargetCharacters()
        {
            // _targetCharactersとの距離が一定以下になる毎に攻撃処理を行う
            foreach( var target in _targetCharacters )
            {
                if( true /* 距離判定 */ )
                {
                    ApplyDamage( target );

                    return true;
                }
            }

            return false;
        }
    }
}