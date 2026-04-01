using UnityEngine;

/// <summary>
/// UI用のMonoBehaviourの基底クラスです
/// UIの初期化処理を共通化するために使用します
/// (初期化はAwakeやStartではなく、このクラスのSetupメソッドで行うようにしてください)。
/// </summary>
public class UiMonoBehaviour : MonoBehaviour
{
    /// <summary>
    /// クラス内のメンバの生成と初期化を行います。
    /// AwakeやStartで生成を行うと、インスペクターでアクティブ状態に設定されていない場合に正しく初期化されないため、このメソッドで行います。
    /// </summary>
    virtual public void Setup()
    {
        gameObject.SetActive( false );
    }
}
