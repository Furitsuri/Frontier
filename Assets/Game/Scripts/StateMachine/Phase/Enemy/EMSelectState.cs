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

        // ‘S‚Ä‚Ì“G‚Ìs“®‚ªI‚í‚Á‚½‚½‚ß–ß‚é
        Back();

        return true;
    }
}
