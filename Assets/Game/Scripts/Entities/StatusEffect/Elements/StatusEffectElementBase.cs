using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Entities
{
    abstract public class StatusEffectElementBase
    {
        protected int       _bitFlag                        = 0;    // この状態異常要素のビットフラグ
        protected int       _additionalBitFlagByCategory    = 0;    // 状態異常のカテゴリごとに追加されるビットフラグ
        protected Character _targetCharacter                = null; // 効果を適用するキャラクター

        public StatusEffectElementBase()
        {
        }

        public void Init( Character character )
        {
            _targetCharacter = character;
            _targetCharacter.StatusEffectBitFlag |= CalcurateStatusEffectBitFlag( _bitFlag, _additionalBitFlagByCategory );
        }

        abstract public void ApplyEffect();

        static public int CalcurateStatusEffectBitFlag( int additionalBitFlagByCategory, int elementIndex )
        {
            return 1 << elementIndex << additionalBitFlagByCategory;
        }
    }
}