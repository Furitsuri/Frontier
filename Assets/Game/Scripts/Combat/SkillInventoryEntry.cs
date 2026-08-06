using System;

namespace Frontier.Combat
{
    /// <summary>
    /// UserDomainが所持するスキルの在庫1件分(スキルIDとその所持数)。
    /// JsonUtility/SerializeFieldはDictionaryを直接扱えないため、Listの要素として保持する。
    /// </summary>
    [Serializable]
    public struct SkillInventoryEntry
    {
        public SkillID SkillID;
        public int Count;
    }
}
