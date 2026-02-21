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

    public GuideIcon[] Icons;                   // 入力アイコン
    public InputCodeStringWrapper Explanation;  // アイコンに対する説明文
    public EnableCallback[] EnableCbs;          // 有効・無効を判定するコールバック
    public IAcceptInputBase[] AcceptInputs;     // 入力受付のコールバック
    public float InputInterval;                 // 入力処理を有効にするインターバル
    public int RegisterClassHashCode;           // 入力コード登録を行ったクラスのハッシュ値
    public bool IsSimultaneousInput;            // 同時入力か否か
    private float _inputLastTime;               // 入力処理を行った最後の時間

    /// <summary>
    /// 入力コードを設定します
    /// 複数のガイドアイコン及び入力受付関数を設定できます
    /// 説明文はラッパークラスを使用してください
    /// </summary>
    /// <param name="icons">ガイドアイコン</param>
    /// <param name="explwrapper">説明文が挿入されたラッパー</param>
    /// <param name="enableCbs">入力受付判定のコールバック</param>
    /// <param name="acceptInputs">入力時のコールバック</param>
    /// <param name="interval">入力受付のインターバル時間</param>
    /// <param name="hashCode">コード登録を行ったクラスのハッシュ値</param>
    public InputCode( GuideIcon[] icons, InputCodeStringWrapper explwrapper, EnableCallback[] enableCbs, IAcceptInputBase[] acceptInputs, float interval, int hashCode )
    {
        Icons                   = icons;
        Explanation             = explwrapper;
        EnableCbs               = enableCbs;
        AcceptInputs            = acceptInputs;
        InputInterval           = interval;
        RegisterClassHashCode   = hashCode;
        _inputLastTime          = 0f;
        IsSimultaneousInput     = false;
    }

    /// <summary>
    /// 上記と同様ですが、説明文を直接文字列で指定します
    /// </summary>
    public InputCode( GuideIcon[] icons, string expl, EnableCallback[] enableCbs, IAcceptInputBase[] acceptInputs, float interval, int hashCode )
    {
        Icons                   = icons;
        Explanation             = new InputCodeStringWrapper( expl );
        EnableCbs               = enableCbs;
        AcceptInputs            = acceptInputs;
        InputInterval           = interval;
        RegisterClassHashCode   = hashCode;
        _inputLastTime          = 0f;
        IsSimultaneousInput     = false;
    }

    /// <summary>
    /// ガイドアイコン及び入力受付関数が単一ケースの入力コードを設定します
    /// 説明文はラッパークラスを使用してください
    /// </summary>
    public InputCode( GuideIcon icon, InputCodeStringWrapper explwrapper, EnableCallback enableCb, IAcceptInputBase acceptInput, float interval, int hashCode )
    {
        Icons           = new GuideIcon[1];
        EnableCbs       = new EnableCallback[1];
        AcceptInputs    = new IAcceptInputBase[1];

        Icons[0]                = icon;
        Explanation             = explwrapper;
        EnableCbs[0]            = enableCb;
        AcceptInputs[0]         = acceptInput;
        InputInterval           = interval;
        RegisterClassHashCode   = hashCode;
        _inputLastTime          = 0f;
        IsSimultaneousInput    = false;
    }

    /// <summary>
    /// 単一のアイコン、入力受付関数を用い、説明文を直接文字列で指定したい場合に用います
    /// </summary>
    public InputCode( GuideIcon icon, string expl, EnableCallback enableCb, IAcceptInputBase acceptInput, float interval, int hashCode )
    {
        Icons           = new GuideIcon[1];
        EnableCbs       = new EnableCallback[1];
        AcceptInputs    = new IAcceptInputBase[1];

        Icons[0]                = icon;
        Explanation             = new InputCodeStringWrapper( expl );
        EnableCbs[0]            = enableCb;
        AcceptInputs[0]         = acceptInput;
        InputInterval           = interval;
        RegisterClassHashCode   = hashCode;
        _inputLastTime          = 0f;
        IsSimultaneousInput     = false;
    }

    /// <summary>
    /// 主に同時入力を受付させる場合に用います
    /// 複数のガイドアイコンを設定可能で、説明文や入力受付関数については単一のものを使用します
    /// </summary>
    public InputCode( GuideIcon[] icons, string expl, EnableCallback enableCb, IAcceptInputBase acceptInput, float interval, int hashCode )
    {
        EnableCbs = new EnableCallback[1];
        AcceptInputs = new IAcceptInputBase[1];

        Icons                   = icons;
        Explanation             = new InputCodeStringWrapper( expl );
        EnableCbs[0]            = enableCb;
        AcceptInputs[0]         = acceptInput;
        InputInterval           = interval;
        RegisterClassHashCode   = hashCode;
        _inputLastTime          = 0f;
        IsSimultaneousInput     = true;
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
    /// 上記のコンストラクタをタプルでまとめて呼び出せるようにするためのオペレーター群です
    /// </summary>
    /// <param name="tuple">オペレーター対象の設定</param>
    static public implicit operator InputCode( (GuideIcon[], InputCodeStringWrapper, EnableCallback[], IAcceptInputBase[], float, int ) tuple )
    {
        return new InputCode( tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6 );
    }

    static public implicit operator InputCode( (GuideIcon[], string, EnableCallback[], IAcceptInputBase[], float, int) tuple )
    {
        return new InputCode( tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6 );
    }

    static public implicit operator InputCode( (GuideIcon, InputCodeStringWrapper, EnableCallback, IAcceptInputBase, float, int) tuple )
    {
        return new InputCode( tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6 );
    }

    static public implicit operator InputCode( (GuideIcon, string, EnableCallback, IAcceptInputBase, float, int) tuple )
    {
        return new InputCode( tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6 );
    }

    static public implicit operator InputCode( (GuideIcon[], string, EnableCallback, IAcceptInputBase, float, int) tuple )
    {
        return new InputCode( tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6 );
    }

    /// <summary>
    /// 入力受付のコールバックを実行します
    /// </summary>
    /// <param name="context"></param>
    /// <param name="acceptIdx"></param>
    /// <returns></returns>
    public bool ExecuteAcceptInputCallback( InputContext context, int acceptIdx )
    {
        if( AcceptInputs == null || AcceptInputs[acceptIdx] == null )
        {
            Debug.Assert( false );
            return false;
        }

        bool hasInput = AcceptInputs[acceptIdx].Accept( context );
        if( hasInput ) { SetInputLastTime( Time.time ); }  // 最後の入力時間を記録

        return hasInput;
    }

    /// <summary>
    /// 同時入力に対する入力受付のコールバックを実行します
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public bool ExecuteAcceptSimultaneousInputCallback( InputContext context )
    {
        if( AcceptInputs == null || AcceptInputs[0] == null )
        {
            Debug.Assert( false );
            return false;
        }

        bool hasInput = AcceptInputs[0].Accept( context );
        if( hasInput ) { SetInputLastTime( Time.time ); }  // 最後の入力時間を記録

        return hasInput;
    }

    public void Dispose()
    {
        Explanation     = null;
        AcceptInputs    = null;
    }

    /// <summary>
    /// innterval時間をリセットします
    /// </summary>
    public void ResetIntervalTime()
    {
        _inputLastTime = 0f;
    }

    /// <summary>
    /// 最後に入力を行った時間を設定します
    /// </summary>
    /// <param name="time">入力を行った時間</param>
    public void SetInputLastTime( float time )
    {
        _inputLastTime = time;
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
        return ( InputInterval <= Time.time - _inputLastTime );
    }

    /// <summary>
    /// いつでも入力可能であることを示すためにtrueを返すだけの関数です
    /// 関数定義が必要のない場面でRegisterに登録してください
    /// </summary>
    /// <returns>入力可能(true)</returns>
    static public bool CanAcceptInputAlways() => true;
}