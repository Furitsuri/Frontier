using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 入力処理全体のファサード。シーンを跨いで永続化するシングルトン。
/// 実体である InputHandler は初回の Setup() でのみ生成し、DontDestroyOnLoad で保持する。
/// 入力ガイドUI(InputGuidePresenter)は IUiSystem を持つシーン(GameMain等)でのみ DI 経由で生成して渡す想定で、
/// 渡さない場合(IUiSystem を持たないシーン)はガイド表示なしで入力処理のみ行う。
/// </summary>
public class InputFacade
{
    private static InputFacade _instance = null;
    public static InputFacade Instance => _instance ??= new InputFacade();

    private InputHandler _inputHdlr             = null;
    private InputGuidePresenter _inputGuideView = null;
    private List<InputCode> _inputCodes         = new List<InputCode>();

    private InputFacade() { }

    /// <summary>
    /// 入力処理本体をセットアップします。
    /// </summary>
    /// <param name="inputGuideView">
    /// 入力ガイドUIの表示制御クラス。IUiSystem を持つシーンから DI 経由で生成して渡す。
    /// 渡さない(null)場合は入力ガイドを表示せず、入力処理のみ行う。
    /// </param>
    public void Setup( InputGuidePresenter inputGuideView = null )
    {
        // シーン切り替えをまたいで古い入力コードが残らないようにする
        UnregisterInputCodes();

        if ( _inputHdlr == null )
        {
            var go = new GameObject( nameof( InputHandler ) );
            Object.DontDestroyOnLoad( go );
            _inputHdlr = go.AddComponent<InputHandler>();
            _inputHdlr.Setup();
        }

        _inputGuideView = inputGuideView;
        _inputGuideView?.Setup();
    }

    public void Init()
    {
        // 入力コード情報を受け渡す
        _inputHdlr.Init( _inputGuideView, _inputCodes );
        _inputGuideView?.Init( _inputCodes );
    }

    /// <summary>
    /// 判定対象となる入力コードを初期化します
    /// Iconは変更しないため、そのままにします
    /// </summary>
    public void UnregisterInputCodes()
    {
        foreach( InputCode code in _inputCodes )
        {
            code.Dispose();
        }

        _inputCodes.Clear();
    }

    /// <summary>
    /// 指定するハッシュ値と一致する登録済みの入力コードを解除します。
    /// </summary>
    /// <param name="hashCode">ハッシュ値</param>
    public void UnregisterInputCodes( int hashCode )
    {
        _inputCodes.RemoveAll( code =>
        {
            if( code.RegisterClassHashCode == hashCode )
            {
                code.Dispose();

                return true;
            }

            return false;
        } );
    }

    /// <summary>
    /// 既に登録済みの入力コードはそのままに、
    /// 現在のゲーム遷移において有効にしたい入力コードを、画面上に表示するガイドUIと併せて登録します。
    /// また、そのキーを押下した際の処理をコールバックとして登録します。
    /// </summary>
    /// <param name="args">登録するアイコン、その説明文、及び押下時に対応する処理の関数コールバック</param>
    public void RegisterInputCodes( params InputCode[] args )
    {
        foreach( var arg in args )
        {
            if( arg == null ) { continue; }

            // 既に登録済みのガイドアイコンで入力コードが登録されている場合はエラー
            foreach( var code in _inputCodes )
            {
                if( code.Icons.First() == arg.Icons.First() )
                {
                    LogHelper.LogError( $"InputCode is already registered. Icon: {arg.Icons.First()}, Explanation: {arg.Explanation}" );
                }
            }

            _inputCodes.Add( arg );
        }

        _inputCodes.Sort( ( a, b ) => a.Icons.First().CompareTo( b.Icons.First() ) );

        // ガイドアイコンを登録
        _inputGuideView?.RegisterInputGuides();
    }

    /// <summary>
    /// 登録している入力コードを初期化した上で、
    /// 現在のゲーム遷移において有効とする操作入力を画面上に表示するガイドUIと併せて登録します。
    /// また、そのキーを押下した際の処理をコールバックとして登録します。
    /// </summary>
    /// <param name="args">登録するアイコン、その説明文、及び押下時に対応する処理の関数コールバック</param>
    public void ReRegisterInputCodes( InputCode[] args )
    {
        UnregisterInputCodes();

        RegisterInputCodes( args );
    }

    /// <summary>
    /// 入力コードのインターバル時間をリセットします。
    /// </summary>
    private void ResetIntervalTimeOnInputCodes()
    {
        for( int i = 0; i < _inputCodes.Count; ++i )
        {
            _inputCodes[i].ResetIntervalTime();
        }
    }

    /// <summary>
    /// 常時入力受付可能を示します
    /// </summary>
    /// <returns></returns>
    static public bool CanBeAcceptAlways()
    {
        return true;
    }

    /// <summary>
    /// 入力ガイドバー全体の表示可否を切り替えます(オプション設定用)。
    /// 入力コードの登録・受付処理自体には影響しません。
    /// </summary>
    /// <param name="visible">表示するか</param>
    public void SetGuideVisible( bool visible )
    {
        _inputGuideView?.SetGuideVisible( visible );
    }
}
