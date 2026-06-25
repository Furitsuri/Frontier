using Frontier.Combat;
using Frontier.Entities;
using Frontier.Loaders;
using static Constants;

namespace Frontier
{
    /// <summary>
    /// 雇用を一切行わずに戦闘へ遷移したケースの保険として割り当てる、
    /// デフォルトのプレイヤーキャラクターステータス定義です。
    /// 値を調整したい場合はここを編集してください。装備スキルは持ちません。
    /// </summary>
    public static class DefaultCharacterStatus
    {
        public const int Prefab      = 0;    // 先頭のプレイヤープレハブを使用
        public const int Level       = 1;
        public const int MaxHP       = 30;
        public const int Atk         = 10;
        public const int Def         = 5;
        public const int MoveRange   = 4;
        public const int JumpForce   = 2;
        public const int AtkRange    = 1;
        public const int ActGaugeMax = 100;
        public const int ActRecovery = 10;

        /// <summary>
        /// デフォルトステータスを反映した PLAYER 用 CharacterDeployData を生成して返します。
        /// 装備スキルはすべて SkillID.NONE（無し）です。
        /// </summary>
        public static BattleFileLoader.CharacterDeployData CreatePlayerDeployData()
        {
            var skills = new int[EQUIPABLE_SKILL_MAX_NUM];
            for( int i = 0; i < skills.Length; ++i ) { skills[i] = ( int ) SkillID.NONE; }

            return new BattleFileLoader.CharacterDeployData
            {
                Prefab        = Prefab,
                CharacterTag  = ( int ) CHARACTER_TAG.PLAYER,
                CharacterIndex= 0,                          // UserDomain.RecruitMember で正しい値へ上書きされる
                Name          = "Default",
                Level         = Level,
                MaxHP         = MaxHP,
                Atk           = Atk,
                Def           = Def,
                MoveRange     = MoveRange,
                JumpForce     = JumpForce,
                AtkRange      = AtkRange,
                ActGaugeMax   = ActGaugeMax,
                ActRecovery   = ActRecovery,
                InitGridIndex = 0,
                InitDir       = ( int ) Direction.BACK,
                ThinkType     = 0,                          // PLAYER では未使用
                Skills        = skills,
            };
        }
    }
}
