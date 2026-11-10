using Frontier.Battle;
using Frontier.Entities;
using Frontier.Stage;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Combat
{
    /// <summary>
    /// 範囲内の自軍(使用者自身を含む)のHPを回復するスキルの実行処理です。
    /// 回復量はスキルデータのHealAmount(汎用パラメータ1)で、最大HPを超えては回復しません。
    /// MEMO : 回復用のモーション・エフェクトはまだ用意されていないため、現状は待ち時間を挟んで回復量を表示するのみです
    /// </summary>
    public class HealSA : SkillActionBase
    {
        private enum HealSAState
        {
            WAIT_APPLY,
            WAIT_END,
            END,
        }

        private BattleCharacterCoordinator _btlCharaCdr = null;
        private StageController _stageCtrl              = null;

        private HealSAState _state;
        private float _elapsedTime;
        private List<Character> _targetCharacters = null;

        [Inject]
        public HealSA( Character owner, List<CharacterKey> targetCharaKeys, BattleRoutineController btlRtnCtrl, StageController stageCtrl )
            : base( owner, btlRtnCtrl.BtlPresenter, btlRtnCtrl.GetBtlCameraCtrl, btlRtnCtrl.GetBtlFileLoader )
        {
            _targetCharacters   = new List<Character>();
            _btlCharaCdr        = btlRtnCtrl.BtlCharaCdr;
            _stageCtrl          = stageCtrl;
            _skillID            = SkillID.HEAL;

            foreach( var key in targetCharaKeys )
            {
                var targetCharacter = _btlCharaCdr.GetCharacter( key );
                if( null != targetCharacter )
                {
                    _targetCharacters.Add( targetCharacter );
                }
            }
        }

        protected override void StartAction()
        {
            base.StartAction();

            // 回復量を予測値として設定する
            // MEMO : 実行時点では使用したスキルのフラグが「使用中(ON)」から「使用済み」へ移っており、
            //        使用中のスキルから効果を判断する予測(BattleCharacterCoordinator.ApplyDamageExpect)ではダメージとして扱われてしまうため、
            //        このスキル自身の回復量から直接求める
            foreach( var target in _targetCharacters )
            {
                int heal = HealForecaster.Forecast( _owner, target, _skillID );
                target.BattleParams.TmpParam.SetExpectedHpChange( heal, heal );
            }

            _elapsedTime    = 0f;
            _state          = HealSAState.WAIT_APPLY;
        }

        protected override void UpdateAction()
        {
            base.UpdateAction();

            _elapsedTime += DeltaTimeProvider.DeltaTime;

            switch( _state )
            {
                case HealSAState.WAIT_APPLY:
                    if( HEAL_SKILL_APPLY_DELAY <= _elapsedTime )
                    {
                        foreach( var target in _targetCharacters )
                        {
                            ApplyHealToTarget( target );
                        }

                        _elapsedTime    = 0f;
                        _state          = HealSAState.WAIT_END;
                    }
                    break;
                case HealSAState.WAIT_END:
                    if( HEAL_SKILL_END_WAIT_TIME <= _elapsedTime )
                    {
                        _state = HealSAState.END;
                    }
                    break;
                case HealSAState.END:
                    break;
            }
        }

        protected override void EndAction()
        {
            base.EndAction();

            _stageCtrl.UnbindGridCursor();
            _stageCtrl.ApplyGridCursor2CharacterTile( _owner );
            _stageCtrl.SetActiveGridCursor( true );
            _stageCtrl.SetActiveTargetCursor( false );
        }

        protected override bool IsFinished()
        {
            return _state == HealSAState.END;
        }

        /// <summary>
        /// 対象のHPを回復し、回復量を表示します(被弾モーション等は再生しません)
        /// </summary>
        private void ApplyHealToTarget( Character target )
        {
            // MEMO : Statusは構造体のため、参照で受け取らないと複製に対する変更となり反映されない
            ref var status  = ref target.GetStatusRef;
            status.CurHP    = Mathf.Min( status.CurHP + target.BattleParams.TmpParam.ExpectedHpChange, status.MaxHP );

            _presenter.ShowDamageOnCharacter( target, 1f );
        }
    }
}
