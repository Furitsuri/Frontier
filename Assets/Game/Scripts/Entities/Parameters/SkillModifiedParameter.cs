using System;

namespace Frontier.Entities
{
    /// <summary>
    /// 特定のスキル使用時のみに上乗せされるパラメータです
    /// </summary>
    [Serializable]
    public struct SkillModifiedParameter
    {
        public int AtkNum;
        public float AtkMagnification;
        public float DefMagnification;

        public void Init()
        {
            AtkNum              = 1;
            AtkMagnification    = 1f;
            DefMagnification    = 1f;
        }

        public void Reset()
        {
            Init();
        }

        public void SetStatusBitFlag( int stBitFlag )
        {
            // Methods.SetBitFlag<StatusEffect>(ref StatusBitFlag, statusBitFlag);
        }
    }
}