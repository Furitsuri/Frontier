using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Character
{
    // Start is called before the first frame update
    void Start()
    {
        // TODO : 試運転用にパラメータをセット。後ほど削除
        this.param.characterIndex = 0;
        this.param.moveRange = 3;
        this.param.initGridIndex = 0;
        BattleManager.instance.AddPlayerToList(this);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
