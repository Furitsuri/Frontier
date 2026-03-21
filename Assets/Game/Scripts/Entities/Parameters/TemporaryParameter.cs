using Frontier.Combat;
using static Constants;

namespace Frontier.Entities
{
    /// <summary>
    /// 戦闘中のみ一時的に使用するパラメータです
    /// </summary>
    public struct TemporaryParameter
    {
        // 該当コマンドの終了フラグ
        public bool[] IsEndCommand;
        // 該当スキルの切替フラグ
        public bool[] IsSkillsToggledON;
        // 該当スキルの使用フラグ
        public bool[] IsSkillsUsed;
        // 該当スキルの使用可否フラグ
        public bool[] IsUseableSkill;
        // 現在位置を示すタイルインデックス
        public int CurrentTileIndex;
        // 1回の攻撃におけるHPの予測変動量(複数回攻撃におけるダメージ総量を考慮しない)
        public int ExpectedHpChange;
        // 全ての攻撃におけるHPの予測総変動量(複数回攻撃におけるダメージ総量を考慮する)
        public int TotalExpectedHpChange;

        public void Setup()
        {
            IsEndCommand        = new bool[( int ) COMMAND_TAG.NUM];
            IsSkillsToggledON    = new bool[EQUIPABLE_SKILL_MAX_NUM];
            IsSkillsUsed        = new bool[EQUIPABLE_SKILL_MAX_NUM];
            IsUseableSkill      = new bool[EQUIPABLE_SKILL_MAX_NUM];
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        public void Init()
        {
            for( int i = 0; i < ( int ) COMMAND_TAG.NUM; ++i )
            {
                IsEndCommand[i] = false;
            }

            for( int i = 0; i < EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                IsSkillsToggledON[i]     = false;
                IsSkillsUsed[i]         = false;
                IsUseableSkill[i]       = false;
            }

            TotalExpectedHpChange = ExpectedHpChange = 0;
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

            copy.IsEndCommand       = ( bool[] ) IsEndCommand.Clone();
            copy.IsSkillsToggledON   = ( bool[] ) IsSkillsToggledON.Clone();
            copy.IsSkillsUsed       = ( bool[] ) IsSkillsUsed.Clone();
            copy.IsUseableSkill     = ( bool[] ) IsUseableSkill.Clone();

            return copy;
        }

        /// <summary>
        /// 全てのパラメータをリセットします
        /// </summary>
        public void Reset()
        {
            Init();
        }

        public void ResetSkillsToggledOn()
        {
            for( int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                IsSkillsToggledON[i] = false;
            }
        }

        public void ResetSkillsUsed()
        {
            for( int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                IsSkillsUsed[i] = false;
            }
        }

        /// <summary>
        /// ダメージを受けた際のHPの予測変動量を取得します
        /// </summary>
        /// <param name="single">単発攻撃の予測変動量</param>
        /// <param name="total">複数回攻撃の予測総変動量</param>
        public void AssignExpectedHpChange( out int single, out int total )
        {
            single  = ExpectedHpChange;
            total   = TotalExpectedHpChange;
        }

        /// <summary>
        /// 戦闘などにおけるHPの予測変動量を設定します
        /// </summary>
        /// <param name="single">単発攻撃における予測変動量</param>
        /// <param name="total">複数回攻撃における予測総変動量</param>
        public void SetExpectedHpChange( int single, int total )
        {
            ExpectedHpChange        = single;
            TotalExpectedHpChange   = total;
        }

        /// <summary>
        /// 各終了コマンドの状態を設定します
        /// </summary>
        /// <param name="isEnd">設定する終了状態のOnまたはOff</param>
        /// <param name="cmdTag">設定対象のコマンドタグ</param>
        public void SetEndCommandStatus( COMMAND_TAG cmdTag, bool isEnd )
        {
            IsEndCommand[( int ) cmdTag] = isEnd;
        }

        public void SetUseableSkillFlag( int index, bool isUseable )
        {
            IsUseableSkill[( int ) index] = isUseable;
        }

        /// <summary>
        /// 行動を終了させます
        /// </summary>
        public void EndAction()
        {
            for( int i = 0; i < ( int ) COMMAND_TAG.NUM; ++i )
            {
                SetEndCommandStatus( ( COMMAND_TAG ) i, true );
            }
        }

        /// <summary>
        /// 現在のターンにおける行動が終了しているかを取得します
        /// MEMO : 仕様上、待機コマンドが終了していれば、行動全てを終了していると判定しています
        /// </summary>
        /// <returns>行動が終了しているか</returns>
        public bool IsEndAction()
        {
            return IsEndCommand[ ( int ) COMMAND_TAG.WAIT ];
        }
    }
}