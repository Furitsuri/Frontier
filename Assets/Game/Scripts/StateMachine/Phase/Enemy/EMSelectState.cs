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
        foreach( Enemy enemy in BattleManager.Instance.GetEnemies() )
        {
            if( enemy == null )
            {
                Back();
                return true;
            }


        }

        // 全ての敵の行動が終わったため戻る
        Back();

        return true;
    }
}
