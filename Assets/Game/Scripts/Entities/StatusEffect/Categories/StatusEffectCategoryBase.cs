using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier.Entities
{
    /// <summary>
    /// 
    /// </summary>
    abstract public class StatusEffectCategoryBase
    {
        [Inject] protected HierarchyBuilderBase _hierarchyBld = null;

        static protected int _additionalBitByCategory;                      // このカテゴリが状態異常IDの何ビット目から始まるか
        static protected Func<StatusEffectElementBase>[] _elementFactorys;  // 状態異常要素の生成関数群

        public StatusEffectCategoryBase()
        {
        }

        public StatusEffectElementBase CreateElement( int elementIndex )
        {
            if( _elementFactorys.Length <= elementIndex )
            {
                Debug.LogError( $"StatusEffectMoveCategory::CreateElement: Invalid elementIndex {elementIndex}" );
                return null;
            }

            return _elementFactorys[elementIndex]();
        }

        static public int GetAdditionalBitByCategory()
        {
            return _additionalBitByCategory;
        }
    }
}