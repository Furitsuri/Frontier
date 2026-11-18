using Frontier.Combat;
using Frontier.Entities;
using Frontier.UI;
using System.Collections.Generic;
using static Constants;

namespace Frontier.Battle
{
    /// <summary>
    /// 敵の攻撃(通常攻撃・スキルによる攻撃)を受けるプレイヤーが、防御時に使用するスキルを選択するステートです。
    /// プレイヤーが自ら攻撃する際のスキル選択(PlSelectSkillState)と同じく、
    /// SkillBoxUIを縦一列へアニメーション移動させ、カーソルで選んだスキルの使用ON/OFFを切り替えます。
    /// 敵の攻撃自体は取り消せないため、キャンセル入力は受け付けません。決定入力で親(EmAttackState/EmSkillState)へ戻り、攻撃が実行されます。
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
            // 防御時に使用可能なスキルの種類のビットフラグ(攻撃の種類によって、効果を持つ防御スキルが異なるため)
            public int UseableActionTypeBit         = NormalAttackDefenceActionTypeBit;
            // 防御スキルの使用を切り替えた際に、予測ダメージを更新する処理(nullの場合は通常攻撃の予測ダメージを更新する)
            public System.Action RefreshDamageExpect = null;
        }

        private TransitContext _transitContext = null;
        private Character _attacker = null;
        private Player _defender    = null;

        private CommandList _commandList = new CommandList();
        private CommandList.CommandIndexedValue _cmdIdxVal;

        // 通常攻撃を受ける際に使用可能な防御スキルの種類(NormalAttackTargetingRangeでの防御側の判定と揃える)
        public static readonly int NormalAttackDefenceActionTypeBit = Methods.ToBit( ActionType.BUFF ) | Methods.ToBit( ActionType.SPECIAL );
        // スキルによる攻撃を受ける際に使用可能な防御スキルの種類。
        // スキルによる攻撃のシーケンスは、防御側の能力値の変化(ガード等の自己バフ)のみを反映し、
        // パリィ・カウンター等の攻撃シーケンス中の反応(SPECIAL)を扱わないため、自己バフのみとする
        public static readonly int SkillAttackDefenceActionTypeBit = Methods.ToBit( ActionType.BUFF );

        /// <summary>
        /// 防御側のスキルの使用可否を、防御シチュエーションで更新します
        /// </summary>
        /// <param name="useableActionTypeBit">使用可能なスキルの種類のビットフラグ(省略時は通常攻撃を受ける場合)</param>
        static public void RefreshDefenceUseableSkillFlags( Character defender, int useableActionTypeBit = -1 )
        {
            defender.RefreshUseableSkillFlags( SituationType.DEFENCE, ( useableActionTypeBit < 0 ) ? NormalAttackDefenceActionTypeBit : useableActionTypeBit );
        }

        /// <summary>
        /// 防御時に使用可能なスキルが1つでも存在するかを判定します。
        /// 事前にRefreshDefenceUseableSkillFlagsで使用可否を更新しておく必要があります。
        /// </summary>
        static public bool HasUseableDefenceSkill( Character defender )
        {
            var skillNames = defender.GetStatusRef.GetEquipSkillNames();

            for( int i = 0; i < EQUIPABLE_ACTIVE_SKILL_MAX_NUM; ++i )
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
            for( int i = 0; i < EQUIPABLE_ACTIVE_SKILL_MAX_NUM; ++i )
            {
                if( 0 < skillNames[i].Length ) { equippedIndices.Add( i ); }
            }

            _cmdIdxVal = new CommandList.CommandIndexedValue( 0, 0 );
            _commandList.Init( ref equippedIndices, CommandList.CommandDirection.VERTICAL, false, _cmdIdxVal );

            RefreshDefenceUseableSkillFlags( _defender, _transitContext.UseableActionTypeBit );

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
            RefreshDefenceUseableSkillFlags( _defender, _transitContext.UseableActionTypeBit );

            // 防御スキルの使用切り替えで予測ダメージが変わるため、予測を更新する
            if( _transitContext.RefreshDamageExpect != null ) { _transitContext.RefreshDamageExpect(); }
            else { RefreshNormalAttackDamageExpect( _attacker, _defender ); }

            return true;
        }
    }
}
