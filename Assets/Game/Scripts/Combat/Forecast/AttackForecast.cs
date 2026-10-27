using Frontier.Entities;
using System;
using System.Collections.Generic;

namespace Frontier.Combat
{
    /// <summary>
    /// 指定された攻撃を行った場合に得られる結果の予測です。
    /// 誰が計算しても同じ値となる事実のみを保持し、ダメージ割合や重み付けなどの評価は利用する側(AI等)で行ってください。
    /// </summary>
    public struct AttackForecast
    {
        public int SingleDamage;        // 単発の与ダメージ(0以上)
        public int TotalDamage;         // 攻撃回数を加味した合計の与ダメージ(0以上)
        public int TargetCurrentHp;     // 攻撃前の対象の残りHP
        public bool IsDefeat;           // 対象を撃破出来るか
        public int ActionGaugeCost;     // 攻撃時に消費するアクションゲージ
        // 攻撃によって自身に付与される状態効果
        // MEMO : スキル使用の反動等で自身に状態効果が付与される仕組みは未実装のため、現在は常に空です
        public IReadOnlyList<StatusEffect> SelfStatusEffects;

        public static readonly IReadOnlyList<StatusEffect> EmptyStatusEffects = Array.Empty<StatusEffect>();
    }
}
