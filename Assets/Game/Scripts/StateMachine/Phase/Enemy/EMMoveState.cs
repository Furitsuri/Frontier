using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EMMoveState : PhaseStateBase
{
    private Enemy _enemy;

    override public void Init()
    {
        var btlInstance = BattleManager.Instance;

        base.Init();

        // ���ݑI�𒆂̃L�����N�^�[�����擾���Ĉړ��͈͂�\��
        _enemy = btlInstance.GetSelectCharacter() as Enemy;
        Debug.Assert(_enemy != null);
    }
}
