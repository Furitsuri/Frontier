using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : Character
{
    /// <summary>
    /// 思考タイプ
    /// </summary>
    public enum ThinkingType
    {
        NEAR = 0,   // 自分の距離に近い敵を優先

        NUM
    }

    private ThinkingType _thikType;
    public EMAIBase EmAI { get; private set; }

    /// <summary>
    /// 初期化します
    /// 思考タイプ設定のために、親クラスのInit関数はoverrideしていません
    /// </summary>
    /// <param name="type">思考タイプ</param>
    public void Init(ThinkingType type)
    {
        base.Init();

        _thikType = type;

        // 思考タイプによってemAIに代入する派生クラスを変更する
        switch (_thikType)
        {
            case ThinkingType.NEAR:
                EmAI = new EMAIAggressive();
                break;
            default:
                EmAI = new EMAIBase();
                break;
        }

        EmAI.Init(this);
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

        BattleManager.Instance.RemoveEnemyFromList(this);
    }

    /// <summary>
    /// 目的座標と標的キャラクターを決定する
    /// </summary>
    public (bool, bool) DetermineDestinationAndTargetWithAI()
    {
        return EmAI.DetermineDestinationAndTarget(param, tmpParam);
    }

    /// <summary>
    /// 目標座標と標的キャラクターを取得します
    /// </summary>
    public void FetchDestinationAndTarget(out int destinationIndex, out Character targetCharacter)
    {
        destinationIndex    = EmAI.GetDestinationGridIndex();
        targetCharacter     = EmAI.GetTargetCharacter();
    }
}