#if UNITY_EDITOR
using Frontier.Combat;
using Frontier.Entities;
using Frontier.Loaders;
using System;
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
        /// デフォルトステータスを反映した PLAYER 用 Status を生成して返します。
        /// 装備スキルはすべて SkillID.NONE（無し）です。
        /// </summary>
        public static Status CreatePlayerStatus()
        {
            var status = new Status();
            status.Setup();

            status.characterTag        = CHARACTER_TAG.PLAYER;
            status.characterIndex      = 0;                 // UserDomain.RecruitMember で正しい値へ上書きされる
            status.PrefabIndex         = Prefab;
            status.Name                = "Default";
            status.Level               = Level;
            status.CurHP               = status.MaxHP = MaxHP;
            status.Atk                 = Atk;
            status.Def                 = Def;
            status.moveRange           = MoveRange;
            status.jumpForce           = JumpForce;
            status.attackRange         = AtkRange;
            status.CurActionGauge      = status.maxActionGauge = Math.Min( ActGaugeMax, Constants.ACTION_GAUGE_MAX );
            status.recoveryActionGauge = ActRecovery;
            for( int i = 0; i < EQUIPABLE_SKILL_MAX_NUM; ++i ) { status.EquipSkills[i] = SkillID.NONE; }

            return status;
        }
    }
}
#endif // UNITY_EDITOR
