using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Frontier.Character;

namespace Frontier
{

    /// <summary>
    /// �e�X�L���̎��s���e�̊֐��W���ł�
    /// </summary>
    public static class SkillsData
    {
        public enum ID
        {
            SKILL_NONE = -1,

            SKILL_PARRY,
            SKILL_GUARD,
            SKILL_COUNTER,
            SKILL_DOUBLE_STRIKE,
            SKILL_TRIPLE_STRIKE,

            SKILL_NUM,
        }

        public enum SituationType
        {
            ATTACK = 0,
            DEFENCE,
            PASSIVE,

            TYPE_NUM,
        }

        [System.Serializable]
        public struct Data
        {
            public string Name;
            public int Cost;
            public SituationType Type;
            public int Duration;
            public float AddAtkMag;
            public float AddDefMag;
            public int AddAtkNum;
            public float Param1;
            public float Param2;
            public float Param3;
            public float Param4;
        }

        public static Data[] data = new Data[(int)ID.SKILL_NUM];

        /// <summary>
        /// �K�[�h�X�L���̎��s���e�ł�
        /// </summary>
        /// <param name="modifiedParam">�X�L���g�p�L�����̃o�t�E�f�o�t�p�p�����[�^</param>
        /// <param name="param">�X�L���g�p�L�����̃p�����[�^</param>
        public static void ExecGuard(ref ModifiedParameter modifiedParam, ref Parameter param)
        {
            modifiedParam.Def = (int)Mathf.Floor(param.Def * 0.5f);
            param.consumptionActionGauge += 1;
        }
    }
}