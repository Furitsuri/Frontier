using Frontier.Battle;
using Frontier.Combat;
using Frontier.Combat.Skill;
using Frontier.Entities;
using Frontier.Sequences;
using Frontier.Stage;
using Frontier.UI;
using System;
using UnityEngine.TextCore.Text;
using Zenject;
using static Constants;

namespace Frontier.Battle
{
    public class PlSelectSkillState : PlPhaseStateBase
    {
        private enum PlSelectSkillPhase
        {
            PL_SELECT_SKILL = 0,
            PL_SELECT_SKILL_END,
        }

        private enum TransitTag
        {
            SKILL_ACTION_TO_TARGET = 0,
        }

        [Inject] private SequenceFacade _sequenceFcd = null;

        private SkillID _transitTargetSelectSkillID = SkillID.NONE;
        private string[] _playerSkillNames          = null;
        private PlSelectSkillPhase _phase = PlSelectSkillPhase.PL_SELECT_SKILL;
        private Func<InputContext, bool>[] AcceptSubs;

        public override void Init( object context )
        {
            base.Init( context);

            _transitTargetSelectSkillID     = SkillID.NONE;
            _playerSkillNames               = _plOwner.GetStatusRef.GetEquipSkillNames();

            AcceptSubs = new Func<InputContext, bool>[]
            {
                ( context ) => base.AcceptSub1( context ),
                ( context ) => base.AcceptSub2( context ),
                ( context ) => base.AcceptSub3( context ),
                ( context ) => base.AcceptSub4( context )
            };

            // 使用可能スキルの更新
            _plOwner.RefreshUseableSkillFlags( Combat.SituationType.ATTACK, 0xff );

            // SkillBoxUIを選択用の縦一列レイアウトへアニメーション移動
            _presenter.AnimateSkillBoxesForSelection( ParameterWindowType.Left );
        }

        public override bool Update()
        {
            if( base.Update() )
            {
                return true;
            }

            switch( _phase )
            {
                case PlSelectSkillPhase.PL_SELECT_SKILL:

                    break;
                case PlSelectSkillPhase.PL_SELECT_SKILL_END:
                    if( _sequenceFcd.IsEmptySequence() )
                    {
                        Back();

                        return true;
                    }
                    break;
            }

            return false;
        }

        public override object ExitState()
        {
            return base.ExitState();
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            // 入力ガイドを登録
            _inputFcd.RegisterInputCodes(
               (GuideIcon.CONFIRM, "CONFIRM", CanAcceptConfirm, new AcceptContextInput( AcceptConfirm ), 0.0f, hashCode),
               (GuideIcon.CANCEL, "BACK", CanAcceptCancel, new AcceptContextInput( AcceptCancel ), 0.0f, hashCode),
               (GuideIcon.SUB1, _playerSkillNames[0], CanAcceptSub1, new AcceptContextInput( AcceptSub1 ), 0.0f, hashCode),
               (GuideIcon.SUB2, _playerSkillNames[1], CanAcceptSub2, new AcceptContextInput( AcceptSub2 ), 0.0f, hashCode),
               (GuideIcon.SUB3, _playerSkillNames[2], CanAcceptSub3, new AcceptContextInput( AcceptSub3 ), 0.0f, hashCode),
               (GuideIcon.SUB4, _playerSkillNames[3], CanAcceptSub4, new AcceptContextInput( AcceptSub4 ), 0.0f, hashCode)
            );
        }

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

            // スキルを既に実行している状態でこのフェーズに遷移してきた場合は、キャンセル扱いで前の状態に戻る
            if( _plOwner.BattleParams.TmpParam.IsEndAction() )
            {
                Back();
                return;
            }

            _phase = PlSelectSkillPhase.PL_SELECT_SKILL;

            // パラメータビューにキャラクターを割り当て
            var layerMaskIndex = BattleRoutinePresenter.GetLayerMaskIndexFromWinType( ParameterWindowType.Left );
            _presenter.CharaParamView( ParameterWindowType.Left ).AssignCharacter( _plOwner, layerMaskIndex );

            // 遷移スキルが設定されていれば、キャンセル後に攻撃範囲を再描画する
            if( _transitTargetSelectSkillID != SkillID.NONE )
            {
                _plOwner.BattleLogic.ActionRangeCtrl.SetupAttackableRangeData( _plOwner.BattleParams.TmpParam.CurrentTileIndex, _transitTargetSelectSkillID );
                _plOwner.BattleLogic.ActionRangeCtrl.DrawAttackableRange();
            }
        }

        protected override bool CanAcceptConfirm()
        {
            if( _phase != PlSelectSkillPhase.PL_SELECT_SKILL ) { return false; }
            if( _presenter.IsSkillBoxLayoutAnimating ) { return false; }

            bool isExistTransitionSkillActionType = false;
            bool isExistToggledOnSkill = false;

            // 所有スキルのうち、どれか一つでも使用フラグが立っていれば決定入力を受け付ける
            for( int i = 0; i < EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                if( _plOwner.BattleLogic.IsEquipSkillToggledOn( i ) )
                {
                    var skillID     = _plOwner.GetEquipSkillID( i );
                    var skillData = SkillsData.data[( int ) skillID];

                    // ただし、ターゲット選択に遷移するスキルタイプのものがある場合は、そちらを優先して遷移させる
                    // ターゲット選択に遷移するスキルタイプのものがある場合は、他のスキルの使用フラグが立っていても攻撃可能な範囲にターゲットが存在しない可能性があるため、そちらをチェック
                    bool isTransitionSkillActionType = SkillsData.IsTransitionSkillActionType( skillData.ActionType );
                    if( isTransitionSkillActionType )
                    {
                        isExistTransitionSkillActionType = true;
                    }
                    else
                    {
                        isExistToggledOnSkill = true;
                    }
                }
            }

            if( isExistTransitionSkillActionType ) { return _transitTargetSelectSkillID != SkillID.NONE; }

            return isExistToggledOnSkill;
        }

        protected override bool CanAcceptCancel()
        {
            if( _phase != PlSelectSkillPhase.PL_SELECT_SKILL ) { return false; }
            if( _presenter.IsSkillBoxLayoutAnimating ) { return false; }

            return true;
        }

        protected override bool CanAcceptSub1() => CanAcceptSub( 0 );
        protected override bool CanAcceptSub2() => CanAcceptSub( 1 );
        protected override bool CanAcceptSub3() => CanAcceptSub( 2 );
        protected override bool CanAcceptSub4() => CanAcceptSub( 3 );

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            // ターゲット選択に遷移するスキルタイプのものがONになっている場合は、ターゲット選択に遷移
            // それ以外の場合(自身へのバフスキル等)は、スキル使用フラグが立っているスキルの消費分だけ行動ゲージを減らす
            if( SkillID.NONE == _transitTargetSelectSkillID )
            {
                // この場合はターゲット選択やオプション選択を挟まずそのまま実行が確定するため、この時点でSkillBoxUIを元の位置に戻す
                _presenter.RevertSkillBoxesFromSelection( ParameterWindowType.Left );

                // スキル使用フラグが立っているスキルの消費分だけ行動ゲージを減らす
                // (一時保存パラメータに保存することで、キャンセルによって消費前に戻せる形に)
                _plOwner.BattleLogic.ConsumeActionGaugeForSkill();
                // スキルを使用した場合は以前の状態には戻せないため、行動履歴をクリア
                _plOwner.ClearCommandHistory();
            }
            else
            {
                // ターゲット選択に遷移するスキルタイプのものがONになっている場合は、ターゲット選択に遷移
                SetSendTransitionContext( _transitTargetSelectSkillID );

                TransitState( ( int ) TransitTag.SKILL_ACTION_TO_TARGET );
            }

            _phase = PlSelectSkillPhase.PL_SELECT_SKILL_END;

            return true;
        }

        protected override bool AcceptCancel( InputContext context )
        {
            if( !base.AcceptCancel( context ) ) { return false; }

            _plOwner.BattleLogic.RevertSkillsToggledOn();                               // 全てのスキルの使用フラグをOFFにする
            _plOwner.BattleLogic.ActionRangeCtrl.ClearActionableRangeDataWithRender();  // 攻撃可能範囲の描画と範囲データをクリアする
            _presenter.RevertSkillBoxesFromSelection( ParameterWindowType.Left );       // SkillBoxUIを元の位置に戻す

            return true;
        }

        protected override bool AcceptSub1( InputContext context ) => AcceptSub( 0, context );
        protected override bool AcceptSub2( InputContext context ) => AcceptSub( 1, context );
        protected override bool AcceptSub3( InputContext context ) => AcceptSub( 2, context );
        protected override bool AcceptSub4( InputContext context ) => AcceptSub( 3, context );

        private bool CanAcceptSub( int index )
        {
            if( _phase != PlSelectSkillPhase.PL_SELECT_SKILL )      { return false; }
            if( _presenter.IsSkillBoxLayoutAnimating )              { return false; }
            if( _playerSkillNames[index].Length <= 0 )              { return false; }

            return _plOwner.BattleParams.TmpParam.IsUseableSkill[index];
        }

        private bool AcceptSub( int index, InputContext context )
        {
            if( !AcceptSubs[index]( context ) ) { return false; }

            var skillID                         = _plOwner.GetEquipSkillID( index );
            var skillData                       = SkillsData.data[( int ) skillID];
            bool isTransitionSkillActionType    = SkillsData.IsTransitionSkillActionType( skillData.ActionType );

            _plOwner.BattleLogic.ToggleEquipSkill( index );
            _plOwner.RefreshUseableSkillFlags( SituationType.ATTACK, useableActionTypeBit : 0xff );  // 使用可能スキルの更新

            // 場面遷移が必要なアクションスキルが選択されている場合
            if( isTransitionSkillActionType )
            {
                // スキル使用フラグが立っている場合はその攻撃範囲などを描画し、
                // その範囲内に攻撃対象が存在している場合のみ、遷移可能なスキルとして保持する
                if( _plOwner.BattleParams.TmpParam.IsSkillsToggledON[index] )
                {
                    _plOwner.BattleLogic.ActionRangeCtrl.SetupAttackableRangeData( _plOwner.BattleParams.TmpParam.CurrentTileIndex, skillID );
                    _plOwner.BattleLogic.ActionRangeCtrl.DrawAttackableRange();

                    foreach( var data in _plOwner.BattleLogic.ActionRangeCtrl.ActionableTileData.AttackableTileMap )
                    {
                        if( Methods.HasAnyFlag( data.Value.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST ) )
                        {
                            _transitTargetSelectSkillID = skillID;
                            break;
                        }
                    }
                }
                else
                {
                    _plOwner.BattleLogic.ActionRangeCtrl.ClearActionableRangeDataWithRender();
                    _transitTargetSelectSkillID = SkillID.NONE;

                }
            }

            return true;
        }
    }
}