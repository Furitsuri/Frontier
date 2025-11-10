using Frontier.Entities;
using Frontier.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Constants;

namespace Froniter.StateMachine
{
    /// <summary>
    /// 配置フェーズ：配置確定終了状態
    /// </summary>
    public class DeploymentConfirmCompletedState : DeploymentPhaseStateBase
    {
        private enum ConfirmTag
        {
            YES = 0,
            NO,

            NUM
        }

        private CommandList _commandList = new CommandList();
        private CommandList.CommandIndexedValue _cmdIdxVal;

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

            _presenter.SetActiveConfirmUis( true );
        }

        override public bool Update()
        {
            if( base.Update() )
            {
                return true;
            }

            _presenter.ApplyTextColor2ConfirmCompleted( _commandList.GetCurrentValue() );

            return IsBack();
        }

        override public void ExitState()
        {
            _presenter.SetActiveConfirmUis( false );

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
               (GuideIcon.HORIZONTAL_CURSOR, "Select", CanAcceptDefault, new AcceptDirectionInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.CONFIRM, "Confirm", CanAcceptDefault, new AcceptBooleanInput( AcceptConfirm ), 0.0f, hashCode),
               (GuideIcon.CANCEL, "Back", CanAcceptDefault, new AcceptBooleanInput( AcceptCancel ), 0.0f, hashCode)
            );
        }

        /// <summary>
        /// 方向入力を受け取り、コマンドリストを操作させます
        /// </summary>
        /// <param name="dir">方向入力</param>
        /// <returns>入力によってリストカーソルの位置が更新されたか</returns>
        override protected bool AcceptDirection( Direction dir )
        {
            return _commandList.OperateListCursor( dir );
        }

        /// <summary>
        /// 決定入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isConfirm">決定入力</param>
        /// /// <returns>決定入力の有無</returns>
        override protected bool AcceptConfirm( bool isInput )
        {
            if( !isInput ) return false;

            // 配置完了を確定させて配置フェーズを終了する
            if( _commandList.GetCurrentValue() == ( int ) ConfirmTag.YES )
            {
                _isEndedPhase = true;
            }

            Back();

            return true;
        }
    }
}