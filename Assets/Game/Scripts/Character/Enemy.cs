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
        AGGERESSIVE = 0,    // 積極的に移動し、攻撃後の結果の評価値が高い対象を狙う
        WAITING,            // 自身の行動範囲に対象が入ってこない限り動かない。動き始めた後はAGGRESSIVEと同じ動作

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
            case ThinkingType.AGGERESSIVE:
                EmAI = new EMAIAggressive();
                break;
            case ThinkingType .WAITING:
                // TODO : Waitタイプを作成次第追加
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