namespace Frontier.Combat
{
    /// <summary>
    /// バフ・デバフなどで上乗せされるパラメータです
    /// </summary>
    public struct ModifiedParameter
    {
        // 攻撃力
        public int Atk;
        // 防御力
        public int Def;
        // 移動レンジ
        public int moveRange;
        // アクションゲージ回復値
        public int recoveryActionGauge;

        public void Reset()
        {
            Atk = 0; Def = 0; moveRange = 0; recoveryActionGauge = 0;
        }
    }
}