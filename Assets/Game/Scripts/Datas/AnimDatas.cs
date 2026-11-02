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
            NONE = -1,

            WAIT = 0,
            MOVE,
            SINGLE_ATTACK,
            DOUBLE_ATTACK,
            TRIPLE_ATTACK,
            DASH_AND_JUMP_ATK_INITIAL,
            DASH_AND_JUMP_ATK_LATTER,
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
            "DashAndJumpAtk_Initial",
            "DashAndJumpAtk_Latter",
            "Guard",
            "Parry",
            "GetHit",
            "Die"
        };

        /// <summary>
        /// キャラクターのAnimatorに該当のアニメーションが存在しない場合に、代わりに用いるアニメーションのタグ。
        /// NONEの場合は代わりのアニメーションを再生せず、再生後すぐに終了したものとして扱います
        /// (スキル専用のアニメーションを持たないキャラクターがスキルを使用した際に、終了待ちで停止しないようにするため)
        /// </summary>
        static public readonly AnimeConditionsTag[] FALLBACK_TAGS =
        {
            AnimeConditionsTag.NONE,            // WAIT
            AnimeConditionsTag.NONE,            // MOVE
            AnimeConditionsTag.NONE,            // SINGLE_ATTACK
            AnimeConditionsTag.SINGLE_ATTACK,   // DOUBLE_ATTACK
            AnimeConditionsTag.SINGLE_ATTACK,   // TRIPLE_ATTACK
            AnimeConditionsTag.NONE,            // DASH_AND_JUMP_ATK_INITIAL (溜めに相当するアニメーションが無いため再生しない)
            AnimeConditionsTag.SINGLE_ATTACK,   // DASH_AND_JUMP_ATK_LATTER
            AnimeConditionsTag.NONE,            // GUARD
            AnimeConditionsTag.GUARD,           // PARRY
            AnimeConditionsTag.NONE,            // GET_HIT
            AnimeConditionsTag.NONE,            // DIE
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
            Debug.Assert(FALLBACK_TAGS.Length == (int)AnimeConditionsTag.NUM);

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