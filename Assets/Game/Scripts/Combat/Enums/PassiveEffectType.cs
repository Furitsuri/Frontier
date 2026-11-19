namespace Frontier.Combat
{
    /// <summary>
    /// パッシブスキルの効果の種類です。パッシブスキル・固有スキルで共通です。効果量は強化段階ごとにPassiveEffect.GradeData.Valueで指定します。
    /// PassiveSkillData.jsonにはint値で記載するため、途中に挿入せず末尾に追加してください。
    /// </summary>
    public enum PassiveEffectType : int
    {
        NONE = -1,

        ATK_MAGNIFICATION,  // 攻撃力倍率の加算(Value: 加算する倍率。0.2で+20%)
        DEF_MAGNIFICATION,  // 防御力倍率の加算(Value: 加算する倍率。0.2で+20%)

        NUM,
    }
}
