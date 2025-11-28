using Frontier.Stage;
using System.Collections.Generic;
using Zenject;
using static Constants;

#if UNITY_EDITOR

/// <summary>
/// 二者択一項目の確認画面の基底クラス
/// </summary>
public class ConfirmStateBaseEditor : EditorStateBase
{
    protected enum ConfirmTag
    {
        YES = 0,
        NO,

        NUM
    }

    protected CommandList _commandList = new CommandList();
    protected CommandList.CommandIndexedValue _cmdIdxVal;
    protected Frontier.ConfirmUI _confirmUi;

    override public void Init()
    {
        base.Init();

        _cmdIdxVal = new CommandList.CommandIndexedValue( 1, 1 );

        List<int> commandIndexs = new List<int>( ( int ) ConfirmTag.NUM );
        for( int i = 0; i < ( int ) ConfirmTag.NUM; ++i )
        {
            commandIndexs.Add( i );
        }
        _commandList.Init( ref commandIndexs, CommandList.CommandDirection.HORIZONTAL, true, _cmdIdxVal );

        _confirmUi = _uiSystem.DebugUi.StageEditorView.GetConfirmSaveLoadUI();
        _confirmUi.gameObject.SetActive( true );
        _confirmUi.Init();
    }

    override public bool Update()
    {
        if( base.Update() )
        {
            return true;
        }

        _confirmUi.ApplyTextColor( _commandList.GetCurrentValue() );

        return IsBack();
    }

    override public void ExitState()
    {
        _confirmUi.gameObject.SetActive( false );

        base.ExitState();
    }

    /// <summary>
    /// 入力コードを登録します
    /// </summary>
    override public void RegisterInputCodes()
    {
        int hashCode = GetInputCodeHash();

        // 入力ガイドを登録
        _inputFcd.RegisterInputCodes(
           (GuideIcon.HORIZONTAL_CURSOR, "Select", CanAcceptDirection, new AcceptDirectionInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, hashCode),
           (GuideIcon.CONFIRM, "Confirm", CanAcceptDefault, new AcceptBooleanInput( AcceptConfirm ), 0.0f, hashCode),
           (GuideIcon.CANCEL, "Back", CanAcceptDefault, new AcceptBooleanInput( AcceptCancel ), 0.0f, hashCode)
        );
    }
}

#endif  // UNITY_EDITOR