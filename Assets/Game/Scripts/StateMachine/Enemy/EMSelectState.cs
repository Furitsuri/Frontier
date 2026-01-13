using Frontier.Combat;
using Frontier.Entities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frontier.StateMachine
{
    public class EmSelectState : PhaseStateBase
    {
        Enemy _currentEnemy = null;
        IEnumerator<Character> _enemyEnumerator;
        bool _isValidDestination = false;
        bool _isValidTarget = false;

        override public void Init()
        {
            bool isExist = false;   // 行動可能なキャラクターが存在するか

            base.Init();

            _enemyEnumerator = _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.ENEMY ).GetEnumerator();
            _currentEnemy = null;

            // 行動済みでないキャラクターを選択する
            while( _enemyEnumerator.MoveNext() )
            {
                _currentEnemy = _enemyEnumerator.Current as Enemy;
                if( ShouldTransitionToNextCharacter( _currentEnemy ) )
                {
                    continue;
                }

                isExist = true;

                _stageCtrl.ApplyCurrentGrid2CharacterTile( _currentEnemy );   // 選択グリッドを合わせる

                if( !_currentEnemy.GetAi().IsDetermined() )
                {
                    (_isValidDestination, _isValidTarget) = _currentEnemy.DetermineDestinationAndTargetWithAI();
                }

                // 攻撃対象がいなかった場合は攻撃済み状態にする
                // ただし、スキルなどで攻撃出来ない状態になっている可能性があるため、SetEndCommandStatus( COMMAND_TAG.ATTACK, _isValidTarget ) としてはならない
                if( !_isValidTarget )
                {
                    _currentEnemy.Params.TmpParam.SetEndCommandStatus( COMMAND_TAG.ATTACK, true );
                }

                break;
            }

            if( !isExist )
            {
                Back();
            }
        }

        override public bool Update()
        {
            // TODO : FetchDestinationAndTargetのくだりが必要かもしれないけど、一旦コメントアウト

            // int gridIndex;
            // Character targetCharacter = null;

            if( IsBack() ) { return true; }

            // _currentEnemy.FetchDestinationAndTarget( out gridIndex, out targetCharacter );

            // 移動行動に遷移するか
            if( ShouldTransitionToMove( _currentEnemy ) )
            {
                TransitStateWithExit( ( int ) COMMAND_TAG.MOVE );

                return true;
            }

            // 攻撃行動に遷移するか
            if( ShouldTransitionToAttack( _currentEnemy ) )
            {
                TransitStateWithExit( ( int ) COMMAND_TAG.ATTACK );

                return true;
            }

            return false;
        }

        /// <summary>
        /// 移動コマンドに遷移するかを取得します
        /// </summary>
        /// <param name="em">判定する敵キャラクター</param>
        /// <returns>遷移するか否か</returns>
        private bool ShouldTransitionToMove( Enemy em )
        {
            if( !_isValidDestination ) return false;

            if( em.Params.TmpParam.IsEndCommand( COMMAND_TAG.MOVE ) ) return false;

            return true;
        }

        /// <summary>
        /// 攻撃コマンドに遷移するかを取得します
        /// </summary>
        /// <param name="em">判定する敵キャラクター</param>
        /// <returns>遷移するか否か</returns>
        private bool ShouldTransitionToAttack( Enemy em )
        {
            if( !_isValidTarget ) return false;

            if( em.Params.TmpParam.IsEndCommand( COMMAND_TAG.ATTACK ) ) return false;

            return true;
        }

        /// <summary>
        /// 次のキャラクターへ遷移するかを取得します
        /// </summary>
        /// <param name="em">判定するキャラクター</param>
        /// <returns>遷移するか否か</returns>
        private bool ShouldTransitionToNextCharacter( Enemy em )
        {
            if( em.Params.TmpParam.IsEndCommand( COMMAND_TAG.MOVE ) && em.Params.TmpParam.IsEndCommand( COMMAND_TAG.ATTACK ) )
            {
                em.Params.TmpParam.EndAction();
                return true;
            }

            if( em.Params.TmpParam.IsEndAction() ) return true;

            return false;
        }
    }
}