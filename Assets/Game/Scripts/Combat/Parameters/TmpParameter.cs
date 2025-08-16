namespace Frontier.Combat
{
    /// <summary>
    /// 戦闘中のみ一時的に使用するパラメータです
    /// </summary>
    public struct TemporaryParameter
    {
        // 該当コマンドの終了フラグ
        public bool[] isEndCommand;
        // 該当スキルの使用フラグ
        public bool[] isUseSkills;
        // 現在位置を示すグリッドインデックス
        public int gridIndex;
        // 1回の攻撃におけるHPの予測変動量(複数回攻撃におけるダメージ総量を考慮しない)
        public int expectedHpChange;
        // 全ての攻撃におけるHPの予測総変動量(複数回攻撃におけるダメージ総量を考慮する)
        public int totalExpectedHpChange;

        /// <summary>
        /// 現在の値をクローンして返します
        /// 巻き戻しの際にバックアップを取りますが、C#の仕様上、配列部分が参照で渡されてしまうため、
        /// Array.Copyで値渡しになるように書き換えています
        /// </summary>
        /// <returns>クローンした変数</returns>
        public TemporaryParameter Clone()
        {
            TemporaryParameter copy = this;

            copy.isEndCommand = (bool[])isEndCommand.Clone();
            copy.isUseSkills = (bool[])isUseSkills.Clone();

            return copy;
        }

        /// <summary>
        /// スキルの使用フラグをリセットします
        /// </summary>
        public void ResetUseSkill()
        {
            for (int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i)
            {
                isUseSkills[i] = false;
            }
        }

        /// <summary>
        /// 全てのパラメータをリセットします
        /// </summary>
        public void Reset()
        {
            for (int i = 0; i < (int)Command.COMMAND_TAG.NUM; ++i)
            {
                isEndCommand[i] = false;
            }

            totalExpectedHpChange = expectedHpChange = 0;
        }
    }
}