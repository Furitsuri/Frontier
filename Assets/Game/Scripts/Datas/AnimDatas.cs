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
        public enum AnimeConditionsTag
        {
            WAIT = 0,
            MOVE,
            SINGLE_ATTACK,
            DOUBLE_ATTACK,
            TRIPLE_ATTACK,
            GUARD,
            PARRY,
            GET_HIT,
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
            "Parry",
            "GetHit",
            "Die"
        };

        // �A�j���[�V�������n�b�V�����X�g
        public static List<int> AnimNameHashList;
        // �U���J�ڏI������ɗp���閼��
        public static string AtkEndStateName;

        /// <summary>
        /// ���������܂�
        /// </summary>
        public static void Init()
        {
            // �^�O�ƃA�j���[�V�����̐��͈�v���Ă��邱��
            Debug.Assert(ANIME_CONDITIONS_NAMES.Length == (int)AnimeConditionsTag.NUM);

            AtkEndStateName = ANIME_CONDITIONS_NAMES[(int)AnimeConditionsTag.SINGLE_ATTACK];

            // �n�b�V�����X�g��������
            AnimNameHashList = new List<int>();
            foreach( var elem in ANIME_CONDITIONS_NAMES )
            {
                AnimNameHashList.Add( Animator.StringToHash(elem) );
            }
        }
    }
}