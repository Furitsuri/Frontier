using Frontier.Battle;
using Frontier.Combat;
using Frontier.Combat.Skill;
using Frontier.Entities;
using Frontier.Sequences;
using Frontier.Stage;
using Frontier.UI;
using System.Collections.Generic;
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

        private CommandList _commandList = new CommandList();
        private CommandList.CommandIndexedValue _cmdIdxVal;

        public override void Init( object context )
        {
            base.Init( context);

            _transitTargetSelectSkillID     = SkillID.NONE;
            _playerSkillNames               = _plOwner.GetStatusRef.GetEquipSkillNames();

            // 装備されているスキルの枠のみをカーソル移動対象にする
            var equippedIndices = new List<int>();
            for( int i = 0; i < EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                if( 0 < _playerSkillNames[i].Length ) { equippedIndices.Add( i ); }
            }

            _cmdIdxVal = new CommandList.CommandIndexedValue( 0, 0 );
            _commandList.Init( ref equippedIndices, CommandList.CommandDirection.VERTICAL, false, _cmdIdxVal );

            // 使用可能スキルの更新(このスキル選択画面内でのみ、対象不在のスキルを使用不可として扱う)
            _plOwner.RefreshUseableSkillFlags( Combat.SituationType.ATTACK, 0xff, checkAttackTarget: true );

            // SkillBoxUIを選択用の縦一列レイアウトへアニメーション移動
            _presenter.AnimateSkillBoxesForSelection( ParameterWindowType.Left );
            _presenter.SetSkillBoxCursorIndex( ParameterWindowType.Left, _cmdIdxVal.value );

            // カーソル位置のスキルに応じた効果範囲を表示する
            RefreshSkillRangeDisplay();
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
            // カーソルハイライトを解除する
            _presenter.SetSkillBoxCursorIndex( ParameterWindowType.Left, -1 );
            // 効果範囲表示を消去する(AcceptCancel以外の退避経路でも確実にクリアするため)
            _plOwner.BattleLogic.ActionRangeCtrl.ClearActionableRangeDataWithRender();

            return base.ExitState();
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            // 入力ガイドを登録
            _inputFcd.RegisterInputCodes(
               (GuideIcon.VERTICAL_CURSOR, "SELECT",  CanAcceptDirection, new AcceptContextInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.CONFIRM,         "CONFIRM", CanAcceptConfirm,   new AcceptContextInput( AcceptConfirm ),   0.0f, hashCode),
               (GuideIcon.CANCEL,          "BACK",    CanAcceptCancel,    new AcceptContextInput( AcceptCancel ),    0.0f, hashCode),
               (GuideIcon.SUB1,            "TOGGLE",  CanAcceptSub1,      new AcceptContextInput( AcceptSub1 ),      0.0f, hashCode)
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

            // 子ステート(PlSkillActionToTargetState)からBack()で復帰した場合、範囲描画が消えている
            // 場合があるため、カーソル位置のスキルに応じて表示を仕切り直す
            RefreshSkillRangeDisplay();
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

        protected override bool CanAcceptDirection()
        {
            if( _phase != PlSelectSkillPhase.PL_SELECT_SKILL ) { return false; }
            if( _presenter.IsSkillBoxLayoutAnimating ) { return false; }

            return true;
        }

        protected override bool CanAcceptSub1()
        {
            if( _phase != PlSelectSkillPhase.PL_SELECT_SKILL ) { return false; }
            if( _presenter.IsSkillBoxLayoutAnimating ) { return false; }

            return _plOwner.BattleParams.TmpParam.IsUseableSkill[_cmdIdxVal.value];
        }

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

        protected override bool AcceptDirection( InputContext context )
        {
            bool moved = _commandList.OperateListCursor( context.Cursor );

            if( moved )
            {
                _presenter.SetSkillBoxCursorIndex( ParameterWindowType.Left, _cmdIdxVal.value );
                RefreshSkillRangeDisplay();
            }

            return moved;
        }

        /// <summary>
        /// カーソルが指しているスキルに応じて効果範囲の表示を切り替えます。
        /// ターゲット選択に遷移するタイプのスキル(攻撃系)は効果範囲を表示し、
        /// 自己バフ等それ以外のスキルは範囲という概念を持たないため何も表示しません。
        /// ON/OFFの切替状態には関わらず、あくまでカーソル位置のプレビューとして表示します。
        /// </summary>
        private void RefreshSkillRangeDisplay()
        {
            var actionRangeCtrl = _plOwner.BattleLogic.ActionRangeCtrl;
            actionRangeCtrl.ClearActionableRangeDataWithRender();

            var skillID = _plOwner.GetEquipSkillID( _cmdIdxVal.value );
            if( SkillID.NONE == skillID ) { return; }

            var skillData = SkillsData.data[( int ) skillID];
            if( !SkillsData.IsTransitionSkillActionType( skillData.ActionType ) ) { return; }

            actionRangeCtrl.SetupAttackableRangeData( _plOwner.BattleParams.TmpParam.CurrentTileIndex, skillID );
            actionRangeCtrl.DrawAttackableRange();
        }

        protected override bool AcceptSub1( InputContext context )
        {
            if( !base.AcceptSub1( context ) ) { return false; }

            int index                           = _cmdIdxVal.value;
            var skillID                         = _plOwner.GetEquipSkillID( index );
            var skillData                       = SkillsData.data[( int ) skillID];
            bool isTransitionSkillActionType    = SkillsData.IsTransitionSkillActionType( skillData.ActionType );

            _plOwner.BattleLogic.ToggleEquipSkill( index );
            // 使用可能スキルの更新(このスキル選択画面内でのみ、対象不在のスキルを使用不可として扱う)
            _plOwner.RefreshUseableSkillFlags( SituationType.ATTACK, useableActionTypeBit : 0xff, checkAttackTarget : true );

            // 場面遷移が必要なアクションスキルが選択されている場合
            if( isTransitionSkillActionType )
            {
                _transitTargetSelectSkillID = SkillID.NONE;

                // 効果範囲の表示自体はカーソル位置に応じてRefreshSkillRangeDisplay()で既に描画済みのため、
                // ここではスキル使用フラグが立っている場合に、その範囲内に攻撃対象が存在するかどうかのみ判定する
                if( _plOwner.BattleParams.TmpParam.IsSkillsToggledON[index] )
                {
                    foreach( var data in _plOwner.BattleLogic.ActionRangeCtrl.ActionableTileData.AttackableTileMap )
                    {
                        if( Methods.HasAnyFlag( data.Value.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST ) )
                        {
                            _transitTargetSelectSkillID = skillID;
                            break;
                        }
                    }
                }
            }

            return true;
        }
    }
}