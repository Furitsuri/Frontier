using Frontier.Entities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frontier
{
    public class EMSelectState : PhaseStateBase
    {
        Enemy _currentEnemy = null;
        IEnumerator<Character> _enemyEnumerator;
        bool _isValidDestination = false;
        bool _isValidTarget = false;

        override public void Init()
        {
            bool isExist = false;

            base.Init();

            // ステージグリッド上のキャラ情報を更新
            _stageCtrl.UpdateGridInfo();

            _enemyEnumerator = _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable(Character.CHARACTER_TAG.ENEMY).GetEnumerator();
            _currentEnemy = null;

            // 行動済みでないキャラクターを選択する
            while (_enemyEnumerator.MoveNext())
            {
                _currentEnemy = _enemyEnumerator.Current as Enemy;

                if (ShouldTransitionToNextCharacter(_currentEnemy))
                {
                    continue;
                }

                isExist = true;

                // 選択グリッドを合わせる
                _stageCtrl.ApplyCurrentGrid2CharacterGrid(_currentEnemy);

                if(!_currentEnemy.GetAi().IsDetermined())
                {
                    (_isValidDestination, _isValidTarget) = _currentEnemy.DetermineDestinationAndTargetWithAI();
                }

                // 攻撃対象がいなかった場合は攻撃済み状態にする
                if (!_isValidTarget)
                {
                    _currentEnemy.SetEndCommandStatus( Character.Command.COMMAND_TAG.ATTACK, true );
                }

                break;
            }

            if (!isExist)
            {
                Back();
            }
        }

        override public bool Update()
        {
            // TODO : FetchDestinationAndTargetのくだりが必要かもしれないけど、一旦コメントアウト

            // int gridIndex;
            // Character targetCharacter = null;

            if ( IsBack() ) return true;

            // _currentEnemy.FetchDestinationAndTarget( out gridIndex, out targetCharacter );

            // 移動行動に遷移するか
            if ( ShouldTransitionToMove( _currentEnemy ) )
            {
                TransitIndex = (int)Character.Command.COMMAND_TAG.MOVE;

                return true;
            }

            // 攻撃行動に遷移するか
            if ( ShouldTransitionToAttack( _currentEnemy ) )
            {
                TransitIndex = (int)Character.Command.COMMAND_TAG.ATTACK;

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
            if (!_isValidDestination) return false;

            if ( em.IsEndCommand( Character.Command.COMMAND_TAG.MOVE ) ) return false;

            return true;
        }

        /// <summary>
        /// 攻撃コマンドに遷移するかを取得します
        /// </summary>
        /// <param name="em">判定する敵キャラクター</param>
        /// <returns>遷移するか否か</returns>
        private bool ShouldTransitionToAttack( Enemy em )
        {
            if ( !_isValidTarget ) return false;

            if ( em.IsEndCommand( Character.Command.COMMAND_TAG.ATTACK ) ) return false;

            return true;
        }

        /// <summary>
        /// 次のキャラクターへ遷移するかを取得します
        /// </summary>
        /// <param name="em">判定するキャラクター</param>
        /// <returns>遷移するか否か</returns>
        private bool ShouldTransitionToNextCharacter( Enemy em )
        {
            if ( em.IsEndCommand( Character.Command.COMMAND_TAG.MOVE ) && em.IsEndCommand( Character.Command.COMMAND_TAG.ATTACK ) )
            {
                em.EndAction();
                return true;
            }

            if ( em.IsEndAction() ) return true;

            return false;
        }
    }
}