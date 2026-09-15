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

        /// <summary>
        /// この確認画面の選択操作方式。既定は既存の左右十字キー方式。十字キーを他の操作
        /// (キャラクター選択等)に使いたいサブクラスはSubButtonsへオーバーライドしてください。
        /// </summary>
        protected virtual ConfirmUIType UIType => ConfirmUIType.HorizontalCursor;

        public override void Init( object context )
        {
            base.Init( context);

            _cmdIdxVal = new CommandList.CommandIndexedValue( 1, 1 );

            List<int> commandIndices = new List<int>( ( int ) ConfirmTag.NUM );
            for( int i = 0; i < ( int ) ConfirmTag.NUM; ++i )
            {
                commandIndices.Add( i );
            }
            _commandList.Init( ref commandIndices, CommandList.CommandDirection.HORIZONTAL, true, _cmdIdxVal );

            _confirmPresenter.SetActiveConfirmUI( true, UIType );
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

        public override object ExitState()
        {
            _confirmPresenter.SetActiveConfirmUI( false, UIType );

            return base.ExitState();
        }

        public override void AssignPresenter( PhasePresenterBase presenter )
        {
            _confirmPresenter = presenter as IConfirmPresenter;
        }

        /// <summary>
        /// 入力コードを登録します。UITypeがSubButtonsの場合、十字キーではなくSUB1/SUB2で
        /// Yes/Noを直接選択する入力コードを登録します。
        /// </summary>
        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            if( UIType == ConfirmUIType.SubButtons )
            {
                _inputFcd.RegisterInputCodes(
                   (GuideIcon.SUB1, "Yes", CanAcceptDefault, new AcceptContextInput( AcceptSub1 ), 0.0f, hashCode),
                   (GuideIcon.SUB2, "No", CanAcceptDefault, new AcceptContextInput( AcceptSub2 ), 0.0f, hashCode),
                   (GuideIcon.CONFIRM, "Confirm", CanAcceptDefault, new AcceptContextInput( AcceptConfirm ), 0.0f, hashCode),
                   (GuideIcon.CANCEL, "Back", CanAcceptDefault, new AcceptContextInput( AcceptCancel ), 0.0f, hashCode)
                );
            }
            else
            {
                _inputFcd.RegisterInputCodes(
                   (GuideIcon.HORIZONTAL_CURSOR, "Select", CanAcceptDefault, new AcceptContextInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, hashCode),
                   (GuideIcon.CONFIRM, "Confirm", CanAcceptDefault, new AcceptContextInput( AcceptConfirm ), 0.0f, hashCode),
                   (GuideIcon.CANCEL, "Back", CanAcceptDefault, new AcceptContextInput( AcceptCancel ), 0.0f, hashCode)
                );
            }
        }

        protected override bool AcceptDirection( InputContext context )
        {
            return _commandList.OperateListCursor( context.Cursor );
        }

        /// <summary>
        /// UIType.SubButtons時、Yes(左側)を直接選択します。
        /// </summary>
        protected override bool AcceptSub1( InputContext context )
        {
            if( !base.AcceptSub1( context ) ) { return false; }

            return _commandList.SetCurrentValue( ( int ) ConfirmTag.YES );
        }

        /// <summary>
        /// UIType.SubButtons時、No(右側)を直接選択します。
        /// </summary>
        protected override bool AcceptSub2( InputContext context )
        {
            if( !base.AcceptSub2( context ) ) { return false; }

            return _commandList.SetCurrentValue( ( int ) ConfirmTag.NO );
        }

        protected override bool AcceptCancel( InputContext context )
        {
            if( !base.AcceptCancel( context ) ) { return false; }

            Back();

            return true;
        }
    }
}