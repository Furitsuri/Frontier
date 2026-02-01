using Frontier.Entities;
using System;
using UnityEngine;
using Zenject;

namespace Frontier
{
    [System.Serializable]
    public class CharacterGroup
    {
        [SerializeField]
        [Header("プレイヤー")]
        public GameObject _playerObj;

        [SerializeField]
        [Header("エネミー")]
        public GameObject _enemyObj;

        [SerializeField]
        [Header("第三勢力")]
        public GameObject _otherObj;
    }

    /// <summary>
    /// オブジェクト・コンポーネント作成クラス
    /// </summary>
    public class HierarchyBuilder : HierarchyBuilderBase
    {
        [SerializeField]
        [Header("カメラオブジェクト")]
        private GameObject _cameraObj;

        [SerializeField]
        [Header("キャラクターオブジェクト")]
        private CharacterGroup _characterObjGrp;

        [SerializeField]
        [Header("コントローラオブジェクト")]
        private GameObject _controllerObj;

        [SerializeField]
        [Header("マネージャーオブジェクト")]
        private GameObject _managerObj;

        void Awake()
        {
            if (_generator == null)
            {
                _generator = gameObject.AddComponent<Generator>();
            }

            Debug.Assert(
                _cameraObj != null ||
                _characterObjGrp._playerObj != null ||
                _characterObjGrp._enemyObj != null ||
                _controllerObj != null ||
                _managerObj != null,
                "Required object reference is missing.");
        }

        /// <summary>
        /// 引数に指定したビヘイビアの紐づけ先のオブジェクトを決定します
        /// </summary>
        /// <param name="original">紐づけを行う対象オブジェクト</param>
        /// <returns>紐づけ先のオブジェクト</returns>
        protected override GameObject MapObjectToType<T>( T original )
        {
            if (original == null)
            {
                Debug.LogWarning("Null object passed as argument");

                return null;
            }

            // 名前空間の末尾部分の文字列で判定を行う
            var ns = original.GetType().Namespace;
            string[] parts = ns != null ? ns.Split('.') : Array.Empty<string>();
            return 1 <= parts.Length ? parts[parts.Length - 1] switch
            {
                "Camera" => _cameraObj,
                // キャラクター名前空間のクラスは派生クラス毎に分類する
                "Entities" => original switch
                {
                    Other => _characterObjGrp._otherObj,
                    Player => _characterObjGrp._playerObj,
                    Enemy => _characterObjGrp._enemyObj,
                    _ => this.gameObject
                },
                "Controller" => _controllerObj,
                "Manager" => _managerObj,
                _ => this.gameObject
            } : this.gameObject;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private object HandlePlayer()
        {
            // Player型に対する処理
            Debug.Log("Handling Player type");
            return "Player-specific value";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="enemy"></param>
        /// <returns></returns>
        private object HandleEnemy(Enemy enemy)
        {
            // Enemy型に対する処理
            Debug.Log("Handling Enemy type");
            return "Enemy-specific value";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private object HandleDefault()
        {
            // その他のGameObject型に対する処理
            Debug.Log("Handling default GameObject type");
            return "Default value";
        }
    }
}