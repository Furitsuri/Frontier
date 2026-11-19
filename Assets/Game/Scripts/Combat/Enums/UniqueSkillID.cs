namespace Frontier.Combat
{
    /// <summary>
    /// 固有スキル(キャラクターの種別ごとに1つ持つ常時効果のスキル)のIDです。
    /// パッシブスキル(PassiveSkillID)とは別物であり、装備・所持・売買の対象になりません。
    /// UniqueSkillData.jsonのUniqueSkillsData配列の並び順と対応させてください。
    /// </summary>
    public enum UniqueSkillID
    {
        NONE = -1,

        IRON_WILL,  // 鉄の意志(Dog Knight)
        KEEN_EYE,   // 鷹の目(Tiger Archer)
        SOFT_BODY,  // 軟体(Slime)
        HARD_SHELL, // 硬い甲羅(Turtle)

        NUM,
    }
}
