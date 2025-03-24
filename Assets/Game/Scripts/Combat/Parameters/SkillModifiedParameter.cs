namespace Frontier.Combat
{
    /// <summary>
    /// 特定のスキル使用時のみに上乗せされるパラメータです
    /// </summary>
    public struct SkillModifiedParameter
    {
        public int AtkNum;
        public float AtkMagnification;
        public float DefMagnification;
    
        public void Reset()
        {
            AtkNum = 1; AtkMagnification = 1f; DefMagnification = 1f;
        }
    }
}