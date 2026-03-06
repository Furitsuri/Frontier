using Frontier.Combat;
using Frontier.Entities;
using Frontier.Stage;
using Frontier.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Constants;

namespace Frontier.Battle
{
    public class PlSkillActionToTargetState : PlPhaseStateBase
    {
        private enum TransitTag
        {
            CHARACTER_STATUS = 0,
        }

        private int skillRange = 0;
        private SkillsData.Data _usingSkillData;

        public override void Init()
        {
            base.Init();

            // パラメータビューにキャラクターを割り当て
            _presenter.AssignCharacterToParameterView( _plOwner, UI.ParameterWindowType.Left );

            _usingSkillData = _plOwner.BattleLogic.GetUsingActionSkillData();
            skillRange      = ( int ) _usingSkillData.Param1;

        }

        public override bool Update()
        {
            _presenter.UpdateParameterView( ParameterWindowType.Left );

            if( base.Update() )
            {
                return true;
            }

            return false;
        }

        public override void ExitState()
        {
            _stageCtrl.ApplyCurrentGrid2CharacterTile( _plOwner );  // グリッドカーソルの位置をプレイヤーの位置に合わせる

            base.ExitState();
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            // 入力ガイドを登録
            _inputFcd.RegisterInputCodes(
                (GuideIcon.ALL_CURSOR,  "SELECT\nTILE", CanAcceptDirection, new AcceptContextInput( AcceptDirection ), GRID_DIRECTION_INPUT_INTERVAL, hashCode),
                (GuideIcon.CONFIRM,     "CONFIRM", CanAcceptConfirm, new AcceptContextInput( AcceptConfirm ), 0.0f, hashCode),
                (GuideIcon.CANCEL,      "BACK", CanAcceptCancel, new AcceptContextInput( AcceptCancel ), 0.0f, hashCode),
                (GuideIcon.INFO,        "STATUS", CanAcceptInfo, new AcceptContextInput( AcceptInfo ), 0.0f, hashCode)
            // (GuideIcon.SUB1, _playerSkillNames[0], CanAcceptSub1, new AcceptContextInput( AcceptSub1 ), 0.0f, hashCode),
            // (GuideIcon.SUB2, _playerSkillNames[1], CanAcceptSub2, new AcceptContextInput( AcceptSub2 ), 0.0f, hashCode),
            // (GuideIcon.SUB3, _playerSkillNames[2], CanAcceptSub3, new AcceptContextInput( AcceptSub3 ), 0.0f, hashCode),
            // (GuideIcon.SUB4, _playerSkillNames[3], CanAcceptSub4, new AcceptContextInput( AcceptSub4 ), 0.0f, hashCode)
            );
        }

        protected override void AdaptSelectPlayer()
        {
            // グリッドカーソルで選択中のプレイヤーを取得
            LazyInject.GetOrCreate( ref _plOwner, () => _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() as Player );
        }

        protected override bool CanAcceptDirection()
        {
            if( !CanAcceptDefault() ) { return false; }

            return true;
        }

        protected override bool CanAcceptConfirm()
        {
            return false;
        }

        protected override bool CanAcceptCancel()
        {
            return true;
        }

        protected override bool CanAcceptInfo()
        {
            if( !CanAcceptDefault() ) { return false; }

            // 自身以外のキャラクターが選択されていない場合は不可
            if( _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() == null || _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() == _plOwner )
            {
                return false;
            }

            return true;
        }

        protected override bool AcceptDirection( InputContext context )
        {
            bool isAcceptDirection = _stageCtrl.OperateGridCursorController( context.Cursor );

            /*
            if( isAcceptDirection )
            {
                var gridSelectChara = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
                _isActiveRightParamUI = ( gridSelectChara != null && gridSelectChara != _plOwner );
                if( _isActiveRightParamUI )
                {
                    _presenter.AssignCharacterToParameterView( gridSelectChara, UI.ParameterWindowType.Right );
                }
                _presenter.SetActiveParamView( _isActiveRightParamUI, UI.ParameterWindowType.Right );
            }
            */

            return isAcceptDirection;
        }

        /// <summary>
        /// 決定入力を受けた際は選択した地点に移動するか、選択した場でそのまま攻撃へ遷移します
        /// </summary>
        /// <param name="isConfirm">決定入力</param>
        /// <returns>決定入力実行の有無</returns>
        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            return true;
        }

        /// <summary>
        /// キャンセル入力を受けた際は巻き戻し処理を行います
        /// </summary>
        /// <param name="isCancel">キャンセル入力</param>
        /// <returns>キャンセル入力実行の有無</returns>
        protected override bool AcceptCancel( InputContext context )
        {
            if( !base.AcceptCancel( context ) ) { return false; }

            return true;
        }

        protected override bool AcceptInfo( InputContext context )
        {
            if( !base.AcceptInfo( context ) ) { return false; }

            Handler.ReceiveContext( _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() );

            TransitState( ( int ) TransitTag.CHARACTER_STATUS );

            return true;
        }
    }
}