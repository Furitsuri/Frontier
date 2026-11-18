using System;

namespace Frontier.Combat
{
    /// <summary>
    /// UserDomainが所持するパッシブスキルの在庫1件分(スキルIDとその所持数)。
    /// ActiveSkillInventoryEntryのパッシブスキル版です。
    /// </summary>
    [Serializable]
    public struct PassiveSkillInventoryEntry
    {
        public PassiveSkillID SkillID;
        public int Count;
    }
}
