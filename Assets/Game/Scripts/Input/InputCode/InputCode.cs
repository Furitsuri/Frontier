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
    public delegate bool EnableCallback();      // 入力可否の判定を行うコールバック

    public GuideIcon[] Icons;                   // 入力アイコン
    public InputCodeStringWrapper Explanation;  // アイコンに対する説明文
    public EnableCallback[] EnableCbs;          // 入力の有効・無効を判定するコールバック
    public IAcceptInputBase[] AcceptInputs;     // 入力受付のコールバック
    public float InputInterval;                 // 入力処理を有効にするインターバル
    public int RegisterClassHashCode;           // 入力コード登録を行ったクラスのハッシュ値
    public bool IsSimultaneousInput;            // 同時入力か否か
    public InputTriggerMode TriggerMode;        // ボタン系入力の判定タイミング(Up/Down/DownRepeat)
    public bool IsGuideVisible = true;          // 入力ガイドとして画面に表示するか(falseの場合、入力受付は行うがガイド表示は行わない)
    public float RepeatDelay = 0f;              // 押しっぱなし状態でリピート入力(インターバル間隔での連続受付)を開始するまでの遅延時間(秒)。0の場合は遅延なし(押した瞬間からインターバル間隔で受け付ける、従来の挙動)
    private float _inputLastTime;               // 入力処理を行った最後の時間
    private float _pressStartTime = -1f;        // 現在の連続押下(hold)が開始した時刻。-1は「押されていない」ことを表す

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
    /// <param name="triggerMode">ボタン系入力の判定タイミング(既定はUp)</param>
    public InputCode( GuideIcon[] icons, InputCodeStringWrapper explwrapper, EnableCallback[] enableCbs, IAcceptInputBase[] acceptInputs, float interval, int hashCode, InputTriggerMode triggerMode = InputTriggerMode.Up )
    {
        Icons                   = icons;
        Explanation             = explwrapper;
        EnableCbs               = enableCbs;
        AcceptInputs            = acceptInputs;
        InputInterval           = interval;
        RegisterClassHashCode   = hashCode;
        _inputLastTime          = 0f;
        IsSimultaneousInput     = false;
        TriggerMode             = triggerMode;
    }

    /// <summary>
    /// 上記と同様ですが、説明文を直接文字列で指定します
    /// </summary>
    public InputCode( GuideIcon[] icons, string expl, EnableCallback[] enableCbs, IAcceptInputBase[] acceptInputs, float interval, int hashCode, InputTriggerMode triggerMode = InputTriggerMode.Up )
    {
        Icons                   = icons;
        Explanation             = new InputCodeStringWrapper( expl );
        EnableCbs               = enableCbs;
        AcceptInputs            = acceptInputs;
        InputInterval           = interval;
        RegisterClassHashCode   = hashCode;
        _inputLastTime          = 0f;
        IsSimultaneousInput     = false;
        TriggerMode             = triggerMode;
    }

    /// <summary>
    /// ガイドアイコン及び入力受付関数が単一ケースの入力コードを設定します
    /// 説明文はラッパークラスを使用してください
    /// </summary>
    public InputCode( GuideIcon icon, InputCodeStringWrapper explwrapper, EnableCallback enableCb, IAcceptInputBase acceptInput, float interval, int hashCode, InputTriggerMode triggerMode = InputTriggerMode.Up )
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
        IsSimultaneousInput     = false;
        TriggerMode             = triggerMode;
    }

    /// <summary>
    /// 単一のアイコン、入力受付関数を用い、説明文を直接文字列で指定したい場合に用います
    /// </summary>
    public InputCode( GuideIcon icon, string expl, EnableCallback enableCb, IAcceptInputBase acceptInput, float interval, int hashCode, InputTriggerMode triggerMode = InputTriggerMode.Up )
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
        TriggerMode             = triggerMode;
    }

    /// <summary>
    /// 主に同時入力を受付させる場合に用います
    /// 複数のガイドアイコンを設定可能で、説明文や入力受付関数については単一のものを使用します
    /// </summary>
    public InputCode( GuideIcon[] icons, string expl, EnableCallback enableCb, IAcceptInputBase acceptInput, float interval, int hashCode, InputTriggerMode triggerMode = InputTriggerMode.Up )
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
        TriggerMode             = triggerMode;
    }

    /// <summary>
    ///
    /// </summary>
    /// <returns></returns>
    public InputCode Clone()
    {
        var clone = new InputCode
        (
            this.Icons,
            this.Explanation,
            this.EnableCbs,
            this.AcceptInputs,
            this.InputInterval,
            this.RegisterClassHashCode,
            this.TriggerMode
        );

        clone.IsGuideVisible = this.IsGuideVisible;
        clone.RepeatDelay    = this.RepeatDelay;

        return clone;
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
    /// 上記に加え、末尾にTriggerModeを指定できる7要素タプル版です。
    /// 押しっぱなしでの連続受付(DownRepeat)等、Up以外のモードを使いたい場合に利用してください。
    /// </summary>
    static public implicit operator InputCode( (GuideIcon[], InputCodeStringWrapper, EnableCallback[], IAcceptInputBase[], float, int, InputTriggerMode) tuple )
    {
        return new InputCode( tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6, tuple.Item7 );
    }

    static public implicit operator InputCode( (GuideIcon[], string, EnableCallback[], IAcceptInputBase[], float, int, InputTriggerMode) tuple )
    {
        return new InputCode( tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6, tuple.Item7 );
    }

    static public implicit operator InputCode( (GuideIcon, InputCodeStringWrapper, EnableCallback, IAcceptInputBase, float, int, InputTriggerMode) tuple )
    {
        return new InputCode( tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6, tuple.Item7 );
    }

    static public implicit operator InputCode( (GuideIcon, string, EnableCallback, IAcceptInputBase, float, int, InputTriggerMode) tuple )
    {
        return new InputCode( tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6, tuple.Item7 );
    }

    static public implicit operator InputCode( (GuideIcon[], string, EnableCallback, IAcceptInputBase, float, int, InputTriggerMode) tuple )
    {
        return new InputCode( tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6, tuple.Item7 );
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
    /// 生の入力状態(isHeld: そのフレームで対応するキー・ボタンが押されているか)を渡し、
    /// このフレームでAccept処理を実行してよいかを判定します。
    /// 押されていなければ連続押下の起点をリセットしfalseを返します。
    /// 押された瞬間はRepeatDelayを待たず、インターバルのみを考慮して受け付けます
    /// (これにより単発のタップ操作が意図せず2回受け付けられることを防ぎます)。
    /// 押しっぱなしの場合は、RepeatDelay経過後、InputIntervalの間隔で繰り返し受け付けます。
    /// RepeatDelayが0の場合は遅延無し(押した瞬間からインターバル間隔で受け付ける、従来の挙動)です。
    /// </summary>
    /// <param name="isHeld">このフレームで対応する入力が押されている状態かどうか</param>
    /// <returns>このフレームでAcceptを実行してよいか</returns>
    public bool UpdateHoldState( bool isHeld )
    {
        if ( !isHeld )
        {
            _pressStartTime = -1f;
            return false;
        }

        bool isNewPress = ( _pressStartTime < 0f );
        if ( isNewPress ) { _pressStartTime = Time.time; }

        if ( !IsIntervalTimePassed() ) { return false; }

        // 押しっぱなし2回目以降の受付は、RepeatDelayが経過するまで待つ
        if ( !isNewPress && 0f < RepeatDelay && ( Time.time - _pressStartTime ) < RepeatDelay ) { return false; }

        return true;
    }

    /// <summary>
    /// いつでも入力可能であることを示すためにtrueを返すだけの関数です
    /// 関数定義が必要のない場面でRegisterに登録してください
    /// </summary>
    /// <returns>入力可能(true)</returns>
    static public bool CanAcceptInputAlways() => true;
}