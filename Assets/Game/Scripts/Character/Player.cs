using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Character
{
    // Start is called before the first frame update
    void Start()
    {
        // TODO : 試運転用にパラメータをセット。後ほど削除
        this.param.characterTag = CHARACTER_TAG.CHARACTER_PLAYER;
        this.param.characterIndex = 0;
        this.param.moveRange = 3;
        this.param.initGridIndex = 0;
        this.param.UICameraLengthY = 1.2f;
        this.param.UICameraLengthZ = 1.5f;
        this.param.UICameraLookAtCorrectY = 1.0f;
        BattleManager.Instance.AddPlayerToList(this);
    }

    override public void setAnimator(ANIME_TAG animTag)
    {
        _animator.SetTrigger(_animNames[(int)animTag]);
    }
    override public void setAnimator(ANIME_TAG animTag, bool b)
    {
        _animator.SetBool(_animNames[(int)animTag], b);
    }

    /// <summary>
    /// 死亡処理。管理リストから削除し、ゲームオブジェクトを破棄します
    /// モーションのイベントフラグから呼び出します
    /// </summary>
    public override void Die()
    {
        base.Die();

        BattleManager.Instance.RemovePlayerFromList(this);
    }
}
