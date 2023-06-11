using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : Character
{
    // Start is called before the first frame update
    void Start()
    {
        
        // TODO : 試運転用にパラメータをセット。後ほど削除
        this.param.characterIndex = 5;
        this.param.moveRange = 2;
        this.param.initGridIndex = this.tmpParam.gridIndex = 13;
        this.param.MaxHP = this.param.CurHP = 8;
        this.param.Atk = 3;
        this.param.Def = 2;
        this.param.initDir = Constants.Direction.BACK;
        this.param.charaTag = CHARACTER_TAG.CHARACTER_ENEMY;
        this.param.UICameraLengthY = 0.8f;
        this.param.UICameraLengthZ = 1.4f;
        this.param.UICameraLookAtCorrectY = 0.45f;
        BattleManager.instance.AddEnemyToList(this);
    }

    override public void setAnimator(ANIME_TAG animTag, bool b)
    {
        string[] animName =
        {
            "Wait",
            "Run",
            "Attack"
        };
        // TODO : animNameの数とANIME_TAGの数が不一致の場合にエラーを返す

        animator.SetBool(animName[(int)animTag], b);
    }
}
