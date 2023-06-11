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
        this.param.charaTag = CHARACTER_TAG.CHARACTER_PLAYER;
        this.param.UICameraLengthY = 1.2f;
        this.param.UICameraLengthZ = 1.5f;
        this.param.UICameraLookAtCorrectY = 1.0f;
        BattleManager.instance.AddPlayerToList(this);
    }
 
    override public void setAnimator(ANIME_TAG animTag, bool b)
    {
        string[] animName =
        {
            "Wait",
            "Run",
            "Attack01"
        };
        // TODO : animName�̐���ANIME_TAG�̐����s��v�̏ꍇ�ɃG���[��Ԃ�

        switch( animTag )
        {
            case ANIME_TAG.ANIME_TAG_ATTACK_01:
                animator.SetTrigger(animName[(int)animTag]);
                break;
            default:
                animator.SetBool(animName[(int)animTag], b);
                break;
        }
    }
}
