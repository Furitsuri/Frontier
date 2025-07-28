using Frontier.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class HierarchyBuilderBase : MonoBehaviour
{
    // オブジェクト生成クラス
    protected Generator _generator = null;

    /// <summary>
    /// DiInstallerから呼び出し、コンテナを登録します
    /// </summary>
    /// <param name="container">DIコンテナ</param>
    [Inject]
    void Construct(DiContainer container, IInstaller installer)
    {
        if (_generator == null)
        {
            _generator = gameObject.AddComponent<Generator>();
        }

        _generator.Inject(container, installer);
    }

    /// <summary>
    /// オブジェクト及びコンポーネントを作成し、ヒエラルキー上の任意のオブジェクトの階層下に設置します
    /// </summary>
    /// <typeparam name="T">作成するコンポーネントの型</typeparam>
    /// <param name="initActive">作成したオブジェクトの初期の有効・無効状態</param>
    /// <returns>作成したコンポーネント</returns>
    public T CreateComponentAndOrganize<T>(bool initActive, string objName) where T : Behaviour
    {
        T generateCpt = _generator.GenerateObjectAndAddComponent<T>(initActive, objName);
        NullCheck.AssertNotNull(generateCpt);

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
        NullCheck.AssertNotNull(generateCpt);

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
        NullCheck.AssertNotNull(generateCpt);

        generateCpt.transform.SetParent(parentObject.transform, false);

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
        NullCheck.AssertNotNull(generateCpt);

        GameObject folderObject = new GameObject(newDirectoryObjectName);
        folderObject.transform.SetParent(parentObject.transform, false);
        generateCpt.transform.SetParent(folderObject.transform, false);

        return generateCpt;
    }

    /// <summary>
    /// DIコンテナを用いてオブジェクト及びコンポーネントを作成し、ヒエラルキー上の任意のオブジェクトの階層下に設置します
    /// </summary>
    /// <typeparam name="T">作成するコンポーネントの型</typeparam>
    /// <param name="initActive">ゲームオブジェクトの初期の有効・無効状態</param>
    /// <returns>作成したコンポーネント</returns>
    public T CreateComponentAndOrganizeWithDiContainer<T>(bool initActive, bool isBind, string objectName) where T : Behaviour
    {
        T generateCpt = _generator.InstantiateComponentWithDiContainerOnNewObj<T>(initActive, isBind, objectName);
        NullCheck.AssertNotNull(generateCpt);

        Organize(generateCpt);

        return generateCpt;
    }

    /// <summary>
    /// DIコンテナを用いてオブジェクト及びコンポーネントを作成し、ヒエラルキー上の任意のオブジェクトの階層下に設置します
    /// </summary>
    /// <typeparam name="T">作成するコンポーネントの型</typeparam>
    /// <param name="initActive">ゲームオブジェクトの初期の有効・無効状態</param>
    /// <returns>作成したコンポーネント</returns>
    public T CreateAndAddComponentAndOrganizeWithDiContainer<T>(bool initActive, bool isBind, string objectName) where T : Behaviour
    {
        T generateCpt = _generator.InstantiateAndAddComponentWithDiContainer<T>(initActive, isBind, objectName);
        NullCheck.AssertNotNull(generateCpt);

        Organize(generateCpt);

        return generateCpt;
    }

    /// <summary>
    /// 引数に渡したオブジェクトからDIコンテナを用いてコンポーネントを作成し、ヒエラルキー上の任意のオブジェクトの階層下に設置します
    /// </summary>
    /// <typeparam name="T">作成するコンポーネントの型</typeparam>
    /// <param name="gameObject">コンポーネントの元となるゲームオブジェクト</param>
    /// <param name="initActive">ゲームオブジェクトの初期の有効・無効状態</param>
    /// <param name="isBind">DIコンテナにバインドするか否か</param>
    /// <returns>作成したコンポーネント</returns>
    public T CreateComponentAndOrganizeWithDiContainer<T>(GameObject gameObject, bool initActive, bool isBind, string objectName) where T : Behaviour
    {
        gameObject.name = objectName;
        T generateCpt = _generator.InstantiateComponentWithDiContainer<T>(gameObject, initActive, isBind);
        NullCheck.AssertNotNull(generateCpt);

        Organize(generateCpt);

        return generateCpt;
    }

    /// <summary>
    /// DIコンテナを用いてオブジェクト及びコンポーネントを作成し、ヒエラルキー上の任意のオブジェクトの階層下に設置します
    /// </summary>
    /// <typeparam name="T">作成するコンポーネントの型</typeparam>
    /// <param name="initActive">ゲームオブジェクトの初期の有効・無効状態</param>
    /// <returns>作成したコンポーネント</returns>
    public T CreateComponentNestedParentWithDiContainer<T>(GameObject parentObject, bool initActive, bool isBind, string objectName) where T : Behaviour
    {
        T generateCpt = _generator.InstantiateComponentWithDiContainerOnNewObj<T>(initActive, isBind, objectName);
        NullCheck.AssertNotNull(generateCpt);

        generateCpt.transform.SetParent(parentObject.transform, false);

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
    /// <param name="isBind">DIコンテナにバインドするか否か</param>
    /// <returns>作成したコンポーネント</returns>
    public T CreateComponentNestedParentWithDiContainer<T>(GameObject gameObject, GameObject parentObject, bool initActive, bool isBind, string objectName) where T : Behaviour
    {
        gameObject.name = objectName;
        T generateCpt = _generator.InstantiateComponentWithDiContainer<T>(gameObject, initActive, isBind);
        NullCheck.AssertNotNull(generateCpt);

        generateCpt.transform.SetParent(parentObject.transform, false);

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
        NullCheck.AssertNotNull(generateCpt);

        GameObject folderObject = new GameObject(newDirectoryObjectName);
        folderObject.transform.SetParent(parentObject.transform, false);
        generateCpt.transform.SetParent(folderObject.transform, false);

        return generateCpt;
    }

    /// <summary>
    /// Diコンテナを用いてインスタンスを作成します
    /// ヒエラルキー上には設置しません
    /// </summary>
    /// <typeparam name="T">作成するインスタンスの型</typeparam>
    /// <param name="isBind">DIコンテナにバインドするか否か</param>
    /// <returns>作成したインスタンス</returns>
    public T InstantiateWithDiContainer<T>( bool isBind )
    {
        return _generator.InstantiateWithDiContainer<T>(isBind);
    }

    public T InstantiateWithDiContainer<T>(T original, Vector3 position, Quaternion rotation, bool isBind) where T : UnityEngine.Object
    {
        return _generator.InstantiateWithDiContainer(original, position, rotation, isBind);
    }

    /// <summary>
    /// 引数に指定したビヘイビアの紐づけ先のオブジェクトを決定します
    /// </summary>
    /// <param name="original">紐づけを行う対象オブジェクト</param>
    /// <returns>紐づけ先のオブジェクト</returns>
    virtual protected GameObject MapObjectToType<T>(T original)
    {
        return null;
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
            bhv.transform.SetParent(parentObj.transform);
        }
    }
}
