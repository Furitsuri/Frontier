using System;
using UnityEngine;
using static Constants;
using static InputFacade;

/// <summary>
/// 指定のKeyCodeがtrueであれば有効,
/// falseであれば無効を表します
/// </summary>
public class InputCode
{
    public delegate bool EnableCallback();
    public delegate bool AcceptInputCallback<T>(T arg);

    // 入力アイコン
    public GuideIcon Icon;
    // アイコンに対する説明文
    public string Explanation;
    // 有効・無効を判定するコールバック
    public EnableCallback EnableCb;
    // 入力受付のコールバック
    private Delegate AcceptCb;
    // 入力処理を有効にするインターバル
    public float InputInterval;
    // 入力処理を行った最後の時間
    private float InputLastTime;

    /// <summary>
    /// 入力コードを設定します
    /// </summary>
    /// <param name="enableCb">入力受付判定のコールバック</param>
    /// <param name="icon">ガイドアイコン</param>
    /// <param name="expl">説明文</param>
    /// <param name="inputCb">入力時のコールバック</param>
    public InputCode(GuideIcon icon, string expl, EnableCallback enableCb, float interval)
    {
        Icon            = icon;
        Explanation     = expl;
        EnableCb        = enableCb;
        InputInterval   = interval;
        InputLastTime   = 0f;
    }

    /// <summary>
    /// オペレーター
    /// </summary>
    /// <param name="tuple">オペレーター対象の設定</param>
    public static implicit operator InputCode((GuideIcon, string, EnableCallback, float) tuple)
    {
        // InputCode code = new InputCode(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
        // code.SetAcceptCallback<T>(tuple.Item4);

        return new InputCode(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
    }

    /// <summary>
    /// 入力受付時のコールバックを実行します
    /// </summary>
    /// <typeparam name="T">実行するコールバックの型</typeparam>
    /// <param name="input">受け取った入力</param>
    public void ExecuteAcceptInputCallback<T>( T input )
    {
        if (AcceptCb == null)
        {
            Debug.Assert(false);
            return;
        }

        bool hasInput = false;

        // 引数の型を判別
        if (input is Constants.Direction dir &&
            AcceptCb is AcceptInputCallback<Constants.Direction> dirCb)
        {
            hasInput = dirCb( dir );
        }
        else if ( input is bool booleanInput &&
            AcceptCb is AcceptInputCallback<bool> booleanCb)
        {
            hasInput = booleanCb( booleanInput );
        }
        else
        {
            Debug.Assert(false, "入力受付のコールバックが正しく設定されていません。");
        }

        // 最後の入力時間を記録
        if( hasInput ) SetInputLastTime(Time.time);
    }

    /// <summary>
    /// 入力受付のコールバック
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="arg"></param>
    public void SetAcceptCallback<T>( AcceptInputCallback<T> arg )
    {
        AcceptCb = arg;
    }

    public void SetAcceptCallback(Delegate cb)
    {
        AcceptCb = cb;
    }

    /// <summary>
    /// 最後に入力を行った時間を設定します
    /// </summary>
    /// <param name="time">入力を行った時間</param>
    public void SetInputLastTime(float time)
    {
        InputLastTime = time;
    }

    /// <summary>
    /// 未登録であるかを取得します
    /// </summary>
    /// <returns>未登録か否か</returns>
    public bool IsUnRegistererd()
    {
        return (EnableCb == null);
    }

    /// <summary>
    /// インターバル時間が経過したかの判定を取得します
    /// </summary>
    /// <returns>インターバル時間が経過したか</returns>
    public bool IsIntervalTimePassed()
    {
        return (InputInterval <= Time.time - InputLastTime);
    }
}