using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{

    static public class AnimDatas
    {
        /// <summary>
        /// アニメーションの各遷移条件タグ
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
        /// アニメーションの各遷移条件名
        /// </summary>
        static public readonly string[] ANIME_CONDITIONS_NAMES =
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

        // アニメーション名ハッシュリスト
        static public List<int> AnimNameHashList;
        // 攻撃遷移終了判定に用いる名称
        static public string AtkEndStateName;

        /// <summary>
        /// 初期化します
        /// </summary>
        static public void Init()
        {
            // タグとアニメーションの数は一致していること
            Debug.Assert(ANIME_CONDITIONS_NAMES.Length == (int)AnimeConditionsTag.NUM);

            AtkEndStateName = ANIME_CONDITIONS_NAMES[(int)AnimeConditionsTag.SINGLE_ATTACK];

            // ハッシュリストを初期化
            AnimNameHashList = new List<int>();
            foreach( var elem in ANIME_CONDITIONS_NAMES )
            {
                AnimNameHashList.Add( Animator.StringToHash(elem) );
            }
        }
    }
}