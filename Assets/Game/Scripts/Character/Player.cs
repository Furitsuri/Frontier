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
        this.param.charaTag = CHARACTER_TAG.CHARACTER_PLAYER;
        this.param.UICameraLengthY = 1.2f;
        this.param.UICameraLengthZ = 1.5f;
        this.param.UICameraLookAtCorrectY = 1.0f;
        BattleManager.instance.AddPlayerToList(this);
    }

    override public void setAnimator(ANIME_TAG animTag)
    {
        string[] animName =
        {
            "Wait",
            "Run",
            "Attack01"
        };
        // TODO : animNameの数とANIME_TAGの数が不一致の場合にエラーを返す

        animator.SetTrigger(animName[(int)animTag]);
    }
    override public void setAnimator(ANIME_TAG animTag, bool b)
    {
        string[] animName =
        {
            "Wait",
            "Run",
            "Attack01"
        };
        // TODO : animNameの数とANIME_TAGの数が不一致の場合にエラーを返す

        animator.SetBool(animName[(int)animTag], b);
    }
}
