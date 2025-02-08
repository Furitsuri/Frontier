using Frontier;
using UnityEngine;
using Zenject;

public class Generator : MonoBehaviour
{
    private DiContainer _container;
    private DiInstaller _installer;

    /// <summary>
    /// DIコンテナのインスタンスに注入します
    /// </summary>
    /// <param name="container">DIコンテナ</param>
    public void Inject( DiContainer container, DiInstaller installer )
    {
        _container = container;
        _installer = installer;
    }

    /// <summary>
    /// オブジェクトを生成し、そのオブジェクトにコンポーネントを追加します
    /// </summary>
    /// <typeparam name="T">オブジェクトに追加するコンポーネント</typeparam>
    /// <param name="initActive">ゲームオブジェクトの初期の有効・無効状態</param>
    /// <returns>生成したコンポーネント</returns>
    public T GenerateObjectAndAddComponent<T>(bool initActive) where T : Behaviour
    {
        GameObject gameObj = new GameObject();

        T original = gameObj.AddComponent<T>();

        gameObj.SetActive(initActive);

        return original;
    }

    /// <summary>
    /// 渡されたゲームオブジェクトからコンポーネントを生成します
    /// </summary>
    /// <typeparam name="T">生成するBehaviorが継承された任意の型</typeparam>
    /// <param name="gameObject">渡すゲームオブジェクト</param>
    /// <param name="initActive">ゲームオブジェクトの初期の有効・無効状態</param>
    /// <returns>生成したコンポーネント</returns>
    public T GenerateComponentFromObject<T> ( GameObject gameObject, bool initActive ) where T : Behaviour
    {
        GameObject gameObj = Instantiate( gameObject );
        Debug.Assert(gameObj != null );

        T original = gameObj.GetComponent<T>();
        Debug.Assert( original != null );
        
        gameObj.SetActive(initActive);

        return original;
    }

    /// <summary>
    /// DIコンテナを用いて、指定のゲームオブジェクトをコンポーネントを付与する形で作成します
    /// </summary>
    /// <typeparam name="T">作成するコンポーネントの型</typeparam>
    /// <param name="initActive">ゲームオブジェクトの初期の有効・無効状態</param>
    /// <param name="isBind">DIコンテナにバインドするか否か</param>
    /// <returns>作成したコンポーネント</returns>
    public T InstantiateComponentWithDiContainer<T>(bool initActive, bool isBind) where T : Behaviour
    {
        GameObject gameObj = new GameObject();
        T original = _container.InstantiatePrefabForComponent<T>(gameObj.AddComponent<T>().gameObject);
        Debug.Assert(original != null);

        original.gameObject.SetActive(initActive);

        // Diコンテナにバインドする場合はここでバインド
        if (isBind)
        {
            _installer.InstallBindings(original);
        }

        return original;
    }

    /// <summary>
    /// DIコンテナを用いて、指定のゲームオブジェクトとコンポーネントを作成します
    /// </summary>
    /// <typeparam name="T">作成するコンポーネントの型</typeparam>
    /// <param name="gameObject">作成するオブジェクト</param>
    /// <param name="initActive">ゲームオブジェクトの初期の有効・無効状態</param>
    /// <param name="isBind">DIコンテナにバインドするか否か</param>
    /// <returns>作成したコンポーネント</returns>
    public T InstantiateComponentWithDiContainer<T>(GameObject gameObject, bool initActive, bool isBind) where T : Behaviour
    {
        T original = _container.InstantiatePrefabForComponent<T>(gameObject);
        Debug.Assert(original != null);

        original.gameObject.SetActive(initActive);

        // Diコンテナにバインドする場合はここでバインド
        if (isBind)
        {
            _installer.InstallBindings(original);
        }

        return original;
    }

    /// <summary>
    /// Diコンテナを用いて、インスタンスを作成します
    /// </summary>
    /// <typeparam name="T">作成するインスタンスの型</typeparam>
    /// <returns>作成したインスタンス</returns>
    public T InstantiateWithDiContainer<T>()
    {
        return _container.Instantiate<T>();
    }
}