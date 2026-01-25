using Frontier.Combat.Skill;
using Frontier.Stage;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Entities
{
    public class FieldLogicBase : MonoBehaviour
    {
        protected ReadOnlyReference<Character> _readOnlyOwner = null;

        /// <summary>
        /// 特に何もしませんが、アクティブ設定のチェックボックスをInspector上に出現させるために定義
        /// </summary>
        void Update()
        {

        }

        public void Regist( Character owner )
        {
            _readOnlyOwner = new ReadOnlyReference<Character>( owner );
        }

        virtual public void Setup()
        {
        }

        virtual public void Init()
        {
        }
    }
}