using Frontier.Combat;

namespace Frontier.Entities
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
        // 現在位置を示すタイルインデックス
        public int gridIndex;
        // 1回の攻撃におけるHPの予測変動量(複数回攻撃におけるダメージ総量を考慮しない)
        public int expectedHpChange;
        // 全ての攻撃におけるHPの予測総変動量(複数回攻撃におけるダメージ総量を考慮する)
        public int totalExpectedHpChange;

        public void Awake()
        {
            isEndCommand    = new bool[(int)Command.COMMAND_TAG.NUM];
            isUseSkills     = new bool[Constants.EQUIPABLE_SKILL_MAX_NUM];
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        public void Init()
        {
            for (int i = 0; i < (int)Command.COMMAND_TAG.NUM; ++i)
            {
                isEndCommand[i] = false;
            }

            totalExpectedHpChange = expectedHpChange = 0;
        }

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
            Init();
        }

        /// <summary>
        /// ダメージを受けた際のHPの予測変動量を取得します
        /// </summary>
        /// <param name="single">単発攻撃の予測変動量</param>
        /// <param name="total">複数回攻撃の予測総変動量</param>
        public void AssignExpectedHpChange(out int single, out int total)
        {
            single  = expectedHpChange;
            total   = totalExpectedHpChange;
        }

        /// <summary>
        /// 戦闘などにおけるHPの予測変動量を設定します
        /// </summary>
        /// <param name="single">単発攻撃における予測変動量</param>
        /// <param name="total">複数回攻撃における予測総変動量</param>
        public void SetExpectedHpChange(int single, int total)
        {
            expectedHpChange        = single;
            totalExpectedHpChange   = total;
        }

        /// <summary>
        /// 現在地点(キャラクターが移動中ではない状態の)のグリッドのインデックス値を設定します
        /// </summary>
        /// <param name="index">設定するインデックス値</param>
        public void SetCurrentGridIndex(int index)
        {
            gridIndex = index;
        }

        /// <summary>
        /// 各終了コマンドの状態を設定します
        /// </summary>
        /// <param name="isEnd">設定する終了状態のOnまたはOff</param>
        /// <param name="cmdTag">設定対象のコマンドタグ</param>
        public void SetEndCommandStatus(Command.COMMAND_TAG cmdTag, bool isEnd)
        {
            isEndCommand[(int)cmdTag] = isEnd;
        }

        /// <summary>
        /// 行動を終了させます
        /// </summary>
        public void EndAction()
        {
            for (int i = 0; i < (int)Command.COMMAND_TAG.NUM; ++i)
            {
                SetEndCommandStatus((Command.COMMAND_TAG)i, true);
            }
        }

        /// <summary>
        /// 現在地点(キャラクターが移動中ではない状態の)のグリッドのインデックス値を返します
        /// </summary>
        /// <returns>現在グリッドのインデックス値</returns>
        public int GetCurrentGridIndex()
        {
            return gridIndex;
        }

        /// <summary>
        /// 指定のコマンドが終了しているかを取得します
        /// </summary>
        /// <param name="cmdTag">指定するコマンドタグ</param>
        /// <returns>指定のコマンドが終了しているか否か</returns>
        public bool IsEndCommand(Command.COMMAND_TAG cmdTag)
        {
            return isEndCommand[(int)cmdTag];
        }

        /// <summary>
        /// 現在のターンにおける行動が終了しているかを取得します
        /// MEMO : 仕様上、待機コマンドが終了していれば、行動全てを終了していると判定しています
        /// </summary>
        /// <returns>行動が終了しているか</returns>
        public bool IsEndAction()
        {
            return IsEndCommand(Command.COMMAND_TAG.WAIT);
        }
    }
}