using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Player : Character
{
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