using Frontier.Battle;
using Frontier.Entities;
using Frontier.Registries;
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
    /// 対象ごとに回復エフェクト(PrefabRegistry.HealEffectPrefab)を再生し、エフェクト内に設定された
    /// EffectEventID.APPLY_EFFECT のタイミングで回復を適用・回復量を表示します(タイミングはエフェクトのTimeline上のEffectEventMarkerで調整)。
    /// MEMO : 使用者側の回復用モーションはまだ用意されていません
    /// </summary>
    public class HealSA : SkillActionBase
    {
        private enum HealSAState
        {
            WAIT_EFFECT,
            WAIT_END,
            END,
        }

        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private PrefabRegistry _prefabReg          = null;

        private BattleCharacterCoordinator _btlCharaCdr = null;
        private StageController _stageCtrl              = null;
        private List<TimelineEffectPlayer> _effects     = new List<TimelineEffectPlayer>();

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
            _skillID            = ActiveSkillID.HEAL;

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

            // HPが全快の対象など、実際には回復しない対象にはエフェクトを再生しない(回復量が0のため適用・表示も不要)
            foreach( var target in _targetCharacters )
            {
                if( target.BattleParams.TmpParam.ExpectedHpChange <= 0 ) { continue; }

                PlayHealEffect( target );
            }

            _elapsedTime    = 0f;
            _state          = HealSAState.WAIT_EFFECT;
        }

        protected override void UpdateAction()
        {
            base.UpdateAction();

            _elapsedTime += DeltaTimeProvider.DeltaTime;

            switch( _state )
            {
                case HealSAState.WAIT_EFFECT:
                    // 回復の適用はエフェクトのイベントから行われるため、ここでは全エフェクトの再生完了のみを待つ
                    if( _effects.TrueForAll( effect => effect == null || effect.IsFinished ) )
                    {
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

            foreach( var effect in _effects )
            {
                if( effect != null ) { UnityEngine.Object.Destroy( effect.gameObject ); }
            }
            _effects.Clear();

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
        /// 対象の足元に回復エフェクトを再生し、エフェクトのイベントで回復を適用するよう登録します。
        /// エフェクトのプレハブが未設定の場合は、演出無しで即座に回復を適用します。
        /// </summary>
        private void PlayHealEffect( Character target )
        {
            if( _prefabReg == null || _prefabReg.HealEffectPrefab == null )
            {
                ApplyHealToTarget( target );
                return;
            }

            var effectObj = _hierarchyBld.CreateGameObject( _prefabReg.HealEffectPrefab, true, "HealEffect" );
            effectObj.transform.SetParent( target.transform, false );

            var player = effectObj.GetComponent<TimelineEffectPlayer>();
            if( player == null )
            {
                Debug.LogWarning( "HealEffectPrefab に TimelineEffectPlayer が付いていないため、即座に回復を適用します。" );
                UnityEngine.Object.Destroy( effectObj );
                ApplyHealToTarget( target );
                return;
            }

            player.OnEffectEvent += id =>
            {
                if( id == EffectEventID.APPLY_EFFECT ) { ApplyHealToTarget( target ); }
            };
            player.Play();
            _effects.Add( player );
        }

        /// <summary>
        /// 対象のHPを回復し、回復量を表示します(被弾モーション等は再生しません)
        /// </summary>
        private void ApplyHealToTarget( Character target )
        {
            // MEMO : Statusは構造体のため、参照で受け取らないと複製に対する変更となり反映されない
            ref var status  = ref target.GetStatusRef;
            status.CurHP    = Mathf.Min( status.CurHP + target.BattleParams.TmpParam.ExpectedHpChange, status.MaxHP );

            _presenter.ShowHealOnCharacter( target, 1f );
        }
    }
}
