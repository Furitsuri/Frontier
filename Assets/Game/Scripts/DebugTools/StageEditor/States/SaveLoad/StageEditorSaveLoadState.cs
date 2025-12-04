using Frontier.DebugTools.StageEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Constants;
using Zenject;

namespace Frontier.DebugTools
{
    public class StageEditorSaveLoadState : ConfirmStateBaseEditor
    {
        [Inject] private readonly StageEditorController _stgEditorCtrl = null;

        protected enum State
        {
            NONE = -1,
            CONFIRM,
            NOTIFY,

            NUM,
        }

        protected string[] _confirmMessage = new string[( int ) State.NUM];
        protected string _failedMessage;
        private State _currentState;
        private Vector2 _confirmWinSize = new Vector2( STAGE_EDITOR_CONFIRM_WIN_WIDTH, STAGE_EDITOR_CONFIRM_WIN_HEIGHT );
        private Vector2 _NotifyWinSize  = new Vector2( STAGE_EDITOR_NOTIFY_WIN_WIDTH, STAGE_EDITOR_NOTIFY_WIN_HEIGHT );
        private Func<string, bool> SaveLoadStageCallback;
        private Action<string> SetMessageCallback;

        public void SetCallbacks( Func<string, bool> saveloadStageCb, Action<string> setMsgCallback )
        {
            SaveLoadStageCallback   = saveloadStageCb;
            SetMessageCallback      = setMsgCallback;
        }

        private void ToggleConfirmState()
        {
            _currentState = State.CONFIRM;
            SetMessageCallback( _confirmMessage[( int ) State.CONFIRM] );   // メッセージを確認中に設定
            _uiSystem.DebugUi.StageEditorView.RefreshConfirmWindowSize( _confirmWinSize );
        }

        private void ToggleNotifyState( bool isSucceeded )
        {
            _currentState = State.NOTIFY;
            _confirmUi.SetActiveAlternativeText( false );                                                   // 選択肢テキストを非表示に設定
            SetMessageCallback( isSucceeded ? _confirmMessage[( int ) State.NOTIFY] : _failedMessage );     // 通知メッセージを設定
            _uiSystem.DebugUi.StageEditorView.RefreshConfirmWindowSize( _NotifyWinSize );
        }

        override public void Init()
        {
            ToggleConfirmState();

            base.Init();
        }

        override protected bool CanAcceptDirection()
        {
            // 現在のステートが確認中でない場合は入力を受け付けない
            if( State.CONFIRM != _currentState ) { return false; }

            return true;
        }

        protected override bool AcceptDirection( Direction dir )
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
            if( !isInput ) { return false; }

            // 現在のステートに応じた処理を行う
            switch( _currentState )
            {
                case State.CONFIRM:
                    {
                        if( _commandList.GetCurrentValue() == ( int ) ConfirmTag.YES )
                        {
                            ToggleNotifyState( SaveLoadStageCallback( _stgEditorCtrl.EditFileName.Value ) );
                        }
                        else { Back(); }
                    }
                    break;
                case State.NOTIFY:
                    {
                        Back();
                    }
                    break;
            }

            return true;
        }

        protected override bool AcceptCancel( bool isCancel )
        {
            if( !isCancel ) { return false; }

            Back();

            return true;
        }
    }
}