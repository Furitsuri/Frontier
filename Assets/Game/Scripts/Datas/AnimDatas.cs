using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{

    static public class AnimDatas
    {
        /// <summary>
        /// �A�j���[�V�����̊e�J�ڏ����^�O
        /// </summary>
        public enum ANIME_CONDITIONS_TAG
        {
            WAIT = 0,
            MOVE,
            SINGLE_ATTACK,
            DOUBLE_ATTACK,
            TRIPLE_ATTACK,
            GUARD,
            DAMAGED,
            DIE,

            NUM,
        }

        /// <summary>
        /// �A�j���[�V�����̊e�J�ڏ�����
        /// </summary>
        public static readonly string[] ANIME_CONDITIONS_NAMES =
        {
            "Wait",
            "Run",
            "SingleAttack",
            "DoubleAttack",
            "TripleAttack",
            "Guard",
            "GetHit",
            "Die"
        };

        public const string END_ATTACK_ANIME_STATE_NAME = "SingleAttack";
    }
}