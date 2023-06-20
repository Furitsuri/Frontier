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

        // 現在選択中のキャラクター情報を取得して移動範囲を表示
        _enemy = btlInstance.GetSelectCharacter() as Enemy;
        Debug.Assert(_enemy != null);
    }
}
