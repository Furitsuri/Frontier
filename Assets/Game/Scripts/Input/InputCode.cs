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

    // 入力アイコン
    public GuideIcon Icon;
    // アイコンに対する説明文
    public string Explanation;
    // 有効・無効を判定するコールバック
    public EnableCallback EnableCb;
    // 入力受付のコールバック
    public IAcceptInputBase AcceptInput;
    // 入力処理を有効にするインターバル
    public float InputInterval;
    // 入力コード登録を行ったクラスのハッシュ値
    public int RegisterClassHashCode;
    // 入力処理を行った最後の時間
    private float InputLastTime;

    /// <summary>
    /// 入力コードを設定します
    /// </summary>
    /// <param name="icon">ガイドアイコン</param>
    /// <param name="expl">説明文</param>
    /// <param name="enableCb">入力受付判定のコールバック</param>
    /// <param name="acceptInput">入力時のコールバック</param>
    /// <param name="interval">入力受付のインターバル時間</param>
    /// <param name="hashCode">コード登録を行ったクラスのハッシュ値</param>
    public InputCode(GuideIcon icon, string expl, EnableCallback enableCb, IAcceptInputBase acceptInput, float interval, int hashCode)
    {
        Icon                    = icon;
        Explanation             = expl;
        EnableCb                = enableCb;
        AcceptInput             = acceptInput;
        InputInterval           = interval;
        RegisterClassHashCode   = hashCode;
        InputLastTime           = 0f;
    }

    /// <summary>
    /// オペレーター
    /// </summary>
    /// <param name="tuple">オペレーター対象の設定</param>
    public static implicit operator InputCode((GuideIcon, string, EnableCallback, IAcceptInputBase, float, int) tuple)
    {
        return new InputCode(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6);
    }

    /// <summary>
    /// 入力受付時のコールバックを実行します
    /// </summary>
    /// <typeparam name="T">実行するコールバックの型</typeparam>
    /// <param name="input">受け取った入力</param>
    public void ExecuteAcceptInputCallback<T>( T input )
    {
        if (AcceptInput == null)
        {
            Debug.Assert(false);
            return;
        }

        bool hasInput = AcceptInput.AcceptInput( input );

        // 最後の入力時間を記録
        if( hasInput ) SetInputLastTime(Time.time);
    }

    /// <summary>
    /// innterval時間をリセットします
    /// </summary>
    public void ResetIntervalTime()
    {
        InputLastTime = 0f;
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