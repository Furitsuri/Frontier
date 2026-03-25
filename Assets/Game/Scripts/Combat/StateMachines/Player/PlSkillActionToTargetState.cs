using Frontier.Combat;
using Frontier.Entities;
using Frontier.Sequences;
using Frontier.Stage;
using Frontier.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Battle
{
    public class PlSkillActionToTargetState : PlPhaseStateBase
    {
        private enum TransitTag
        {
            CHARACTER_STATUS = 0,
        }

        private SkillID _useSkillID;
        private SkillsData.Data _usingSkillData;

        public override void Init( object context )
        {
            base.Init( context);

            // パラメータビューにキャラクターを割り当て
            var layerMaskIndex = BattleRoutinePresenter.GetLayerMaskIndexFromWinType( ParameterWindowType.Left );
            _presenter.CharaParamView( ParameterWindowType.Left ).AssignCharacter( _plOwner, layerMaskIndex );

            // 使用スキルから攻撃可能範囲を決定
            _plOwner.BattleLogic.ActionRangeCtrl.SetupAttackableRangeData( _plOwner.BattleParams.TmpParam.CurrentTileIndex, _useSkillID );

            ReceiveContext( ref _useSkillID, context );
            _usingSkillData = _plOwner.BattleLogic.GetUsingActionSkillData();
        }

        public override bool Update()
        {
            if( base.Update() )
            {
                return true;
            }

            return false;
        }

        public override object ExitState()
        {
            _stageCtrl.ApplyCurrentGrid2CharacterTile( _plOwner );  // グリッドカーソルの位置をプレイヤーの位置に合わせる

            return base.ExitState();
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
                    _presenter.AssignCharacter( gridSelectChara, UI.ParameterWindowType.Right );
                }
                _presenter.SetActiveParamView( _isActiveRightParamUI, UI.ParameterWindowType.Right );
            }
            */

            return isAcceptDirection;
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            

            return true;
        }

        protected override bool AcceptCancel( InputContext context )
        {
            if( !base.AcceptCancel( context ) ) { return false; }

            return true;
        }

        /// <summary>
        /// 情報キー入力時、ステータス画面へ遷移します
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected override bool AcceptInfo( InputContext context )
        {
            if( !base.AcceptInfo( context ) ) { return false; }

            // ステータス表示ステートに対象キャラクターを渡す
            SetSendTransitionContext( _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() );

            TransitState( ( int ) TransitTag.CHARACTER_STATUS );

            return true;
        }
    }
}