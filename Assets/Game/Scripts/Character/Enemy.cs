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

    public struct Plan
    {
        // 移動目標グリッドインデックス値
        int destGridIndex;
        // 攻撃目標ユニットインデックス値
        int targetCharaIndex;
    }

    public ThinkingType ThinkType { get; private set; }
    private EMAIBase emAI;

    // Start is called before the first frame update
    void Start()
    {
        // TODO : 試運転用にパラメータをセット。後ほど削除
        this.param.characterTag = CHARACTER_TAG.CHARACTER_ENEMY;
        this.param.characterIndex = 5;
        this.param.moveRange = 2;
        this.param.initGridIndex = this.tmpParam.gridIndex = 13;
        this.param.MaxHP = this.param.CurHP = 8;
        this.param.Atk = 3;
        this.param.Def = 2;
        this.param.initDir = Constants.Direction.BACK;
        this.param.UICameraLengthY = 0.8f;
        this.param.UICameraLengthZ = 1.4f;
        this.param.UICameraLookAtCorrectY = 0.45f;
        BattleManager.Instance.AddEnemyToList(this);

        // 思考タイプによってemAIに代入する派生クラスを変更する
        switch(ThinkType)
        {
            case ThinkingType.NEAR:
                emAI = new EMAINearTarget();
                break;
            default:
                emAI = new EMAIBase();
                break;
        }

        emAI.Init(this);
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
    /// 思考パターンを用いて目標とする対象と目標座標を決定します
    /// </summary>
    public void DetermineTargetIndexWithAI()
    {
        // 各グリッドに対する評価値を算出
        emAI.CreateEvaluationValues( param, tmpParam );
    }
}
