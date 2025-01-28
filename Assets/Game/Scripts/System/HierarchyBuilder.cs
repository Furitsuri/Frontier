using System.Collections;
using System.Collections.Generic;
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
    }

    /// <summary>
    /// オブジェクト・コンポーネント作成クラス
    /// </summary>
    public class HierarchyBuilder : MonoBehaviour
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

        // オブジェクト生成クラス
        Generator _generator = null;

        /// <summary>
        /// DiInstallerから呼び出し、コンテナを登録します
        /// </summary>
        /// <param name="container">DIコンテナ</param>
        [Inject]
        void Construct(DiContainer container, DiInstaller installer)
        {
            if (_generator == null)
            {
                _generator = gameObject.AddComponent<Generator>();
            }

            _generator.Inject(container, installer);
        }

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
        private GameObject MapObjectToType<T>( T original )
        {
            if (original == null)
            {
                Debug.LogWarning("Null object passed as argument");

                return null;
            }
            
            return original switch
            {
                Camera => _cameraObj,
                Player => _characterObjGrp._playerObj,
                Enemy => _characterObjGrp._enemyObj,
                Controller => _controllerObj,
                Manager => _managerObj,
                _ => this.gameObject
            };
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

        /// <summary>
        /// 生成されたオブジェクトを指定のオブジェクトの階層化に配置します
        /// </summary>
        /// <param name="bhv">生成されたオブジェクト</param>
        private void Organize(Behaviour bhv)
        {
            GameObject parentObj = MapObjectToType(bhv);

            if (parentObj != null)
            {
                bhv.transform.SetParent( parentObj.transform );
            }
        }

        /// <summary>
        /// オブジェクト及びコンポーネントを作成し、ヒエラルキー上の任意のオブジェクトの階層下に設置します
        /// </summary>
        /// <typeparam name="T">作成するコンポーネントの型</typeparam>
        /// <param name="initActive">作成したオブジェクトの初期の有効・無効状態</param>
        /// <returns>作成したコンポーネント</returns>
        public T CreateComponentAndOrganize<T>( bool initActive) where T : Behaviour
        {
            T generateCpt = _generator.GenerateObjectAndAddComponent<T>(initActive);
            Debug.Assert(generateCpt != null);

            Organize(generateCpt);

            return generateCpt;
        }

        /// <summary>
        /// 引数に渡したオブジェクトからコンポーネントを作成し、ヒエラルキー上の任意のオブジェクトの階層下に設置します
        /// </summary>
        /// <typeparam name="T">作成するコンポーネントの型</typeparam>
        /// <param name="gameObject">コンポーネントの元となるゲームオブジェクト</param>
        /// <param name="initActive">ゲームオブジェクトの初期の有効・無効状態</param>
        /// <returns>作成したコンポーネント</returns>
        public T CreateComponentAndOrganize<T>(GameObject gameObject, bool initActive) where T : Behaviour
        {
            T generateCpt = _generator.GenerateComponentFromObject<T>(gameObject, initActive);
            Debug.Assert(generateCpt != null);

            Organize(generateCpt);

            return generateCpt;
        }

        /// <summary>
        /// 引数に渡したオブジェクトからコンポーネントを作成します
        /// また、作成したコンポーネントを親とするオブジェクトの子として設定し、ヒエラルキー上の任意のオブジェクトの階層下に設置します
        /// </summary>
        /// <typeparam name="T">作成するコンポーネントの型</typeparam>
        /// <param name="gameObject">コンポーネントの元となるゲームオブジェクト</param>
        /// <param name="parentObject">ヒエラルキー上で作成したオブジェクトの親となるオブジェクト</param>
        /// <param name="initActive">ゲームオブジェクトの初期の有効・無効状態</param>
        /// <returns>作成したコンポーネント</returns>
        public T CreateComponentWithNestedParent<T>(GameObject gameObject, GameObject parentObject, bool initActive) where T : Behaviour
        {
            T generateCpt = _generator.GenerateComponentFromObject<T>(gameObject, initActive);
            Debug.Assert(generateCpt != null);

            generateCpt.transform.parent = parentObject.transform;

            return generateCpt;
        }

        /// <summary>
        /// 引数に渡したオブジェクトからコンポーネントを作成します
        /// また、作成したコンポーネントを親とするオブジェクトの子として設定し、更に指定の名前で作成したディレクトリの子としてその親を設定の上、
        /// ヒエラルキー上の任意のオブジェクトの階層下に設置します
        /// </summary>
        /// <typeparam name="T">作成するコンポーネントの型</typeparam>
        /// <param name="gameObject">コンポーネントの元となるゲームオブジェクト</param>
        /// <param name="parentObject">ヒエラルキー上で作成したオブジェクトの親となるオブジェクト</param>
        /// <param name="newDirectoryObjectName">作成するヒエラルキー上のオブジェクト(ディレクトリの代替となる空オブジェクト)の名前</param>
        /// <param name="initActive">ゲームオブジェクトの初期の有効・無効状態</param>
        /// <returns>作成したコンポーネント</returns>
        public T CreateComponentWithNestedNewDirectory<T>(GameObject gameObject, GameObject parentObject, string newDirectoryObjectName, bool initActive) where T : Behaviour
        {
            T generateCpt = _generator.GenerateComponentFromObject<T>(gameObject, initActive);
            Debug.Assert(generateCpt != null);

            GameObject folderObject = new GameObject(newDirectoryObjectName);
            folderObject.transform.parent = parentObject.transform;
            generateCpt.transform.parent = folderObject.transform;

            return generateCpt;
        }

        /// <summary>
        /// DIコンテナを用いてオブジェクト及びコンポーネントを作成し、ヒエラルキー上の任意のオブジェクトの階層下に設置します
        /// </summary>
        /// <typeparam name="T">作成するコンポーネントの型</typeparam>
        /// <param name="initActive">ゲームオブジェクトの初期の有効・無効状態</param>
        /// <returns>作成したコンポーネント</returns>
        public T CreateComponentAndOrganizeWithDiContainer<T>( bool initActive, bool isBind ) where T : Behaviour
        {
            GameObject gameObj = new GameObject();
            T generateCpt = _generator.InstantiateComponentWithDiContainer<T>(gameObject, initActive, isBind);
            Debug.Assert(generateCpt != null);

            Organize(generateCpt);

            return generateCpt;
        }

        /// <summary>
        /// 引数に渡したオブジェクトからDIコンテナを用いてコンポーネントを作成し、ヒエラルキー上の任意のオブジェクトの階層下に設置します
        /// </summary>
        /// <typeparam name="T">作成するコンポーネントの型</typeparam>
        /// <param name="gameObject">コンポーネントの元となるゲームオブジェクト</param>
        /// <param name="initActive">ゲームオブジェクトの初期の有効・無効状態</param>
        /// <returns>作成したコンポーネント</returns>
        public T CreateComponentAndOrganizeWithDiContainer<T>(GameObject gameObject, bool initActive, bool isBind) where T : Behaviour
        {
            T generateCpt = _generator.InstantiateComponentWithDiContainer<T>(gameObject, initActive, isBind);
            Debug.Assert(generateCpt != null);

            Organize(generateCpt);

            return generateCpt;
        }

        /// <summary>
        /// 引数に渡したオブジェクトからDIコンテナを用いてコンポーネントを作成します
        /// また、作成したコンポーネントを親とするオブジェクトの子として設定し、ヒエラルキー上の任意のオブジェクトの階層下に設置します
        /// </summary>
        /// <typeparam name="T">作成するコンポーネントの型</typeparam>
        /// <param name="gameObject">コンポーネントの元となるゲームオブジェクト</param>
        /// <param name="parentObject">ヒエラルキー上で作成したオブジェクトの親となるオブジェクト</param>
        /// <param name="initActive">ゲームオブジェクトの初期の有効・無効状態</param>
        /// <returns>作成したコンポーネント</returns>
        public T CreateComponentNestedParentWithDiContainer<T>(GameObject gameObject, GameObject parentObject, bool initActive, bool isBind) where T : Behaviour
        {
            T generateCpt = _generator.InstantiateComponentWithDiContainer<T>(gameObject, initActive, isBind);
            Debug.Assert(generateCpt != null);

            generateCpt.transform.parent = parentObject.transform;

            return generateCpt;
        }

        /// <summary>
        /// 引数に渡したオブジェクトからDIコンテナを用いてコンポーネントを作成します
        /// また、作成したコンポーネントを親とするオブジェクトの子として設定し、更に指定の名前で作成したディレクトリの子としてその親を設定の上、
        /// ヒエラルキー上の任意のオブジェクトの階層下に設置します
        /// </summary>
        /// <typeparam name="T">作成するコンポーネントの型</typeparam>
        /// <param name="gameObject">コンポーネントの元となるゲームオブジェクト</param>
        /// <param name="parentObject">ヒエラルキー上で作成したオブジェクトの親となるオブジェクト</param>
        /// <param name="newDirectoryObjectName">作成するヒエラルキー上のオブジェクト(ディレクトリの代替となる空オブジェクト)の名前</param>
        /// <param name="initActive">ゲームオブジェクトの初期の有効・無効状態</param>
        /// <returns>作成したコンポーネント</returns>
        public T CreateComponentNestedNewDirectoryWithDiContainer<T>(GameObject gameObject, GameObject parentObject, string newDirectoryObjectName, bool initActive, bool isBind) where T : Behaviour
        {
            T generateCpt = _generator.InstantiateComponentWithDiContainer<T>(gameObject, initActive, isBind);
            Debug.Assert(generateCpt != null);

            GameObject folderObject = new GameObject(newDirectoryObjectName);
            folderObject.transform.parent = parentObject.transform;
            generateCpt.transform.parent = folderObject.transform;

            return generateCpt;
        }

        /// <summary>
        /// Diコンテナを用いてインスタンスを作成します
        /// ヒエラルキー上には設置しません
        /// </summary>
        /// <typeparam name="T">作成するインスタンスの型</typeparam>
        /// <returns>作成したインスタンス</returns>
        public T InstantiateWithDiContainer<T>()
        {
            return _generator.InstantiateWithDiContainer<T>();
        }
    }
}