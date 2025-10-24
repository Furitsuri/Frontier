using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

#if UNITY_EDITOR

public class EditorStateBase : StateBase
{
    [Inject]
    public void Construct(InputFacade inputFcd)
    {
        _inputFcd = inputFcd;
    }

    /// <summary>
    /// 入力コードのハッシュ値を取得します
    /// </summary>
    /// <returns>入力コードのハッシュ値</returns>
    protected int GetInputCodeHash()
    {
        return Hash.GetStableHash(GetType().Name);
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
        UnregisterInputCodes(Hash.GetStableHash(GetType().Name));
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

    /// <summary>
    /// 方向入力を受け取った際の処理を行います
    /// </summary>
    /// <param name="dir">方向入力</param>
    /// <returns>入力実行の有無</returns>
    virtual protected bool AcceptDirection(Direction dir)
    {
        return false;
    }

    /// <summary>
    /// 決定入力を受けた際の処理を行います
    /// </summary>
    /// <param name="isConfirm">決定入力</param>
    /// <returns>入力実行の有無</returns>
    virtual protected bool AcceptConfirm(bool isInput)
    {
        return false;
    }

    /// <summary>
    /// キャンセル入力を受けた際の処理を行います
    /// </summary>
    /// <param name="isCancel">キャンセル入力</param>
    /// <returns>入力実行の有無</returns>
    virtual protected bool AcceptCancel(bool isCancel)
    {
        return false;
    }

    /// <summary>
    /// ツール画面入力を受けた際の処理を行います
    /// </summary>
    /// <param name="isInput">ツール画面入力</param>
    /// <returns>入力実行の有無</returns>
    virtual protected bool AcceptTool ( bool isInput )
    {
        return false;
    }

    /// <summary>
    /// 情報画面入力を受けた際の処理を行います
    /// </summary>
    /// <param name="isInput">情報画面入力</param>
    /// <returns>入力実行の有無</returns>
    virtual protected bool AcceptInfo( bool isInput )
    {
        return false;
    }

    /// <summary>
    /// オプション入力1を受けた際の処理を行います
    /// </summary>
    /// <param name="isOptional">オプション入力</param>
    /// <returns>入力実行の有無</returns>
    virtual protected bool AcceptOptional1(bool isOptional)
    {
        return false;
    }

    /// <summary>
    /// オプション入力2を受けた際の処理を行います
    /// </summary>
    /// <param name="isOptional">オプション入力</param>
    /// <returns>入力実行の有無</returns>
    virtual protected bool AcceptOptional2(bool isOptional)
    {
        return false;
    }

    virtual protected bool AcceptSub1(bool isInput)
    {
        return false;
    }

    virtual protected bool AcceptSub2(bool isInput)
    {
        return false;
    }

    virtual protected bool AcceptSub3(bool isInput)
    {
        return false;
    }

    virtual protected bool AcceptSub4(bool isInput)
    {
        return false;
    }
}

#endif // UNITY_EDITOR