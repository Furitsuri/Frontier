using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Character
{
    // Start is called before the first frame update
    void Start()
    {
        // TODO : ���^�]�p�Ƀp�����[�^���Z�b�g�B��قǍ폜
        this.param.characterIndex = 0;
        this.param.moveRange = 3;
        this.param.initGridIndex = 0;
        BattleManager.instance.AddPlayerToList(this);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // 
    override public void setAnimator(ANIME_TAG animTag, bool b)
    {
        string[] animName =
        {
            "Wait",
            "Run",
            "Attack"
        };
        // TODO : animName�̐���ANIME_TAG�̐����s��v�̏ꍇ�ɃG���[��Ԃ�

        animator.SetBool(animName[(int)animTag], b);
    }
}
