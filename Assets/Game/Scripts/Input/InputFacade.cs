using System.Collections.Generic;
using System.Linq;
using Zenject;
using static Constants;

public class InputFacade
{
    [Inject] private HierarchyBuilderBase _hierarchyBld = null;

    private InputGuidePresenter _inputGuideView = null;
    private InputHandler _inputHdlr             = null;
    private List<InputCode> _inputCodes         = new List<InputCode>();

    /// <summary>
    /// 初期化します
    /// </summary>
    public void Init()
    {
        LazyInject.GetOrCreate( ref _inputHdlr, () => _hierarchyBld.CreateComponentAndOrganize<InputHandler>( true, "InputHandler" ) );
        LazyInject.GetOrCreate( ref _inputGuideView, () => _hierarchyBld.InstantiateWithDiContainer<InputGuidePresenter>( false ) );
        _inputHdlr.Setup();
        _inputGuideView.Setup();

        // 入力コード情報を受け渡す
        _inputHdlr.Init( _inputGuideView, _inputCodes );
        _inputGuideView.Init( _inputCodes );
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
        for( int i = 0; i < _inputCodes.Count; ++i )
        {
            if( _inputCodes[i].RegisterClassHashCode == hashCode )
            {
                _inputCodes[i].Explanation = new InputCodeStringWrapper( "" );
                _inputCodes[i].EnableCbs = null;
                _inputCodes[i].ResetIntervalTime();
                _inputCodes[i].SetInputLastTime( 0.0f );
            }
        }
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
                if( code.Icons.First() == arg.Icons.First() && !code.IsUnRegistererd() )
                {
                    LogHelper.LogError( $"InputCode is already registered. Icon: {arg.Icons.First()}, Explanation: {arg.Explanation}" );
                }
            }

            _inputCodes.Add( arg );
        }

        _inputCodes.Sort( ( a, b ) => a.Icons.First().CompareTo( b.Icons.First() ) );

        // ガイドアイコンを登録
        _inputGuideView.RegisterInputGuides();
    }

    /// <summary>
    /// 登録している入力コードを初期化した上で、
    /// 現在のゲーム遷移において有効とする操作入力を画面上に表示するガイドUIと併せて登録します。
    /// また、そのキーを押下した際の処理をコールバックとして登録します。
    /// </summary>
    /// <param name="args">登録するアイコン、その説明文、及び押下時に対応する処理の関数コールバック</param>
    public void ReRegisterInputCodes<T>( InputCode[] args )
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
}