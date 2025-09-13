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
    public GuideIcon[] Icons;
    // アイコンに対する説明文
    public string Explanation;
    // 有効・無効を判定するコールバック
    public EnableCallback[] EnableCbs;
    // 入力受付のコールバック
    public IAcceptInputBase[] AcceptInputs;
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
    public InputCode( GuideIcon[] icons, string expl, EnableCallback[] enableCbs, IAcceptInputBase[] acceptInputs, float interval, int hashCode )
    {
        Icons = icons;
        Explanation = expl;
        EnableCbs = enableCbs;
        AcceptInputs = acceptInputs;
        InputInterval = interval;
        RegisterClassHashCode = hashCode;
        InputLastTime = 0f;
    }

    /// <summary>
    /// ガイドアイコン及び入力受付関数が単一ケースの入力コードを設定します
    /// </summary>
    /// <param name="icon">ガイドアイコン</param>
    /// <param name="expl">説明文</param>
    /// <param name="enableCb">入力受付判定のコールバック</param>
    /// <param name="acceptInput">入力時のコールバック</param>
    /// <param name="interval">入力受付のインターバル時間</param>
    /// <param name="hashCode">コード登録を行ったクラスのハッシュ値</param>
    public InputCode( GuideIcon icon, string expl, EnableCallback enableCb, IAcceptInputBase acceptInput, float interval, int hashCode )
    {
        Icons = new GuideIcon[1];
        EnableCbs = new EnableCallback[1];
        AcceptInputs = new IAcceptInputBase[1];

        Icons[0] = icon;
        Explanation = expl;
        EnableCbs[0] = enableCb;
        AcceptInputs[0] = acceptInput;
        InputInterval = interval;
        RegisterClassHashCode = hashCode;
        InputLastTime = 0f;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public InputCode Clone()
    {
        return new InputCode
        (
            this.Icons,
            this.Explanation,
            this.EnableCbs,
            this.AcceptInputs,
            this.InputInterval,
            this.RegisterClassHashCode
        );
    }

    /// <summary>
    /// オペレーター
    /// </summary>
    /// <param name="tuple">オペレーター対象の設定</param>
    static public implicit operator InputCode( (GuideIcon[], string, EnableCallback[], IAcceptInputBase[], float, int) tuple )
    {
        return new InputCode( tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6 );
    }

    /// <summary>
    /// オペレーター
    /// </summary>
    /// <param name="tuple">オペレーター対象の設定</param>
    static public implicit operator InputCode( (GuideIcon, string, EnableCallback, IAcceptInputBase, float, int) tuple )
    {
        return new InputCode( tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6 );
    }

    /// <summary>
    /// 入力受付時のコールバックを実行します
    /// </summary>
    /// <typeparam name="T">実行するコールバックの型</typeparam>
    /// <param name="input">受け取った入力</param>
    public bool ExecuteAcceptInputCallback<T>( T input, int acceptIdx )
    {
        if ( AcceptInputs == null || AcceptInputs[acceptIdx] == null )
        {
            Debug.Assert( false );
            return false;
        }

        bool hasInput = AcceptInputs[acceptIdx].Accept( input );
        if ( hasInput ) { SetInputLastTime( Time.time ); }  // 最後の入力時間を記録

        return hasInput;
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
    public void SetInputLastTime( float time )
    {
        InputLastTime = time;
    }

    /// <summary>
    /// 未登録であるかを取得します
    /// </summary>
    /// <returns>未登録か否か</returns>
    public bool IsUnRegistererd()
    {
        return ( EnableCbs == null );
    }

    /// <summary>
    /// インターバル時間が経過したかの判定を取得します
    /// </summary>
    /// <returns>インターバル時間が経過したか</returns>
    public bool IsIntervalTimePassed()
    {
        return ( InputInterval <= Time.time - InputLastTime );
    }

    /// <summary>
    /// いつでも入力可能であることを示すためにtrueを返すだけの関数です
    /// 関数定義が必要のない場面でRegisterに登録してください
    /// </summary>
    /// <returns>入力可能(true)</returns>
    static public bool CanAcceptInputAlways() => true;
}