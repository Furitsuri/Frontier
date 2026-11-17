using Frontier.Combat;
using Frontier.Entities;
using Frontier.Entities.Ai;
using Frontier.Sequences;
using Frontier.Stage;
using Frontier.UI;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Battle
{
    /// <summary>
    /// 敵がスキルによる攻撃(回復等も含む)を行うステートです。
    /// 使用するスキル・向き・攻撃対象などは、AIが選択した攻撃(AttackOption)に従います。
    /// 実行前の流れはNpcActionStateBaseで共通化しています。
    /// 攻撃を受けるプレイヤーが防御スキル(スキルによる攻撃に効果を持つもの)を使用出来る場合は、該当するプレイヤー1人ずつに防御スキルを選択させた後に実行し、
    /// 介入する手立てが無い場合は、確認の入力を待たずに一定時間表示した後に実行します。
    /// 予兆を伴うスキルは、予兆を示した範囲に攻撃対象がいなくなっても実行するため、主となる攻撃対象(_targetCharacter)がnullの場合があります
    /// </summary>
    public class EmSkillState : NpcActionStateBase
    {
        private enum EmSkillPhase
        {
            EM_SKILL_CONFIRM = 0,
            EM_SKILL_EXECUTE,
            EM_SKILL_END,
        }

        private enum TransitTag
        {
            DEFENCE_SKILL_SELECT = 0,
        }

        [Inject] private SequenceFacade _sequenceFcd = null;
        [Inject] private AttackTelegraphPlanner _telegraphPlanner = null;

        private EmSkillPhase _phase;
        private Enemy _attackCharacter      = null;
        private Character _targetCharacter  = null;
        private AttackOption _attackOption;
        private TargetingRangeContext _targetingContext = null;
        // 防御スキルを選択させるプレイヤー(介入の手立てがあるプレイヤー)と、次に選択させるプレイヤーの番号
        private List<Player> _interveningPlayers        = new List<Player>();
        private int _nextInterveningIndex               = 0;
        private bool _isWaitingForDefenceSkillSelect    = false;

        public override void Init( object context )
        {
            base.Init( context );

            _attackCharacter = context as Enemy;    // 手番の敵はEmSelectStateから渡される
            Debug.Assert( _attackCharacter != null );

            bool isSelected = _attackCharacter.BattleLogic.GetAi().TryGetAttackOption( out _attackOption );
            Debug.Assert( isSelected && !_attackOption.IsNormalAttack, "スキルによる攻撃が選択されていない状態でEmSkillStateに遷移しています。" );

            // 予約していた攻撃(予兆を伴うスキル)の実行を開始するため、予兆の表示を消去する(以降は下記の攻撃範囲の表示に切り替わる)
            _telegraphPlanner.NotifyReservedActionStarted( _attackCharacter );

            _targetCharacter    = _attackOption.MainTarget;
            var skillData       = SkillsData.data[( int ) _attackOption.SkillID];

            // スキルを使用ONにして、スキルの効果(攻撃回数・倍率・消費ゲージ等)を適用する
            int slotIndex = GetEquipSkillSlotIndex( _attackCharacter, _attackOption.SkillID );
            Debug.Assert( 0 <= slotIndex, "使用するスキルを装備していません。" );
            _attackCharacter.BattleLogic.ToggleEquipSkill( slotIndex );

            // スキルの攻撃範囲を設定して表示する
            var actionRangeCtrl = _attackCharacter.BattleLogic.ActionRangeCtrl;
            int dprtTileIndex   = _attackCharacter.BattleParams.TmpParam.CurrentTileIndex;
            actionRangeCtrl.SetupAttackableRangeData( dprtTileIndex, _attackOption.SkillID );

            if( TargetingMode.DIRECTIONAL == skillData.TargetingMode )
            {
                // 選択した向きへ回転させた上で、プレイヤーの対象選択と同じ処理で対象範囲・着地先(ゴースト)を表示する
                // (NPCは射程を調整せず、常に最大の射程で使用する。ただし移動を伴うスキルは、予兆で示した着地先までの距離を射程とする)
                _attackCharacter.OrderRotate( Quaternion.Euler( 0f, StageDirectionConverter.DirectionAngles[( int ) _attackOption.Direction], 0f ) );
                int range = ( skillData.IsMovingSkill && 0 <= _attackOption.LandingTileIndex )
                    ? _stageCtrl.CalculateTotalRange( dprtTileIndex, _attackOption.LandingTileIndex )
                    : actionRangeCtrl.SkillMaxRange;
                actionRangeCtrl.RefreshTargetableRange( TargetingMode.DIRECTIONAL, true, skillData.IsMovingSkill, dprtTileIndex, range );

                if( skillData.IsMovingSkill && _attackCharacter.GetActiveGhostTileIndex() != _attackOption.LandingTileIndex )
                {
                    Debug.LogWarning( $"敵のスキルの着地先が、AIが選択した着地先と異なります。(選択:{_attackOption.LandingTileIndex}, 実際:{_attackCharacter.GetActiveGhostTileIndex()})" );
                }
            }
            else
            {
                actionRangeCtrl.DrawAttackableRange();

                // 攻撃者と主となる攻撃対象を向かい合わせる。攻撃対象がいない場合は範囲の中心を向く
                // (回復等で対象が自分自身の場合は、向く方向が定まらないため向きを変えない)
                int faceTileIndex = ( _targetCharacter != null ) ? _targetCharacter.BattleParams.TmpParam.CurrentTileIndex : _attackOption.CenterTileIndex;
                if( _targetCharacter != _attackCharacter && 0 <= faceTileIndex && faceTileIndex != dprtTileIndex )
                {
                    var targetTileData = _stageCtrl.GetTileStaticData( faceTileIndex );
                    _attackCharacter.RotateToPosition( targetTileData.CharaStandPos );
                }
            }

            // アタッカーキャラクターの設定
            _stageCtrl.BindGridCursor( GridCursorState.ATTACK, _attackCharacter );
            _stageCtrl.ApplyGridCursor2CharacterTile( _attackCharacter );
            // MEMO : カメラの注視先はターゲットカーソルへ切り替えない。
            //        敵の行動後はグリッドカーソルへ注視先を戻さない(CleanupTargetSelectionStateでfocusOwner:false)ため、
            //        切り替えるとプレイヤーフェーズになってもカメラがグリッドカーソルに追従しなくなる
            if( _targetCharacter != null ) { _stageCtrl.ApplyTargetCursor2CharacterTile( false, _targetCharacter ); }
            else { _stageCtrl.SetActiveTargetCursor( false ); }

            // 対象選択中はカメラを少しだけ近づける
            _btlRtnCtrl.GetBtlCameraCtrl.SetTargetSelectZoomActive( true );

            // 主となる攻撃対象をパラメータビュー(左)に表示する
            // (スキルを使用する敵自身(右)の表示と、行動終了後に左右を閉じる処理はEmSelectStateが行う)
            // 攻撃対象がいない場合は、左のパラメータビュー・ダメージ予測を表示しない
            if( _targetCharacter != null ) { ShowParameterWindow( ParameterWindowType.Left, _targetCharacter ); }
            else { CloseParameterWindow( ParameterWindowType.Left ); }
            _presenter.SetActiveActionResultExpect( _targetCharacter != null, ParameterWindowType.Right );

            // 攻撃を受ける側のスキルの使用可否を、防御シチュエーション(スキルによる攻撃に効果を持つ種類のみ)で更新する
            // (プレイヤーフェーズ終了時の更新結果のままだと、防御時に使用出来ないスキルまで使用可能な表示になるため。通常攻撃(EmAttackState)と揃える)。
            // 回復等の自軍を対象とするスキルは攻撃ではないため、防御スキルの選択の対象としない
            _interveningPlayers.Clear();
            _nextInterveningIndex           = 0;
            _isWaitingForDefenceSkillSelect = false;
            if( ActionType.ATTACK == skillData.ActionType )
            {
                foreach( var target in GetTargets() )
                {
                    EmDefenceSkillSelectState.RefreshDefenceUseableSkillFlags( target, EmDefenceSkillSelectState.SkillAttackDefenceActionTypeBit );
                    target.BattleLogic.SelectUseSkills( SituationType.DEFENCE );   // プレイヤー以外はAIが選択する(プレイヤーは防御スキル選択ステートで選択する)
                }
                _interveningPlayers.AddRange( CollectInterveningPlayers( GetTargets() ) );
            }

            // 予測ダメージを適応し、攻撃対象全員のHPゲージに予測ダメージ分の点滅を反映する
            _targetingContext = new TargetingRangeContext { BtlRtnCtrl = _btlRtnCtrl, Presenter = _presenter, Owner = _attackCharacter, StageCtrl = _stageCtrl };
            RefreshSkillDamageExpect();

            BeginActionConfirm( 0 < _interveningPlayers.Count );

            _phase = EmSkillPhase.EM_SKILL_CONFIRM;
        }

        public override bool Update()
        {
            switch( _phase )
            {
                case EmSkillPhase.EM_SKILL_CONFIRM:
                    if( _isWaitingForDefenceSkillSelect ) { break; }

                    // 対象への向き直りが終わった時点で、介入の手立てがあるプレイヤーには防御スキルを選択させ、
                    // 無い場合はスキルの内容を一定時間表示した時点で実行する
                    switch( UpdateActionConfirm( !_attackCharacter.IsRotationOrdered() ) )
                    {
                        case ActionConfirmResult.REQUEST_INTERVENTION:
                            // 防御スキルの選択を、対象のプレイヤー1人ずつ順に行い、全員が終えた時点で実行する
                            if( _nextInterveningIndex < _interveningPlayers.Count ) { TransitToDefenceSkillSelect( _interveningPlayers[_nextInterveningIndex++] ); }
                            else { ExecuteSkill(); }
                            break;
                        case ActionConfirmResult.EXECUTE:
                            ExecuteSkill();
                            break;
                    }
                    break;
                case EmSkillPhase.EM_SKILL_EXECUTE:
                    _phase = EmSkillPhase.EM_SKILL_END;
                    break;
                case EmSkillPhase.EM_SKILL_END:
                    // 攻撃またはスキルを実行した時点で行動終了とするため、攻撃・スキルの両コマンドを選択不可にする
                    _attackCharacter.BattleParams.TmpParam.SetEndCommandStatus( COMMAND_TAG.ATTACK, true );
                    _attackCharacter.BattleParams.TmpParam.SetEndCommandStatus( COMMAND_TAG.SKILL, true );

                    Back();

                    return true;
            }

            return false;
        }

        protected override void OnActivated()
        {
            base.OnActivated();

            // 防御スキルの選択から戻った場合は、次のプレイヤーの選択または実行へ進む(Updateで判定する)。
            // 全員の選択を終えた場合は、パラメータウィンドウ(左)を主となる攻撃対象の表示に戻す
            if( _isWaitingForDefenceSkillSelect )
            {
                _isWaitingForDefenceSkillSelect = false;
                if( _interveningPlayers.Count <= _nextInterveningIndex && _targetCharacter != null ) { ShowParameterWindow( ParameterWindowType.Left, _targetCharacter ); }
            }
        }

        public override object ExitState()
        {
            // 対象選択の共通後始末
            // (パラメータウィンドウはスキルの演出中も表示し続けるため非表示にせず、グリッドカーソルもEmAttackStateと同様の理由でオーナーへ戻さない)
            CleanupTargetSelectionState( _attackCharacter, _targetCharacter, ParameterWindowType.Right, hideTargetWindow: false, focusOwner: false );
            // 他キャラクターの描画分も含め、タイルメッシュの描画をすべてクリア
            // (着地先のゴーストはスキルの実行開始時(SkillActionBase.OnBeforeNameDisplay)に非表示になっている)
            _btlRtnCtrl.BtlCharaCdr.ClearAllTileMeshes();

            return base.ExitState();
        }

        /// <summary>
        /// スキルを実行します(アクションゲージを消費し、スキルのシーケンスを登録します)
        /// </summary>
        private void ExecuteSkill()
        {
            // キャラクターのアクションゲージを消費
            _attackCharacter.BattleLogic.ConsumeActionGaugeForSkill();
            // 防御スキルを選択したプレイヤーは、使用分のゲージを消費し、使用ONのスキルを使用済みへ移す(通常攻撃(EmAttackState)と同様)
            foreach( var defender in _interveningPlayers ) { defender.BattleLogic.ConsumeActionGaugeForSkill(); }
            if( _targetCharacter != null && !( _targetCharacter is Player mainDefender && _interveningPlayers.Contains( mainDefender ) ) )
            {
                _targetCharacter.BattleLogic.ConsumeActionGauge();
            }

            FinalizeTargetSelection( ParameterWindowType.Right );
            _attackCharacter.BattleLogic.ActionRangeCtrl.ActionableRangeRdr.ClearTileMeshesByType( TileMapType.ATTACKABLE | TileMapType.TARGETABLE | TileMapType.QUEUED );

            // 防御側が自己バフスキル(GUARD等)を使用している場合はバフシーケンスを登録する(必ずスキルのシーケンスより先に登録する)
            foreach( var defender in _interveningPlayers ) { defender.BattleLogic.RegistSelfBuffSequences(); }

            _sequenceFcd.RegistSkillAction( _attackCharacter, _targetCharacter, _attackOption.SkillID, _attackOption.AttackTargetKeys ?? new List<CharacterKey>() );    // スキルのシーケンス処理に移行

            _phase = EmSkillPhase.EM_SKILL_EXECUTE;
        }

        /// <summary>
        /// 指定のプレイヤーに防御スキルを選択させます(選択中はパラメータウィンドウ(左)にそのプレイヤーを表示します)
        /// </summary>
        private void TransitToDefenceSkillSelect( Player defender )
        {
            ShowParameterWindow( ParameterWindowType.Left, defender );

            _isWaitingForDefenceSkillSelect = true;
            SetSendTransitionContext( new EmDefenceSkillSelectState.TransitContext
            {
                Attacker                = _attackCharacter,
                Defender                = defender,
                UseableActionTypeBit    = EmDefenceSkillSelectState.SkillAttackDefenceActionTypeBit,
                RefreshDamageExpect     = RefreshSkillDamageExpect,
            } );
            TransitState( ( int ) TransitTag.DEFENCE_SKILL_SELECT );
        }

        /// <summary>
        /// 予測ダメージを適応し、攻撃対象全員のHPゲージに予測ダメージ分の点滅を反映します
        /// (防御スキルの使用を切り替えた際にも、予測ダメージが変わるため呼び出します)
        /// </summary>
        private void RefreshSkillDamageExpect()
        {
            _btlRtnCtrl.BtlCharaCdr.ApplyDamageExpect( _attackCharacter, _targetCharacter );
            TargetingPreviewHelper.RefreshPredictedDamageGauges( _targetingContext, _attackOption.AttackTargetKeys );
        }

        /// <summary>
        /// スキルの対象(主となる対象を含む)を取得します
        /// </summary>
        private IEnumerable<Character> GetTargets()
        {
            if( null != _targetCharacter ) { yield return _targetCharacter; }

            if( null == _attackOption.AttackTargetKeys ) { yield break; }

            foreach( var key in _attackOption.AttackTargetKeys )
            {
                var target = _btlRtnCtrl.BtlCharaCdr.GetCharacter( key );
                if( null != target && target != _targetCharacter ) { yield return target; }
            }
        }

        /// <summary>
        /// 指定のスキルを装備しているスロットのうち、未使用のスロットの番号を取得します
        /// </summary>
        /// <returns>スロットの番号。存在しない場合は-1</returns>
        private int GetEquipSkillSlotIndex( Character chara, SkillID skillID )
        {
            for( int i = 0; i < EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                if( chara.GetEquipSkillID( i ) == skillID && !chara.BattleParams.TmpParam.IsSkillsUsed[i] ) { return i; }
            }

            return -1;
        }
    }
}
