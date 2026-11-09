using Frontier.Combat;
using Frontier.Entities;
using Frontier.Entities.Ai;
using Frontier.StateMachine;
using Frontier.UI;
using System;
using System.Collections.Generic;
using Zenject;

namespace Frontier.Battle
{
    /// <summary>
    /// 敵フェーズで、次に行動する敵とその行動(移動・攻撃・スキル・待機)を選ぶステートです。
    /// 次に行動する敵は、番号順ではなく軍全体の調整役(TroopActionCoordinator)が、行動の度に現在の盤面を見て選びます。
    /// 行動し得る敵がいなくなった時点で、残りの敵をまとめて行動終了とし、フェーズを終了します。
    /// </summary>
    public class EmSelectState : UnitPhaseState
    {
        [Inject] private TroopActionCoordinator _coordinator = null;

        Enemy _currentEnemy = null;
        bool _isValidDestination = false;
        bool _isValidTarget = false;
        // 行動中としてパラメータウィンドウに表示している敵。
        // 子ステート(移動・攻撃・スキル)から戻る度にInit()が呼ばれるため、Init()ではリセットしない
        Enemy _actingEnemy = null;

        // 遷移先の判定表。先頭から順に判定し、最初に条件を満たしたコマンドへ遷移する(並び順が判定の優先順位)。
        // isAction : 行動(移動・攻撃・スキル)の開始として、パラメータウィンドウに表示するか
        readonly ( Func<Enemy, bool> shouldTransit, COMMAND_TAG command, bool isAction )[] _transitions;

        public EmSelectState()
        {
            _transitions = new ( Func<Enemy, bool>, COMMAND_TAG, bool )[]
            {
                ( em => ShouldTransitionToMove( em, _isValidDestination ),                  COMMAND_TAG.MOVE,   true  ),   // 移動
                ( em => ShouldTransitionToAttack( em, _isValidTarget ),                     COMMAND_TAG.ATTACK, true  ),   // 攻撃
                ( em => ShouldTransitionToSkill( em, _isValidTarget ),                      COMMAND_TAG.SKILL,  true  ),   // スキルによる攻撃
                ( em => ShouldTransitionToWait( em, _isValidDestination, _isValidTarget ),  COMMAND_TAG.WAIT,   false ),   // 待機(行動しないため表示しない)
            };
        }

        /// <summary>
        /// このフェーズで最初に行動(移動・攻撃・スキル)する敵を求めます。
        /// 調整役が選んだ敵の行動をAIに決定させ、行動しない(待機のみの)敵であれば行動終了として次の敵を選ぶ、を繰り返します。
        /// 敵の手番(ShouldExitState)で行う選択・決定と同じ処理を、いずれの敵もまだ行動していない時点で前倒しで行うだけのため、
        /// 結果は変わりません。決定した結果はAI側に保持され、手番の際にはそのまま使われます。
        /// </summary>
        /// <param name="coordinator">軍全体の調整役</param>
        /// <param name="enemies">手番の順に並んだ敵</param>
        /// <returns>最初に行動する敵(行動する敵がいない場合はnull)</returns>
        public static Enemy FindFirstActingEnemy( TroopActionCoordinator coordinator, IEnumerable<Character> enemies )
        {
            while( coordinator.SelectNextActor( enemies ) is Enemy enemy )
            {
                var ( isValidDestination, isValidTarget ) = coordinator.PrepareAction( enemy );
                if( WillTakeAction( enemy, isValidDestination, isValidTarget ) ) { return enemy; }

                // 待機のみの敵は、ここで行動終了とする(手番で待機(EmWaitState)を選んだ場合と同じ処理)
                enemy.BattleParams.TmpParam.EndAction();
            }

            return null;
        }

        /// <summary>
        /// 指定の敵が、移動・攻撃・スキルのいずれかの行動を行うかを取得します
        /// </summary>
        private static bool WillTakeAction( Enemy enemy, bool isValidDestination, bool isValidTarget )
        {
            return ShouldTransitionToMove( enemy, isValidDestination ) ||
                   ShouldTransitionToAttack( enemy, isValidTarget ) ||
                   ShouldTransitionToSkill( enemy, isValidTarget );
        }

        public override void Init( object context )
        {
            base.Init( context);

            _currentEnemy = null;
        }

        public override bool Update()
        {
            if( IsBack() ) { return true; }

            foreach( var ( shouldTransit, command, isAction ) in _transitions )
            {
                if( !shouldTransit( _currentEnemy ) ) { continue; }

                if( isAction )
                {
                    // 実際に行動(移動・攻撃・スキル)する敵にのみ選択グリッドを合わせる
                    // (待機のみの敵にまで合わせると、行動しない敵へカーソル・カメラの注視点が寄ってしまうため)
                    _stageCtrl.ApplyGridCursor2CharacterTile( _currentEnemy );
                    BeginActingDisplay( _currentEnemy );
                }
                // 遷移先(移動・攻撃・スキル・待機)へ、手番の敵を直接渡す
                // (グリッドカーソルの位置から求めると、待機のみの敵にはカーソルを合わせないため、別の敵を対象としてしまう)
                SetSendTransitionContext( _currentEnemy );
                TransitStateWithExit( ( int ) command );

                return true;
            }

            return false;
        }

        protected override void OnActivated()
        {
            base.OnActivated();

            bool shouldExit = ShouldExitState();

            // 行動中として表示していた敵の行動が終わっていれば(次に行動する敵が別の敵、または行動する敵がもういない)表示を閉じる。
            // 子ステートからはシーケンス(攻撃・スキル)の完全終了後に戻ってくるため、カメラ演出によるウィンドウの再表示より後になる
            if( null != _actingEnemy && ( shouldExit || _actingEnemy != _currentEnemy ) )
            {
                EndActingDisplay();
            }

            if( shouldExit )
            {
                Back();
            }
        }

        /// <summary>
        /// 行動を開始する敵をパラメータウィンドウ(右)に表示します。
        /// 同じ敵が移動に続けて攻撃・スキルを行う場合は、表示を継続します。
        /// </summary>
        private void BeginActingDisplay( Enemy enemy )
        {
            if( _actingEnemy == enemy ) { return; }

            _actingEnemy = enemy;
            CloseParameterWindow( ParameterWindowType.Left );   // プレイヤーフェーズの表示が残っている場合があるため閉じる(攻撃対象は攻撃・スキルのステートが表示する)
            ShowParameterWindow( ParameterWindowType.Right, enemy );
        }

        /// <summary>
        /// 行動を終えた敵の表示を終了し、左右のパラメータウィンドウを閉じます
        /// (攻撃・スキルの対象として表示していた左側も含めて閉じます)。
        /// </summary>
        private void EndActingDisplay()
        {
            _actingEnemy = null;
            CloseParameterWindow( ParameterWindowType.Left );
            CloseParameterWindow( ParameterWindowType.Right );
        }

        private bool ShouldExitState()
        {
            var enemies = _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.ENEMY );

            // 移動・攻撃の両方を終えた敵を行動終了とする
            foreach( var chara in enemies )
            {
                if( chara is Enemy enemy ) { ShouldTransitionToNextCharacter( enemy ); }
            }

            // 次に行動する敵を、軍全体の調整役に選ばせる(行動の途中の敵がいれば、その敵が選ばれる)
            _currentEnemy = _coordinator.SelectNextActor( enemies ) as Enemy;

            // 行動し得る敵がいなくなった場合は、残りの敵(待ち構えていて行動を開始していない敵等)をまとめて行動終了とする
            if( null == _currentEnemy )
            {
                foreach( var chara in enemies )
                {
                    chara.BattleParams.TmpParam.EndAction();
                }

                return true;
            }

            // MEMO : 選択グリッドは、行動の内容が決まった後(Update内で行動へ遷移する時点)で合わせる

            // AIに行動を決定させる(既に決定済みの場合はその結果を用いる)
            (_isValidDestination, _isValidTarget) = _coordinator.PrepareAction( _currentEnemy );

            // 攻撃対象がいなかった場合は攻撃済み状態にする
            // ただし、スキルなどで攻撃出来ない状態になっている可能性があるため、SetEndCommandStatus( COMMAND_TAG.ATTACK, _isValidTarget ) としてはならない
            if( !_isValidTarget )
            {
                _currentEnemy.BattleParams.TmpParam.SetEndCommandStatus( COMMAND_TAG.ATTACK, true );
            }

            return false;
        }

        /// <summary>
        /// 移動コマンドに遷移するかを取得します
        /// </summary>
        /// <param name="em">判定する敵キャラクター</param>
        /// <param name="isValidDestination">AIが決定した目的地が有効か</param>
        /// <returns>遷移するか否か</returns>
        private static bool ShouldTransitionToMove( Enemy em, bool isValidDestination )
        {
            if( !isValidDestination ) return false;

            if( em.BattleParams.TmpParam.IsEndCommand[ ( int ) COMMAND_TAG.MOVE ] ) return false;

            return true;
        }

        /// <summary>
        /// 攻撃コマンドに遷移するかを取得します
        /// </summary>
        /// <param name="em">判定する敵キャラクター</param>
        /// <param name="isValidTarget">AIが決定した攻撃対象が有効か</param>
        /// <returns>遷移するか否か</returns>
        private static bool ShouldTransitionToAttack( Enemy em, bool isValidTarget )
        {
            if( !isValidTarget ) return false;

            if( em.BattleParams.TmpParam.IsEndCommand[ ( int ) COMMAND_TAG.ATTACK ] ) return false;

            // AIがスキルによる攻撃を選択している場合は、通常攻撃ではなくスキルの行動へ遷移させる
            if( em.BattleLogic.GetAi().TryGetAttackOption( out var option ) && !option.IsNormalAttack ) return false;

            return true;
        }

        /// <summary>
        /// スキルによる攻撃コマンドに遷移するかを取得します
        /// </summary>
        /// <param name="em">判定する敵キャラクター</param>
        /// <param name="isValidTarget">AIが決定した攻撃対象が有効か</param>
        /// <returns>遷移するか否か</returns>
        private static bool ShouldTransitionToSkill( Enemy em, bool isValidTarget )
        {
            if( !isValidTarget ) return false;

            if( em.BattleParams.TmpParam.IsEndCommand[ ( int ) COMMAND_TAG.ATTACK ] ||
                em.BattleParams.TmpParam.IsEndCommand[ ( int ) COMMAND_TAG.SKILL ] ) return false;

            return em.BattleLogic.GetAi().TryGetAttackOption( out var option ) && !option.IsNormalAttack;
        }

        private static bool ShouldTransitionToWait( Enemy em, bool isValidDestination, bool isValidTarget )
        {
            if( isValidDestination || isValidTarget ) { return false; }

            if( !em.BattleParams.TmpParam.IsEndCommand[( int ) COMMAND_TAG.MOVE] &&
                !em.BattleParams.TmpParam.IsEndCommand[( int ) COMMAND_TAG.ATTACK] ) { return false; }

            return true;
        }

        /// <summary>
        /// 次のキャラクターへ遷移するかを取得します
        /// </summary>
        /// <param name="em">判定するキャラクター</param>
        /// <returns>遷移するか否か</returns>
        private bool ShouldTransitionToNextCharacter( Enemy em )
        {
            if( em.BattleParams.TmpParam.IsEndCommand[ ( int ) COMMAND_TAG.MOVE ] && em.BattleParams.TmpParam.IsEndCommand[( int ) COMMAND_TAG.ATTACK] )
            {
                em.BattleParams.TmpParam.EndAction();
                return true;
            }

            if( em.BattleParams.TmpParam.IsEndAction() ) return true;

            return false;
        }
    }
}