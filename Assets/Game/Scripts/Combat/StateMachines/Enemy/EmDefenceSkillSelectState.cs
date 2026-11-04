using Frontier.Combat;
using Frontier.Entities;
using Frontier.UI;
using System.Collections.Generic;
using static Constants;

namespace Frontier.Battle
{
    /// <summary>
    /// 敵の通常攻撃を受けるプレイヤーが、防御時に使用するスキルを選択するステートです。
    /// プレイヤーが自ら攻撃する際のスキル選択(PlSelectSkillState)と同じく、
    /// SkillBoxUIを縦一列へアニメーション移動させ、カーソルで選んだスキルの使用ON/OFFを切り替えます。
    /// 敵の攻撃自体は取り消せないため、キャンセル入力は受け付けません。決定入力で親(EmAttackState)へ戻り、攻撃が実行されます。
    /// </summary>
    public class EmDefenceSkillSelectState : UnitPhaseState
    {
        /// <summary>
        /// 親ステートから受け取る遷移コンテキストです
        /// </summary>
        public class TransitContext
        {
            public Character Attacker;
            public Player Defender;
        }

        private TransitContext _transitContext = null;
        private Character _attacker = null;
        private Player _defender    = null;

        private CommandList _commandList = new CommandList();
        private CommandList.CommandIndexedValue _cmdIdxVal;

        // 防御時に使用可能なスキルの種類(NormalAttackTargetingRangeでの防御側の判定と揃える)
        private static readonly int DefenceActionTypeBit = Methods.ToBit( ActionType.BUFF ) | Methods.ToBit( ActionType.SPECIAL );

        /// <summary>
        /// 防御側のスキルの使用可否を、防御シチュエーションで更新します
        /// </summary>
        static public void RefreshDefenceUseableSkillFlags( Character defender )
        {
            defender.RefreshUseableSkillFlags( SituationType.DEFENCE, DefenceActionTypeBit );
        }

        /// <summary>
        /// 防御時に使用可能なスキルが1つでも存在するかを判定します。
        /// 事前にRefreshDefenceUseableSkillFlagsで使用可否を更新しておく必要があります。
        /// </summary>
        static public bool HasUseableDefenceSkill( Character defender )
        {
            var skillNames = defender.GetStatusRef.GetEquipSkillNames();

            for( int i = 0; i < EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                if( 0 < skillNames[i].Length && defender.BattleParams.TmpParam.IsUseableSkill[i] ) { return true; }
            }

            return false;
        }

        public override void Init( object context )
        {
            base.Init( context );

            ReceiveContext( ref _transitContext, context );
            NullCheck.AssertNotNull( _transitContext, nameof( _transitContext ) );
            _attacker = _transitContext.Attacker;
            _defender = _transitContext.Defender;

            // 装備されているスキルの枠のみをカーソル移動対象にする
            var skillNames      = _defender.GetStatusRef.GetEquipSkillNames();
            var equippedIndices = new List<int>();
            for( int i = 0; i < EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                if( 0 < skillNames[i].Length ) { equippedIndices.Add( i ); }
            }

            _cmdIdxVal = new CommandList.CommandIndexedValue( 0, 0 );
            _commandList.Init( ref equippedIndices, CommandList.CommandDirection.VERTICAL, false, _cmdIdxVal );

            RefreshDefenceUseableSkillFlags( _defender );

            // SkillBoxUIを選択用の縦一列レイアウトへアニメーション移動(防御側のプレイヤーは左のパラメータウィンドウに表示されている)
            _presenter.AnimateSkillBoxesForSelection( ParameterWindowType.Left );
            _presenter.SetSkillBoxCursorIndex( ParameterWindowType.Left, _cmdIdxVal.value );

            // スキル詳細情報パネルを表示する
            _presenter.SetActiveSkillDetail( true );
        }

        public override object ExitState()
        {
            // カーソルハイライトを解除する
            _presenter.SetSkillBoxCursorIndex( ParameterWindowType.Left, -1 );
            // スキル詳細情報パネルを非表示にする
            _presenter.SetActiveSkillDetail( false );

            return base.ExitState();
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (GuideIcon.VERTICAL_CURSOR, "SELECT",  CanAcceptDirection, new AcceptContextInput( AcceptDirection ), MENU_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.CONFIRM,         "CONFIRM", CanAcceptConfirm,   new AcceptContextInput( AcceptConfirm ),   0.0f, hashCode),
               (GuideIcon.SUB1,            "TOGGLE",  CanAcceptSub1,      new AcceptContextInput( AcceptSub1 ),      0.0f, hashCode)
            );
        }

        protected override bool CanAcceptDirection()
        {
            if( !CanAcceptDefault() ) { return false; }

            return !_presenter.IsSkillBoxLayoutAnimating;
        }

        protected override bool CanAcceptConfirm()
        {
            // 防御スキルを1つも使用しない選択も可能なため、使用フラグの有無は問わない
            return CanAcceptDirection();
        }

        protected override bool CanAcceptSub1()
        {
            if( !CanAcceptDirection() ) { return false; }

            return _defender.BattleParams.TmpParam.IsUseableSkill[_cmdIdxVal.value];
        }

        protected override bool AcceptDirection( InputContext context )
        {
            bool moved = _commandList.OperateListCursor( context.Cursor );

            if( moved )
            {
                _presenter.SetSkillBoxCursorIndex( ParameterWindowType.Left, _cmdIdxVal.value );
            }

            return moved;
        }

        protected override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            // 決定後はそのまま攻撃シーケンスへ移行するため、この時点でSkillBoxUIを元の位置に戻す
            _presenter.RevertSkillBoxesFromSelection( ParameterWindowType.Left );

            Back();

            return true;
        }

        protected override bool AcceptSub1( InputContext context )
        {
            if( !base.AcceptSub1( context ) ) { return false; }

            _defender.BattleLogic.ToggleEquipSkill( _cmdIdxVal.value );
            RefreshDefenceUseableSkillFlags( _defender );

            // 防御スキルの使用切り替えで予測ダメージが変わるため、予測を更新する
            RefreshNormalAttackDamageExpect( _attacker, _defender );

            return true;
        }
    }
}
