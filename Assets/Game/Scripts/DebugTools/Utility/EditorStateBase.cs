using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

#if UNITY_EDITOR

public class EditorStateBase : StateBase
{
    [Inject] protected IUiSystem _uiSystem = null;

    /// <summary>
    /// 入力コードのハッシュ値を取得します
    /// </summary>
    /// <returns>入力コードのハッシュ値</returns>
    protected int GetInputCodeHash()
    {
        return Hash.GetStableHash( GetType().Name );
    }

    /// <summary>
    /// 現在のステートを実行します
    /// </summary>
    override public void RunState()
    {
        base.RunState();
        RegisterInputCodes();
    }

    /// <summary>
    /// 現在のステートを再開します
    /// </summary>
    override public void RestartState()
    {
        base.RestartState();
        RegisterInputCodes();
    }

    /// <summary>
    /// 現在のステートを中断します
    /// </summary>
    override public void PauseState()
    {
        base.PauseState();
        UnregisterInputCodes( Hash.GetStableHash( GetType().Name ) );
    }

    /// <summary>
    /// 現在のステートから退避します
    /// </summary>
    override public void ExitState()
    {
        base.ExitState();
        UnregisterInputCodes( Hash.GetStableHash( GetType().Name ) );
    }

    /// <summary>
    /// 入力コードを登録します
    /// </summary>
    virtual public void RegisterInputCodes() { }

    /// <summary>
    /// 入力を受付るかを取得します
    /// 多くのケースでこちらの関数を用いて判定します
    /// </summary>
    /// <returns>入力受付の可否</returns>
    virtual protected bool CanAcceptDefault()
    {
        // 現在のステートから脱出する場合は入力を受け付けない
        return !IsBack();
    }

    /// <summary>
    /// 指定するハッシュ値の入力コードを登録解除します
    /// </summary>
    /// <param name="hashCode">ハッシュ値</param>
    virtual public void UnregisterInputCodes( int hashCode )
    {
        _inputFcd.UnregisterInputCodes( hashCode );
    }

    virtual protected bool CanAcceptDirection() { return false; }
    virtual protected bool CanAcceptConfirm() { return false; }
    virtual protected bool CanAcceptCancel() { return false; }
    virtual protected bool CanAcceptTool() { return false; }
    virtual protected bool CanAcceptInfo() { return false; }
    virtual protected bool CanAcceptOptional1() { return false; }
    virtual protected bool CanAcceptOptional2() { return false; }
    virtual protected bool CanAcceptSub1() { return false; }
    virtual protected bool CanAcceptSub2() { return false; }
    virtual protected bool CanAcceptSub3() { return false; }
    virtual protected bool CanAcceptSub4() { return false; }
    virtual protected bool CanAcceptDebugTransition() { return false; }


    virtual protected bool AcceptDirection( Direction dir ) { return false; }
    virtual protected bool AcceptConfirm( bool isInput ) { return false; }
    virtual protected bool AcceptCancel( bool isCancel ) { return false; }
    virtual protected bool AcceptTool( bool isInput ) { return false; }
    virtual protected bool AcceptInfo( bool isInput ) { return false; }
    virtual protected bool AcceptOptional1( bool isOptional ) { return false; }
    virtual protected bool AcceptOptional2( bool isOptional ) { return false; }
    virtual protected bool AcceptSub1( bool isInput ) { return false; }
    virtual protected bool AcceptSub2( bool isInput ) { return false; }
    virtual protected bool AcceptSub3( bool isInput ) { return false; }
    virtual protected bool AcceptSub4( bool isInput ) { return false; }
    virtual protected bool AcceptDebugTransition( bool isInput ) { return false; }
}

#endif // UNITY_EDITOR