using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EMSelectState : PhaseStateBase
{
    override public void Init()
    {
        base.Init();
    }

    public override bool Update()
    {
        foreach( Enemy enemy in BattleManager.Instance.GetEnemyEnumerable() )
        {
            if( enemy == null )
            {
                Back();
                return true;
            }


        }

        // �S�Ă̓G�̍s�����I��������ߖ߂�
        Back();

        return true;
    }
}
