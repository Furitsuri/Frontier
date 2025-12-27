using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
