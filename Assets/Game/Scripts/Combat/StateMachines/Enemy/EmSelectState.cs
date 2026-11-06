using Frontier.Combat;
using Frontier.Entities;
using Frontier.StateMachine;
using Frontier.UI;
using System;
using System.Collections.Generic;

namespace Frontier.Battle
{
    public class EmSelectState : UnitPhaseState
    {
        Enemy _currentEnemy = null;
        IEnumerator<Character> _enemyEnumerator;
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
                ( ShouldTransitionToMove,   COMMAND_TAG.MOVE,   true  ),   // 移動
                ( ShouldTransitionToAttack, COMMAND_TAG.ATTACK, true  ),   // 攻撃
                ( ShouldTransitionToSkill,  COMMAND_TAG.SKILL,  true  ),   // スキルによる攻撃
                ( ShouldTransitionToWait,   COMMAND_TAG.WAIT,   false ),   // 待機(行動しないため表示しない)
            };
        }

        public override void Init( object context )
        {
            base.Init( context);

            _enemyEnumerator = _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.ENEMY ).GetEnumerator();
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
            bool isExist = false;   // 行動可能なキャラクターが存在するか

            // 行動済みでないキャラクターを選択する
            while( _enemyEnumerator.MoveNext() )
            {
                _currentEnemy = _enemyEnumerator.Current as Enemy;
                if( ShouldTransitionToNextCharacter( _currentEnemy ) )
                {
                    continue;
                }

                isExist = true;

                // MEMO : 選択グリッドは、行動の内容が決まった後(Update内で行動へ遷移する時点)で合わせる

                if( !_currentEnemy.BattleLogic.GetAi().IsDetermined() )
                {
                    EnemyBattleLogic enemyLogic = _currentEnemy.BattleLogic as EnemyBattleLogic;
                    (_isValidDestination, _isValidTarget) = enemyLogic.DetermineDestinationAndTargetWithAI();

                    // 移動先が現在地(移動経路が空)の場合は、移動の必要が無いため移動済み状態にする
                    // (移動コマンドに遷移させず、移動・攻撃の両コマンドの終了状態による行動終了判定が成り立つようにするため)
                    if( _currentEnemy.BattleLogic.ActionRangeCtrl.MovePathHdlr.ProposedMovePath.Count <= 0 )
                    {
                        _currentEnemy.BattleParams.TmpParam.SetEndCommandStatus( COMMAND_TAG.MOVE, true );
                    }
                }

                // 攻撃対象がいなかった場合は攻撃済み状態にする
                // ただし、スキルなどで攻撃出来ない状態になっている可能性があるため、SetEndCommandStatus( COMMAND_TAG.ATTACK, _isValidTarget ) としてはならない
                if( !_isValidTarget )
                {
                    _currentEnemy.BattleParams.TmpParam.SetEndCommandStatus( COMMAND_TAG.ATTACK, true );
                }

                break;
            }

            return !isExist;
        }

        /// <summary>
        /// 移動コマンドに遷移するかを取得します
        /// </summary>
        /// <param name="em">判定する敵キャラクター</param>
        /// <returns>遷移するか否か</returns>
        private bool ShouldTransitionToMove( Enemy em )
        {
            if( !_isValidDestination ) return false;

            if( em.BattleParams.TmpParam.IsEndCommand[ ( int ) COMMAND_TAG.MOVE ] ) return false;

            return true;
        }

        /// <summary>
        /// 攻撃コマンドに遷移するかを取得します
        /// </summary>
        /// <param name="em">判定する敵キャラクター</param>
        /// <returns>遷移するか否か</returns>
        private bool ShouldTransitionToAttack( Enemy em )
        {
            if( !_isValidTarget ) return false;

            if( em.BattleParams.TmpParam.IsEndCommand[ ( int ) COMMAND_TAG.ATTACK ] ) return false;

            // AIがスキルによる攻撃を選択している場合は、通常攻撃ではなくスキルの行動へ遷移させる
            if( em.BattleLogic.GetAi().TryGetAttackOption( out var option ) && !option.IsNormalAttack ) return false;

            return true;
        }

        /// <summary>
        /// スキルによる攻撃コマンドに遷移するかを取得します
        /// </summary>
        /// <param name="em">判定する敵キャラクター</param>
        /// <returns>遷移するか否か</returns>
        private bool ShouldTransitionToSkill( Enemy em )
        {
            if( !_isValidTarget ) return false;

            if( em.BattleParams.TmpParam.IsEndCommand[ ( int ) COMMAND_TAG.ATTACK ] ||
                em.BattleParams.TmpParam.IsEndCommand[ ( int ) COMMAND_TAG.SKILL ] ) return false;

            return em.BattleLogic.GetAi().TryGetAttackOption( out var option ) && !option.IsNormalAttack;
        }

        private bool ShouldTransitionToWait( Enemy em )
        {
            if( _isValidDestination || _isValidTarget ) { return false; }

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