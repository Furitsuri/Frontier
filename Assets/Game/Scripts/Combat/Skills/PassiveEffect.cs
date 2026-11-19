using System;
using UnityEngine;

namespace Frontier.Combat
{
    /// <summary>
    /// 常時効果(装備中のパッシブスキル・固有スキルで共通)の表現です。効果の種類と、強化段階(Grade)ごとの効果量を持ちます。
    /// Grades[0]が未強化の値です。強化段階の決まり方(パッシブスキルは所持スキルの強化、固有スキルはレベル)は利用側で扱います。
    /// </summary>
    public struct PassiveEffect
    {
        /// <summary>
        /// 強化段階1つ分のデータです
        /// </summary>
        [Serializable]
        public struct GradeData
        {
            public float Value;     // 効果量(意味はEffectTypeによる)
        }

        public PassiveEffectType EffectType;    // 効果の種類
        public GradeData[] Grades;              // 強化段階ごとのデータ(Grades[0]が未強化)

        public PassiveEffect( PassiveEffectType effectType, GradeData[] grades )
        {
            EffectType  = effectType;
            Grades      = grades ?? Array.Empty<GradeData>();
        }

        /// <summary>
        /// 指定強化段階のデータを取得します。段階がデータの範囲を超える場合は、範囲内に丸めた段階のデータを返します。
        /// </summary>
        public GradeData GetGradeData( int grade )
        {
            if ( Grades == null || Grades.Length == 0 ) { return default; }

            return Grades[Mathf.Clamp( grade, 0, Grades.Length - 1 )];
        }

        /// <summary>
        /// 指定の種類の効果である場合、指定強化段階での倍率を取得します。
        /// </summary>
        /// <returns>指定の種類の効果であるか否か</returns>
        public bool TryGetMagnification( PassiveEffectType effectType, int grade, out float magnification )
        {
            magnification = 0f;
            if ( EffectType != effectType ) { return false; }

            magnification = GetGradeData( grade ).Value;
            return true;
        }
    }
}
