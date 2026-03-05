using Frontier.Battle;
using Frontier.Combat;
using Frontier.Combat.Skill;
using Frontier.Entities;
using Frontier.UI;
using static Constants;
using static Frontier.UI.BattleUISystem;

namespace Frontier.Battle
{
    public class PlSelectSkillState : PlPhaseStateBase
    {
        private enum PlSelectSkillPhase
        {
            PL_SELECT_SKILL = 0,
            PL_SELECT_SKILL_END,
        }

        private string[] _playerSkillNames = null;
        private PlSelectSkillPhase _phase = PlSelectSkillPhase.PL_SELECT_SKILL;

        public override void Init()
        {
            base.Init();

            _playerSkillNames   = _plOwner.GetStatusRef.GetEquipSkillNames();
            _phase              = PlSelectSkillPhase.PL_SELECT_SKILL;

            // パラメータビューにキャラクターを割り当て
            _presenter.AssignCharacterToParameterView( _plOwner, UI.ParameterWindowType.Left );
        }

        public override bool Update()
        {
            _presenter.UpdateParameterView( ParameterWindowType.Left );

            if( base.Update() )
            {
                return true;
            }

            switch( _phase )
            {
                case PlSelectSkillPhase.PL_SELECT_SKILL:

                    break;
                case PlSelectSkillPhase.PL_SELECT_SKILL_END:
                    break;
            }

            return false;
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            // 入力ガイドを登録
            _inputFcd.RegisterInputCodes(
               ( GuideIcon.CONFIRM, "CONFIRM",  CanAcceptConfirm,   new AcceptContextInput( AcceptConfirm ),    0.0f, hashCode ),
               ( GuideIcon.CANCEL,  "BACK",     CanAcceptCancel,    new AcceptContextInput( AcceptCancel ),     0.0f, hashCode ),
               ( GuideIcon.SUB1,    _playerSkillNames[0], CanAcceptSub1, new AcceptContextInput( AcceptSub1 ),  0.0f, hashCode ),
               ( GuideIcon.SUB2,    _playerSkillNames[1], CanAcceptSub2, new AcceptContextInput( AcceptSub2 ),  0.0f, hashCode ),
               ( GuideIcon.SUB3,    _playerSkillNames[2], CanAcceptSub3, new AcceptContextInput( AcceptSub3 ),  0.0f, hashCode ),
               ( GuideIcon.SUB4,    _playerSkillNames[3], CanAcceptSub4, new AcceptContextInput( AcceptSub4 ),  0.0f, hashCode )
            );
        }

        protected override void AdaptSelectPlayer()
        {
            // グリッドカーソルで選択中のプレイヤーを取得
            LazyInject.GetOrCreate( ref _plOwner, () => _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter() as Player );
        }

        protected override bool CanAcceptConfirm()
        {
            // 所有スキルのうち、どれか一つでも使用フラグが立っていれば決定入力を受け付ける
            for( int i = 0; i < EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                if( _plOwner.BattleLogic.IsUsingEquipSkill( i ) )
                {
                    return true;
                }
            }

            return false;
        }

        protected override bool CanAcceptCancel()
        {
            return true;
        }

        protected override bool CanAcceptSub1()
        {
            if( _playerSkillNames[0].Length <= 0 ) return false;

            bool useable = _plOwner.BattleLogic.CanToggleEquipSkill( 0, SituationType.ATTACK );
            _presenter.SetUseableSkillOnParamView( 0, useable, ParameterWindowType.Left );

            return useable;
        }

        protected override bool CanAcceptSub2()
        {
            if( _playerSkillNames[1].Length <= 0 ) return false;

            bool useable = _plOwner.BattleLogic.CanToggleEquipSkill( 1, SituationType.ATTACK );
            _presenter.SetUseableSkillOnParamView( 1, useable, ParameterWindowType.Left );

            return useable;
        }

        protected override bool CanAcceptSub3()
        {
            if( _playerSkillNames[2].Length <= 0 ) return false;

            bool useable = _plOwner.BattleLogic.CanToggleEquipSkill( 2, SituationType.ATTACK );
            _presenter.SetUseableSkillOnParamView( 2, useable, ParameterWindowType.Left );

            return useable;
        }

        protected override bool CanAcceptSub4()
        {
            if( _playerSkillNames[3].Length <= 0 ) return false;

            bool useable = _plOwner.BattleLogic.CanToggleEquipSkill( 3, SituationType.ATTACK );
            _presenter.SetUseableSkillOnParamView( 3, useable, ParameterWindowType.Left );

            return useable;
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            // スキル使用フラグが立っているスキルの消費分だけ行動ゲージを減らす
            // (一時保存パラメータに保存することで、キャンセルによって消費前に戻せる形に)
            _plOwner.BattleLogic.TemporarilyConsumeActionGauge();
            
            // コマンド履歴にスキル選択を追加
            _plOwner.PushCommandHistory( COMMAND_TAG.SKILL );

            Back();

            return true;
        }

        protected override bool AcceptCancel( InputContext context )
        {
            if( !base.AcceptCancel( context ) ) { return false; }

            _plOwner.BattleLogic.ResetUseSkills();

            return true;
        }

        protected override bool AcceptSub1( InputContext context )
        {
            if( !base.AcceptSub1( context ) ) return false;

            _plOwner.BattleLogic.ToggleUseSkill( 0 );
            _presenter.SetSkillFlickOnParamView( 0, _plOwner.BattleLogic.IsUsingEquipSkill( 0 ), ParameterWindowType.Left );

            return true;
        }

        protected override bool AcceptSub2( InputContext context )
        {
            if( !base.AcceptSub2( context ) ) return false;

            _plOwner.BattleLogic.ToggleUseSkill( 1 );
            _presenter.SetSkillFlickOnParamView( 1, _plOwner.BattleLogic.IsUsingEquipSkill( 1 ), ParameterWindowType.Left );

            return true;
        }

        protected override bool AcceptSub3( InputContext context )
        {
            if( !base.AcceptSub3( context ) ) return false;

            _plOwner.BattleLogic.ToggleUseSkill( 2 );
            _presenter.SetSkillFlickOnParamView( 2, _plOwner.BattleLogic.IsUsingEquipSkill( 2 ), ParameterWindowType.Left );

            return true;
        }

        protected override bool AcceptSub4( InputContext context )
        {
            if( !base.AcceptSub4( context ) ) return false;

            _plOwner.BattleLogic.ToggleUseSkill( 3 );
            _presenter.SetSkillFlickOnParamView( 3, _plOwner.BattleLogic.IsUsingEquipSkill( 3 ), ParameterWindowType.Left );

            return true;
        }
    }
}