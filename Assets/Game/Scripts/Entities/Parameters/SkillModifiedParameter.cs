using System;

namespace Frontier.Entities
{
    /// <summary>
    /// 特定のスキル使用時のみに上乗せされるパラメータです
    /// </summary>
    [Serializable]
    public struct SkillModifiedParameter
    {
        public int AddAtkNum;
        public float AtkMagnification;
        public float DefMagnification;

        public void Init()
        {
            AddAtkNum           = 0;
            AtkMagnification    = 0f;
            DefMagnification    = 0f;
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