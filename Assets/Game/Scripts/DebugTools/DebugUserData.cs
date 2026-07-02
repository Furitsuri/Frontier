#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using Frontier.Combat;
using Frontier.Entities;
using Frontier.Loaders;
using static Constants;

namespace Frontier.DebugTools
{
    [Serializable]
    public class DebugUserData
    {
        public int money = 0;
        public int stageLevel = 0;

        /// <summary>
        /// true の場合、UserDomain.Members を members の内容で上書きします。
        /// false の場合、money / stageLevel のみ反映し、Members はそのままにします。
        /// </summary>
        public bool overrideMembers = false;

        public List<MemberEntry> members = new List<MemberEntry>();

        [Serializable]
        public class MemberEntry
        {
            public int    prefabIndex        = 0;
            public string name               = "Debug Player";
            public int    level              = 1;
            public int    maxHP              = 30;
            public int    atk               = 10;
            public int    def               = 5;
            public int    moveRange          = 4;
            public int    jumpForce          = 2;
            public int    attackRange        = 1;
            public int    maxActionGauge     = 100;
            public int    recoveryActionGauge = 10;

            /// <summary>
            /// スキルID（整数値）の配列。-1 は SkillID.NONE（未装備）を表します。
            /// </summary>
            public int[] equipSkills = new int[EQUIPABLE_SKILL_MAX_NUM];

            public BattleFileLoader.CharacterDeployData ToDeployData()
            {
                var skills = new int[EQUIPABLE_SKILL_MAX_NUM];
                for ( int i = 0; i < EQUIPABLE_SKILL_MAX_NUM; i++ )
                {
                    skills[i] = ( equipSkills != null && i < equipSkills.Length )
                        ? equipSkills[i]
                        : (int) SkillID.NONE;
                }

                return new BattleFileLoader.CharacterDeployData
                {
                    Prefab         = prefabIndex,
                    CharacterTag   = (int) CHARACTER_TAG.PLAYER,
                    CharacterIndex = 0,        // RecruitMember() で正しい値に上書きされる
                    Name           = name,
                    Level          = level,
                    MaxHP          = maxHP,
                    Atk            = atk,
                    Def            = def,
                    MoveRange      = moveRange,
                    JumpForce      = jumpForce,
                    AtkRange       = attackRange,
                    ActGaugeMax    = maxActionGauge,
                    ActRecovery    = recoveryActionGauge,
                    InitGridIndex  = 0,
                    InitDir        = (int) Direction.BACK,
                    ThinkType      = 0,
                    Skills         = skills,
                };
            }
        }
    }
}
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD
