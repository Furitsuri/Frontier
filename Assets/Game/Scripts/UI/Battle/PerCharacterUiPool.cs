using Frontier.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Frontier.UI
{
    /// <summary>
    /// キャラクター1体につき1つずつ、テンプレートを複製して生成するUIを管理します。
    /// 「キャラクターごとに初回だけ複製し、以降は同じインスタンスを使い回し、最後にまとめて破棄する」という
    /// 頭上HPゲージ・ダメージ表記・連携演出の渦などに共通する処理をまとめたものです。
    /// </summary>
    /// <typeparam name="T">管理するUIの型</typeparam>
    public class PerCharacterUiPool<T> where T : Component
    {
        private readonly T _template;
        private readonly Transform _parent;
        private readonly Action<T> _onCreated;

        // キャラクターInstanceID → UI のマッピング
        private readonly Dictionary<int, T> _uiByCharaId = new Dictionary<int, T>();

        /// <summary>
        /// 生成済みのUIの一覧(破棄済みのものは含みません)
        /// </summary>
        public IEnumerable<T> Values
        {
            get
            {
                foreach( var ui in _uiByCharaId.Values )
                {
                    if( ui != null ) { yield return ui; }
                }
            }
        }

        /// <summary>
        /// 生成済みのUIを、対応するキャラクターのInstanceIDと組で列挙します(破棄済みのものは含みません)
        /// </summary>
        public IEnumerable<KeyValuePair<int, T>> Pairs
        {
            get
            {
                foreach( var pair in _uiByCharaId )
                {
                    if( pair.Value != null ) { yield return pair; }
                }
            }
        }

        /// <param name="template">複製元のテンプレート</param>
        /// <param name="parent">複製したUIを配置する親</param>
        /// <param name="onCreated">複製直後に行う初期化処理</param>
        public PerCharacterUiPool( T template, Transform parent, Action<T> onCreated )
        {
            _template  = template;
            _parent    = parent;
            _onCreated = onCreated;
        }

        /// <summary>
        /// キャラクターに対応するUIを返します。存在しない場合はテンプレートを複製して新規生成します。
        /// </summary>
        public T GetOrCreate( Character chara )
        {
            int id = chara.GetInstanceID();
            if( !_uiByCharaId.TryGetValue( id, out T ui ) || ui == null )
            {
                ui = Object.Instantiate( _template, _parent );
                _onCreated?.Invoke( ui );
                _uiByCharaId[id] = ui;
            }
            return ui;
        }

        /// <summary>
        /// キャラクターに対応する生成済みのUIを取得します
        /// </summary>
        public bool TryGet( Character chara, out T ui )
        {
            return _uiByCharaId.TryGetValue( chara.GetInstanceID(), out ui ) && ui != null;
        }

        /// <summary>
        /// キャラクターに対応するUIを破棄します
        /// </summary>
        public void Remove( Character chara )
        {
            int id = chara.GetInstanceID();
            if( _uiByCharaId.TryGetValue( id, out T ui ) )
            {
                if( ui != null ) { Object.Destroy( ui.gameObject ); }
                _uiByCharaId.Remove( id );
            }
        }

        /// <summary>
        /// 生成したすべてのUIを破棄し、管理辞書をクリアします
        /// </summary>
        public void Clear()
        {
            foreach( var ui in Values )
            {
                Object.Destroy( ui.gameObject );
            }
            _uiByCharaId.Clear();
        }
    }
}
