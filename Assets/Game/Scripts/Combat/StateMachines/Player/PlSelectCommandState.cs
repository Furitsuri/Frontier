using Frontier.Combat;
using Frontier.Entities;
using Frontier.UI;
using System.Collections.Generic;
using static Constants;

namespace Frontier.Battle
{
    public class PlSelectCommandState : PlPhaseStateBase, ICommandCursorProvider
    {
        private CommandList _commandList = new CommandList();
        private CommandList.CommandIndexedValue _cmdIdxVal;

        /// <summary>
        /// 初期化します
        /// </summary>
        public override void Init( object context )
        {
            base.Init( context );

            // 可能な行動が全て終了している場合は即終了
            if( _plOwner.BattleParams.TmpParam.IsEndAction() )
            {
                _plOwner.BattleLogic.ActionRangeCtrl.ClearActionableRangeDataWithRender();
                return;
            }
            // 使用可能スキルを更新
            _plOwner.RefreshUseableSkillFlags( SituationType.ATTACK, 0xff );
            // 実行可能なコマンドを設定
            SetupExecutableCommands();

            // 現在選択中のコマンドに応じた範囲を表示する
            RefreshCommandRangeDisplay();
        }

        /// <summary>
        /// 更新します
        /// </summary>
        /// <returns>0以上の値のとき次の状態に遷移します</returns>
        public override bool Update()
        {
            if( _plOwner.BattleParams.TmpParam.IsEndAction() )
            {
                Back();
                return true;
            }

            // IsBackの判定を行うため、base.Updateは最後に呼び出す
            if( base.Update() )
            {
                return true;
            }

            return ( 0 <= TransitIndex );
        }

        /// <summary>
        /// 現在のステートから離脱します
        /// </summary>
        public override object ExitState()
        {
            // 移動コマンドを選択した場合は、この時点でのキャラクターの位置情報を保存する
            // ( PlMoveStateのInitなどで保存すると、『移動ステート中に敵を直接攻撃→攻撃をキャンセルして移動に戻る』とした場合に、
            //   移動ステートに戻った時点で位置情報が再保存されてしまうため、ここで処理する )
            if( TransitIndex == ( int ) COMMAND_TAG.MOVE )
            {
                _plOwner.HoldBeforeMoveInfo();
            }

            _plOwner.BattleLogic.ActionRangeCtrl.ClearActionableRangeDataWithRender();
            _presenter.ExitPLCommandView();

            return base.ExitState();
        }

        /// <summary>
        /// 入力コードを登録します
        /// </summary>
        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (GuideIcon.VERTICAL_CURSOR,  "Select",   CanAcceptDefault, new AcceptContextInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.CONFIRM,          "Confirm",  CanAcceptDefault, new AcceptContextInput( AcceptConfirm ), 0.0f, hashCode),
               (GuideIcon.CANCEL,           "Back",     CanAcceptDefault, new AcceptContextInput( AcceptCancel ), 0.0f, hashCode)
            );
        }

        /// <summary>
        /// 入力を受付るかを取得します。
        /// PlSelectSkillStateをキャンセルで戻った直後は、SkillBoxUIの元位置への復帰アニメーションが
        /// 再生中の場合があるため、その間は入力を受け付けない
        /// </summary>
        protected override bool CanAcceptDefault()
        {
            if( _presenter.IsSkillBoxLayoutAnimating ) { return false; }

            return base.CanAcceptDefault();
        }

        /// <summary>
        /// 操作対象のプレイヤーを設定します
        /// </summary>
        protected override void AdaptSelectPlayer()
        {
            // グリッドカーソルで選択中のプレイヤーを取得
            var selectCharacter = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            _plOwner            = _btlRtnCtrl.BtlCharaCdr.GetPlayer( selectCharacter.GetCharacterKey() );
            NullCheck.AssertNotNull( _plOwner, nameof( _plOwner ) );
        }

        protected override void OnActivated()
        {
            base.OnActivated();

            // パラメータビューにキャラクターを割り当て
            var layerMaskIndex = BattleRoutinePresenter.GetLayerMaskIndexFromWinType( ParameterWindowType.Left );
            _presenter.CharaParamView( ParameterWindowType.Left ).AssignCharacter( _plOwner, layerMaskIndex );

            // 子ステート(PlMoveState等)からBack()で復帰した場合、子ステート側で表示していた範囲描画が
            // 残っている、または消えている場合があるため、選択中コマンドに応じて表示を仕切り直す。
            // ただし行動が全て終了している場合はInit()でコマンドリストが構築されておらず
            // GetCommandValue()が参照できないため対象外とする
            if( !_plOwner.BattleParams.TmpParam.IsEndAction() )
            {
                RefreshCommandRangeDisplay();
            }
        }

        /// <summary>
        /// 現在選択中のコマンドに応じて行動可能範囲の表示を切り替えます。
        /// MOVEなら移動+攻撃範囲、ATTACKなら攻撃範囲のみ、SKILL・WAIT等はレンジという概念を持たないため
        /// 何も表示しません(スキルの効果範囲はSKILL選択時点では未確定のため)。
        /// </summary>
        private void RefreshCommandRangeDisplay()
        {
            var actionRangeCtrl = _plOwner.BattleLogic.ActionRangeCtrl;
            actionRangeCtrl.ClearActionableRangeDataWithRender();

            int dprtIdx = _plOwner.BattleParams.TmpParam.CurrentTileIndex;
            switch( ( COMMAND_TAG ) GetCommandValue() )
            {
                case COMMAND_TAG.MOVE:
                    float dprtHeight = _stageCtrl.GetTileStaticData( dprtIdx ).Height;
                    actionRangeCtrl.SetupActionableRangeData( dprtIdx, dprtHeight );
                    actionRangeCtrl.DrawActionableRange();
                    break;

                case COMMAND_TAG.ATTACK:
                    actionRangeCtrl.SetupAttackableRangeData( dprtIdx );
                    actionRangeCtrl.DrawAttackableRange();
                    break;
            }
        }

        /// <summary>
        /// 決定入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isConfirm">決定入力</param>
        /// <returns>入力実行の有無</returns>
        protected override bool AcceptDirection( InputContext context )
        {
            bool isChanged = _commandList.OperateListCursor( context.Cursor );

            if( isChanged )
            {
                RefreshCommandRangeDisplay();
            }

            return isChanged;
        }

        /// <summary>
        /// 決定入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isConfirm">決定入力</param>
        /// /// <returns>決定入力の有無</returns>
        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            TransitStateWithExit( GetCommandValue() );

            return true;
        }

        /// <summary>
        /// キャンセル入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isCancel">キャンセル入力の有無</param>
        protected override bool AcceptCancel( InputContext context )
        {
            if( !base.AcceptCancel( context ) ) { return false; }

            // 以前の状態に巻き戻せる場合は状態を巻き戻す
            if( _plOwner.IsEnableRevertState() )
            {
                RevertCommandHistory();
                // コマンド選択状態を終了させる必要はないため、初期化した後にfalseを返す
                Init( null );
                return false;
            }

            return true;
        }

        public int GetCurrentIndex() => _cmdIdxVal.index;

        /// <summary>
        /// 現在のコマンドのValue値を取得します
        /// </summary>
        /// <returns>コマンドのValue値</returns>
        public int GetCommandValue()
        {
            return _cmdIdxVal.value;
        }

        /// <summary>
        /// 実行可能なコマンドを設定します
        /// </summary>
        private void SetupExecutableCommands()
        {
            _cmdIdxVal = new CommandList.CommandIndexedValue( 0, 0 );

            // UI側へこのスクリプトを登録し、UIを表示
            List<COMMAND_TAG> executableCommands;
            _plOwner.BattleLogic.FetchExecutableCommand( out executableCommands, _stageCtrl );

            // 入力ベース情報の設定
            List<int> commandIndices = new List<int>();
            foreach( var executableCmd in executableCommands )
            {
                commandIndices.Add( ( int ) executableCmd );
            }
            _commandList.Init( ref commandIndices, CommandList.CommandDirection.VERTICAL, false, _cmdIdxVal );

            _presenter.InitPLCommandView( this, executableCommands );
        }
    }
}