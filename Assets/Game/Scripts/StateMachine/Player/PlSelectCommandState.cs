using Frontier.Combat;
using Frontier.Entities;
using System.Collections.Generic;
using static Constants;

namespace Frontier.StateMachine
{
    public class PlSelectCommandState : PlPhaseStateBase
    {
        private CommandList _commandList = new CommandList();
        private CommandList.CommandIndexedValue _cmdIdxVal;

        /// <summary>
        /// 入力情報を初期化します
        /// </summary>
        private void InitInputInfo()
        {
            _cmdIdxVal = new CommandList.CommandIndexedValue( 0, 0 );

            // UI側へこのスクリプトを登録し、UIを表示
            List<COMMAND_TAG> executableCommands;
            _plOwner.FetchExecutableCommand( out executableCommands, _stageCtrl );

            // 入力ベース情報の設定
            List<int> commandIndexs = new List<int>();
            foreach( var executableCmd in executableCommands )
            {
                commandIndexs.Add( ( int ) executableCmd );
            }
            _commandList.Init( ref commandIndexs, CommandList.CommandDirection.VERTICAL, false, _cmdIdxVal );

            _uiSystem.BattleUi.PlCommandWindow.RegistPLCommandScript( this );
            _uiSystem.BattleUi.PlCommandWindow.SetExecutableCommandList( executableCommands );
            _uiSystem.BattleUi.SetPlayerCommandActive( true );
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        public override void Init()
        {
            base.Init();

            // 可能な行動が全て終了している場合は即終了
            if( _plOwner.Params.TmpParam.IsEndAction() )
            {
                return;
            }

            InitInputInfo();
        }

        /// <summary>
        /// 更新します
        /// </summary>
        /// <returns>0以上の値のとき次の状態に遷移します</returns>
        public override bool Update()
        {
            if( _plOwner.Params.TmpParam.IsEndAction() )
            {
                Back();
                return true;
            }

            // IsBackの判定を行うため、base.Updateは最後に呼び出す
            if( base.Update() )
            {
                // コマンドのうち、移動のみが終了している場合は移動前の状態に戻れるように          
                if( _plOwner.Params.TmpParam.IsEndCommand( COMMAND_TAG.MOVE ) && !_plOwner.Params.TmpParam.IsEndCommand( COMMAND_TAG.ATTACK ) )
                {
                    _stageCtrl.FollowFootprint( _plOwner );
                    _plOwner.Params.TmpParam.SetEndCommandStatus( COMMAND_TAG.MOVE, false );
                }

                return true;
            }

            return ( 0 <= TransitIndex );
        }

        /// <summary>
        /// 現在のステートから離脱します
        /// </summary>
        public override void ExitState()
        {
            // 移動コマンドを選択した場合は、この時点でのキャラクターの位置情報を保存する
            // ( PlMoveStateのInitなどで保存すると、『移動ステート中に敵を直接攻撃→攻撃をキャンセルして移動に戻る』とした場合に、
            //   移動ステートに戻った時点で位置情報が再保存されてしまうため、ここで処理する )
            if( TransitIndex == ( int ) COMMAND_TAG.MOVE )
            {
                _plOwner.HoldBeforeMoveInfo();
                _stageCtrl.HoldFootprint( _plOwner );  // キャラクターの現在の位置情報を保持
            }

            _uiSystem.BattleUi.SetPlayerCommandActive( false );  // UIを非表示

            base.ExitState();
        }

        /// <summary>
        /// 入力コードを登録します
        /// </summary>
        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (GuideIcon.VERTICAL_CURSOR, "Select", CanAcceptDefault, new AcceptDirectionInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.CONFIRM, "Confirm", CanAcceptDefault, new AcceptBooleanInput( AcceptConfirm ), 0.0f, hashCode),
               (GuideIcon.CANCEL, "Back", CanAcceptDefault, new AcceptBooleanInput( AcceptCancel ), 0.0f, hashCode)
            );
        }

        /// <summary>
        /// 操作対象のプレイヤーを設定します
        /// </summary>
        override protected void AdaptSelectPlayer()
        {
            // グリッドカーソルで選択中のプレイヤーを取得
            _plOwner = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() as Player;
            NullCheck.AssertNotNull( _plOwner, nameof( _plOwner ) );
        }

        /// <summary>
        /// 決定入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isConfirm">決定入力</param>
        /// <returns>入力実行の有無</returns>
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
            if( !isInput ) { return false; }

            TransitStateWithExit( GetCommandValue() );

            return true;
        }

        /// <summary>
        /// キャンセル入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isCancel">キャンセル入力の有無</param>
        override protected bool AcceptCancel( bool isCancel )
        {
            if( base.AcceptCancel( isCancel ) )
            {
                // 以前の状態に巻き戻せる場合は状態を巻き戻す
                if( _plOwner.IsRewindStatePossible() )
                {
                    Rewind();
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// 現在のコマンドのIndex値を取得します
        /// </summary>
        /// <returns>コマンドのIndex値</returns>
        public int GetCommandIndex()
        {
            return _cmdIdxVal.index;
        }

        /// <summary>
        /// 現在のコマンドのValue値を取得します
        /// </summary>
        /// <returns>コマンドのValue値</returns>
        public int GetCommandValue()
        {
            return _cmdIdxVal.value;
        }
    }
}