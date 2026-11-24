using System;

namespace Frontier.Entities
{
    /// <summary>
    /// バフ・デバフなどで上乗せされるパラメータです
    /// </summary>
    [Serializable]
    public struct ModifiedParameter
    {
        // 攻撃力
        public int Atk;
        // 防御力
        public int Def;
        // 移動レンジ
        public int moveRange;
        // スキルゲージ回復値
        public int recoverySkillGauge;

        public void Init()
        {
            Atk = 0; Def = 0; moveRange = 0; recoverySkillGauge = 0;
        }

        public void Reset()
        {
            Init();
        }
    }
}