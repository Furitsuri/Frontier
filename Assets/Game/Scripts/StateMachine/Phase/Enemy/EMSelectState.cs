using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Frontier.Character;
using static UnityEngine.EventSystems.EventTrigger;

namespace Frontier
{
    public class EMSelectState : PhaseStateBase
    {
        Enemy _currentEnemy = null;
        IEnumerator<Enemy> _enemyEnumerator;
        bool _isValidDestination = false;
        bool _isValidTarget = false;

        override public void Init()
        {
            bool isExist = false;

            base.Init();

            // ステージグリッド上のキャラ情報を更新
            _stageCtrl.UpdateGridInfo();

            _enemyEnumerator = _btlMgr.GetEnemyEnumerable().GetEnumerator();
            _currentEnemy = null;

            // 行動済みでないキャラクターを選択する
            while (_enemyEnumerator.MoveNext())
            {
                _currentEnemy = _enemyEnumerator.Current;
                var tmpParam = _currentEnemy.tmpParam;

                if (IsTransitNextCharacter(tmpParam))
                {
                    continue;
                }

                isExist = true;
                // 選択グリッドを合わせる
                _stageCtrl.ApplyCurrentGrid2CharacterGrid(_currentEnemy);

                if (!_currentEnemy.EmAI.IsDetermined())
                {
                    (_isValidDestination, _isValidTarget) = _currentEnemy.DetermineDestinationAndTargetWithAI();
                }

                // 攻撃対象がいなかった場合は攻撃済み状態にする
                if (!_isValidTarget)
                {
                    _currentEnemy.tmpParam.isEndCommand[(int)Character.Command.COMMAND_TAG.ATTACK] = true;
                }

                break;
            }

            if (!isExist)
            {
                Back();
            }
        }

        public override bool Update()
        {
            int gridIndex;
            Character targetCharacter;

            if (IsBack()) return true;

            var tmpParam = _currentEnemy.tmpParam;
            _currentEnemy.FetchDestinationAndTarget(out gridIndex, out targetCharacter);

            // 移動行動に遷移するか
            if (IsTransitMove(tmpParam))
            {
                TransitIndex = (int)Command.COMMAND_TAG.MOVE;

                return true;
            }

            // 攻撃行動に遷移するか
            if (IsTransitAttack(tmpParam))
            {
                TransitIndex = (int)Command.COMMAND_TAG.ATTACK;

                return true;
            }

            return false;
        }

        private bool IsTransitMove(Character.TmpParameter tmpParam)
        {
            if (!_isValidDestination) return false;

            if (!tmpParam.IsExecutableCommand(Character.Command.COMMAND_TAG.MOVE)) return false;


            return true;
        }

        private bool IsTransitAttack(Character.TmpParameter tmpParam)
        {
            if (!_isValidTarget) return false;

            if (!tmpParam.IsExecutableCommand(Character.Command.COMMAND_TAG.ATTACK)) return false;

            return true;
        }

        private bool IsTransitNextCharacter(Character.TmpParameter tmpParam)
        {
            if (!tmpParam.IsExecutableCommand(Command.COMMAND_TAG.MOVE) && (!tmpParam.IsExecutableCommand(Command.COMMAND_TAG.ATTACK)))
            {
                tmpParam.isEndCommand[(int)Character.Command.COMMAND_TAG.WAIT] = true;
                return true;
            }

            if (!tmpParam.IsExecutableCommand(Character.Command.COMMAND_TAG.WAIT)) return true;

            return false;
        }
    }
}