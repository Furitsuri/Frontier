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

            public Status ToStatus()
            {
                var status = new Status();
                status.Setup();

                status.characterTag        = CHARACTER_TAG.PLAYER;
                status.characterIndex      = 0;        // RecruitMember() で正しい値に上書きされる
                status.PrefabIndex         = prefabIndex;
                status.Name                = name;
                status.Level                = level;
                status.CurHP                = status.MaxHP = maxHP;
                status.Atk                  = atk;
                status.Def                  = def;
                status.moveRange            = moveRange;
                status.jumpForce            = jumpForce;
                status.attackRange          = attackRange;
                status.CurActionGauge       = status.maxActionGauge = maxActionGauge;
                status.recoveryActionGauge  = recoveryActionGauge;

                for ( int i = 0; i < EQUIPABLE_SKILL_MAX_NUM; i++ )
                {
                    status.EquipSkills[i] = ( equipSkills != null && i < equipSkills.Length )
                        ? (SkillID) equipSkills[i]
                        : SkillID.NONE;
                }

                return status;
            }
        }
    }
}
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD
