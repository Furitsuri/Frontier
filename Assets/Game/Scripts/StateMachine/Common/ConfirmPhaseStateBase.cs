using Frontier.Entities;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static Constants;

namespace Frontier.StateMachine
{
    /// <summary>
    /// 二者択一項目の確認画面の基底クラス
    /// </summary>
    public class ConfirmPhaseStateBase : PhaseStateBase
    {
        protected enum ConfirmTag
        {
            YES = 0,
            NO,

            NUM
        }

        protected IConfirmPresenter _confirmPresenter   = null;
        protected CommandList _commandList              = new CommandList();
        protected CommandList.CommandIndexedValue _cmdIdxVal;

        public override void Init()
        {
            base.Init();

            _cmdIdxVal = new CommandList.CommandIndexedValue( 1, 1 );

            List<int> commandIndexs = new List<int>( ( int ) ConfirmTag.NUM );
            for( int i = 0; i < ( int ) ConfirmTag.NUM; ++i )
            {
                commandIndexs.Add( i );
            }
            _commandList.Init( ref commandIndexs, CommandList.CommandDirection.HORIZONTAL, true, _cmdIdxVal );

            _confirmPresenter.SetActiveConfirmUI( true );
        }

        public override bool Update()
        {
            if( base.Update() )
            {
                return true;
            }

            _confirmPresenter.ApplyColor2Options( _commandList.GetCurrentValue() );

            return IsBack();
        }

        public override void ExitState()
        {
            _confirmPresenter.SetActiveConfirmUI( false );

            base.ExitState();
        }

        public override void AssignPresenter( PhasePresenterBase presenter )
        {
            _confirmPresenter = presenter as IConfirmPresenter;
        }

        /// <summary>
        /// 入力コードを登録します
        /// </summary>
        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            // 入力ガイドを登録
            _inputFcd.RegisterInputCodes(
               (GuideIcon.HORIZONTAL_CURSOR, "Select", CanAcceptDefault, new AcceptContextInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.CONFIRM, "Confirm", CanAcceptDefault, new AcceptContextInput( AcceptConfirm ), 0.0f, hashCode),
               (GuideIcon.CANCEL, "Back", CanAcceptDefault, new AcceptContextInput( AcceptCancel ), 0.0f, hashCode)
            );
        }

        protected override bool AcceptDirection( InputContext context )
        {
            return _commandList.OperateListCursor( context.Cursor );
        }

        protected override bool AcceptCancel( InputContext context )
        {
            if( !base.AcceptCancel( context ) ) { return false; }

            Back();

            return true;
        }
    }
}