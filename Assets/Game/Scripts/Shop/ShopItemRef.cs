using System;
using Frontier.Combat;

namespace Frontier.Shop
{
    /// <summary>
    /// ショップで販売する対象を指す値です。カテゴリ(ShopItemCategory)+カテゴリ内IDの組で表現することで、
    /// 将来スキル以外の販売対象(装備品・消耗品等)が増えてもShopHandlerの公開APIを変更せずに済みます。
    /// </summary>
    [Serializable]
    public struct ShopItemRef : IEquatable<ShopItemRef>
    {
        public ShopItemCategory Category;
        public int              RawId;

        public static ShopItemRef FromActiveSkill( ActiveSkillID skillID )
            => new ShopItemRef { Category = ShopItemCategory.ActiveSkill, RawId = ( int ) skillID };

        public static ShopItemRef FromPassiveSkill( PassiveSkillID skillID )
            => new ShopItemRef { Category = ShopItemCategory.PassiveSkill, RawId = ( int ) skillID };

        /// <summary>Category が ActiveSkill の場合のみ有効です。</summary>
        public ActiveSkillID AsActiveSkillID => ( ActiveSkillID ) RawId;

        /// <summary>Category が PassiveSkill の場合のみ有効です。</summary>
        public PassiveSkillID AsPassiveSkillID => ( PassiveSkillID ) RawId;

        public bool Equals( ShopItemRef other ) => Category == other.Category && RawId == other.RawId;
        public override bool Equals( object obj ) => obj is ShopItemRef other && Equals( other );
        public override int GetHashCode() => HashCode.Combine( Category, RawId );
    }
}
