using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Character;
using static UnityEngine.EventSystems.EventTrigger;

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
        StageGrid.Instance.UpdateGridInfo();

        _enemyEnumerator = BattleManager.Instance.GetEnemyEnumerable().GetEnumerator();
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
            StageGrid.Instance.ApplyCurrentGrid2CharacterGrid(_currentEnemy);

            if (!_currentEnemy.EmAI.IsDetermined())
            {
               (_isValidDestination, _isValidTarget) = _currentEnemy.DetermineDestinationAndTargetWithAI();
            }

            // 攻撃対象がいなかった場合は攻撃済み状態にする
            if (!_isValidTarget)
            {
                _currentEnemy.tmpParam.isEndCommand[(int)Character.BaseCommand.COMMAND_ATTACK] = true;
            }

            break;
        }

        if(!isExist)
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
        _currentEnemy.FetchDestinationAndTarget( out gridIndex, out targetCharacter );
        
        // 移動行動に遷移するか
        if ( IsTransitMove( tmpParam ) )
        {
            TransitIndex = (int)BaseCommand.COMMAND_MOVE;

            return true;
        }

        // 攻撃行動に遷移するか
        if ( IsTransitAttack( tmpParam ) )
        {
            TransitIndex = (int)BaseCommand.COMMAND_ATTACK;

            return true;
        }

        return false;
    }

    private bool IsTransitMove( Character.TmpParameter tmpParam )
    {
        if(!_isValidDestination) return false;

        if(tmpParam.isEndCommand[(int)Character.BaseCommand.COMMAND_MOVE]) return false;


        return true;
    }

    private bool IsTransitAttack( Character.TmpParameter tmpParam )
    {
        if(!_isValidTarget) return false;

        if(tmpParam.isEndCommand[(int)Character.BaseCommand.COMMAND_ATTACK]) return false;

        return true;
    }

    private bool IsTransitNextCharacter( Character.TmpParameter tmpParam )
    {
        if(tmpParam.isEndCommand[(int)BaseCommand.COMMAND_MOVE] && ( tmpParam.isEndCommand[(int)BaseCommand.COMMAND_ATTACK]))
        {
            tmpParam.isEndCommand[(int)Character.BaseCommand.COMMAND_WAIT] = true;
            return true;
        }

        if (tmpParam.isEndCommand[(int)Character.BaseCommand.COMMAND_WAIT]) return true;

        return false;
    }
}
