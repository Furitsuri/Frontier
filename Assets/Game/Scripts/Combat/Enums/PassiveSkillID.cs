namespace Frontier.Combat
{
    /// <summary>
    /// パッシブスキル(装備しているだけで常時効果を持つスキル)のIDです。
    /// IDはスキルの「系統」を表し、強化段階(攻撃力アップ/攻撃力アップ+等)はIDとは別の値で表現します
    /// (強化版ごとに別のIDを設けないこと。同一系統のスキルを1人が複数装備出来ない判定をIDの比較で行うため)。
    /// PassiveSkillData.jsonの配列の並び順と対応させてください。
    /// </summary>
    public enum PassiveSkillID
    {
        NONE = -1,

        ATK_UP,     // 攻撃力アップ
        DEF_UP,     // 防御力アップ

        NUM,
    }
}
