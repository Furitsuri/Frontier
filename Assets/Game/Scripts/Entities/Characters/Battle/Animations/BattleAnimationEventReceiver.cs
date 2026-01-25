using Frontier.Combat.Skill;
using Frontier.Stage;
using UnityEngine;
using Zenject;

namespace Frontier.Entities
{
    [SerializeField]
    public class BattleAnimationEventReceiver : MonoBehaviour, IBasicAnimationEvent, IParryAnimationEvent
    {
        [Inject] private IUiSystem _uiSystem                                = null;
        [Inject] private StageController _stageCtrl                         = null;
        [Inject] private CombatSkillEventController _combatSkillEventCtrl   = null;

        // 攻撃用アニメーションタグ
        static public AnimDatas.AnimeConditionsTag[] AttackAnimTags = new AnimDatas.AnimeConditionsTag[]
        {
            AnimDatas.AnimeConditionsTag.SINGLE_ATTACK,
            AnimDatas.AnimeConditionsTag.DOUBLE_ATTACK,
            AnimDatas.AnimeConditionsTag.TRIPLE_ATTACK
        };

        private ReadOnlyReference<Character> _readOnlyOwner                         = null;
        private ReadOnlyReference<Bullet> _readOnlyBullet                           = null;
        private ReadOnlyReference<BattleLogicBase> _readOnlyBattleLogic             = null;
        private ReadOnlyReference<BattleCameraController> _readOnlyBattleCameraCtrl = null;


        public bool IsAttacked { get; set; } = false;
        public int AtkRemainingNum { get; set; } = 0;                               // 攻撃シーケンスにおける残り攻撃回数

        /// <summary>
        /// 特に何もしませんが、アクティブ設定のチェックボックスをInspector上に出現させるために定義
        /// </summary>
        void Update() { }

        public void Regist( Character owner, BattleCameraController btlCamCtrl )
        {
            _readOnlyOwner              = new ReadOnlyReference<Character>( owner );
            _readOnlyBullet             = new ReadOnlyReference<Bullet>( owner.GetBullet() );
            _readOnlyBattleLogic        = new ReadOnlyReference<BattleLogicBase>( owner.BattleLogic );
            _readOnlyBattleCameraCtrl   = new ReadOnlyReference<BattleCameraController>( btlCamCtrl );
        }

        /// <summary>
        /// 死亡処理を開始します
        /// ※各キャラクターのアニメーションから呼ばれます
        /// </summary>
        public void DieOnAnimEvent()
        {
        }

        /// <summary>
        /// キャラクターに設定されている弾を発射します
        /// ※各キャラクターのアニメーションから呼ばれます
        /// </summary>
        public void FireBulletOnAnimEvent()
        {
            if( _readOnlyBullet.Value == null || _readOnlyBattleLogic.Value.GetOpponent() == null ) { return; }

            _readOnlyBullet.Value.gameObject.SetActive( true );

            // 射出地点、目標地点などを設定して弾を発射
            var firingPoint = transform.position;
            firingPoint.y += _readOnlyOwner.Value.CameraParam.UICameraLookAtCorrectY;
            _readOnlyBullet.Value.SetFiringPoint( firingPoint );
            var targetCoordinate = _readOnlyBattleLogic.Value.GetOpponent().transform.position;
            targetCoordinate.y += _readOnlyBattleLogic.Value.GetOpponent().CameraParam.UICameraLookAtCorrectY;
            _readOnlyBullet.Value.SetTargetCoordinate( targetCoordinate );
            var gridLength = _stageCtrl.CalcurateGridLength( _readOnlyBattleLogic.Value.BattleParams.TmpParam.gridIndex, _readOnlyBattleLogic.Value.GetOpponent().BattleLogic.BattleParams.TmpParam.gridIndex );
            _readOnlyBullet.Value.SetFlightTimeFromGridLength( gridLength );
            _readOnlyBullet.Value.StartUpdateCoroutine( HurtOpponentByAnimation );
            _readOnlyBattleCameraCtrl.Value.TransitNextPhaseCameraParam( null, _readOnlyBullet.Value.transform );   // 発射と同時に次のカメラパラメータを適用

            // この攻撃によって相手が倒されるかどうかを判定
            _readOnlyBattleLogic.Value.GetOpponent().BattleLogic.IsDeclaredDead = ( _readOnlyBattleLogic.Value.GetOpponent().GetStatusRef.CurHP + _readOnlyBattleLogic.Value.GetOpponent().BattleLogic.BattleParams.TmpParam.expectedHpChange ) <= 0;
            if( !_readOnlyBattleLogic.Value.GetOpponent().BattleLogic.IsDeclaredDead && 0 < AtkRemainingNum )
            {
                --AtkRemainingNum;
                _readOnlyOwner.Value.AnimCtrl.SetAnimator( AttackAnimTags[AtkRemainingNum] );
            }
        }

        /// <summary>
        /// 相手を攻撃した際の処理を開始します
        /// ※各キャラクターのアニメーションから呼ばれます
        /// </summary>
        public void AttackOpponentOnAnimEvent()
        {
            if( _readOnlyBattleLogic.Value.GetOpponent() == null )
            {
                Debug.Assert( false );
            }

            IsAttacked = true;
            _readOnlyBattleLogic.Value.GetOpponent().GetStatusRef.CurHP += _readOnlyBattleLogic.Value.GetOpponent().BattleLogic.BattleParams.TmpParam.expectedHpChange;

            //　ダメージが0の場合はモーションを取らない
            if( _readOnlyBattleLogic.Value.GetOpponent().BattleLogic.BattleParams.TmpParam.expectedHpChange != 0 )
            {
                if( _readOnlyBattleLogic.Value.GetOpponent().GetStatusRef.CurHP <= 0 )
                {
                    _readOnlyBattleLogic.Value.GetOpponent().GetStatusRef.CurHP = 0;
                    _readOnlyBattleLogic.Value.GetOpponent().AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.DIE );
                }
                // ガードスキル使用時は死亡時以外はダメージモーションを再生しない
                else if( _readOnlyBattleLogic.Value.GetOpponent().BattleLogic.GetUsingSkillSlotIndexById( ID.SKILL_GUARD ) < 0 )
                {
                    _readOnlyBattleLogic.Value.GetOpponent().AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.GET_HIT );
                }
            }

            _uiSystem.BattleUi.ShowDamageOnCharacter( _readOnlyBattleLogic.Value.GetOpponent() ); // ダメージUIを表示
        }

        /// <summary>
        /// 対戦相手にダメージを与えるイベントを発生させます
        /// ※ 弾の着弾以外では近接攻撃アニメーションからも呼び出される設計です
        ///    近接攻撃キャラクターの攻撃アニメーションの適当なフレームでこのメソッドイベントを挿入してください
        /// </summary>
        public void HurtOpponentByAnimation()
        {
            if( _readOnlyBattleLogic.Value.GetOpponent() == null )
            {
                Debug.Assert( false );
            }

            IsAttacked = true;
            _readOnlyBattleLogic.Value.GetOpponent().GetStatusRef.CurHP += _readOnlyBattleLogic.Value.GetOpponent().BattleLogic.BattleParams.TmpParam.expectedHpChange;

            //　ダメージが0の場合はモーションを取らない
            if( _readOnlyBattleLogic.Value.GetOpponent().BattleLogic.BattleParams.TmpParam.expectedHpChange != 0 )
            {
                if( _readOnlyBattleLogic.Value.GetOpponent().GetStatusRef.CurHP <= 0 )
                {
                    _readOnlyBattleLogic.Value.GetOpponent().GetStatusRef.CurHP = 0;
                    _readOnlyBattleLogic.Value.GetOpponent().AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.DIE );
                }
                // ガードスキル使用時は死亡時以外はダメージモーションを再生しない
                else if( _readOnlyBattleLogic.Value.GetOpponent().BattleLogic.GetUsingSkillSlotIndexById( ID.SKILL_GUARD ) < 0 )
                {
                    _readOnlyBattleLogic.Value.GetOpponent().AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.GET_HIT );
                }
            }

            // ダメージUIを表示
            _uiSystem.BattleUi.ShowDamageOnCharacter( _readOnlyBattleLogic.Value.GetOpponent() );
        }

        /// <summary>
        /// パリィイベントを開始します
        /// MEMO ; パリィは各キャラクターの攻撃アニメーションに設定されたタイミングから開始するため、
        ///        パリィを行うキャラクターのアニメーションではなく、
        ///        その対戦相手のアニメーションから呼ばれることに注意してください
        /// </summary>
        public void StartParryOnAnimEvent()
        {
            int parrySkillIdx = _readOnlyBattleLogic.Value.GetOpponent().BattleLogic.GetUsingSkillSlotIndexById( ID.SKILL_PARRY );
            if( parrySkillIdx < 0 ) { return; }
            var parrySkillNotifier = _readOnlyBattleLogic.Value.GetOpponent().BattleLogic.SkillNotifier( parrySkillIdx ) as ParrySkillNotifier;
            NullCheck.AssertNotNull( parrySkillNotifier, nameof( parrySkillNotifier ) );

            parrySkillNotifier.StartParryJudgeEvent();
        }

        /// <summary>
        /// 相手の攻撃を弾く動作を行います
        /// ※各キャラクターのパリィ用アニメーションから呼ばれます
        /// </summary>
        public void ParryAttackOnAnimEvent()
        {
            int parrySkillIdx = _readOnlyBattleLogic.Value.GetOpponent().BattleLogic.GetUsingSkillSlotIndexById( ID.SKILL_PARRY );
            if( parrySkillIdx < 0 ) { return; }
            var parrySkillNotifier = _readOnlyBattleLogic.Value.GetOpponent().BattleLogic.SkillNotifier( parrySkillIdx ) as ParrySkillNotifier;
            NullCheck.AssertNotNull( parrySkillNotifier, nameof( parrySkillNotifier ) );

            parrySkillNotifier.ParryOpponentEvent();
        }

        /// <summary>
        /// パリィの判定が得られる以前に、パリィによる振り払いモーションが再生されてしまうとまずいため、
        /// 振り払い直前でアニメーションを停止するために用います
        /// ※各キャラクターのパリィ用アニメーションから呼ばれます
        /// </summary>
        public void StopParryAnimationOnAnimEvent()
        {
            ParrySkillHandler parrySkillHdlr = _combatSkillEventCtrl.CurrentSkillHandler as ParrySkillHandler;
            if( parrySkillHdlr == null ) return;

            if( !parrySkillHdlr.IsJudgeEnd() )
            {
                _readOnlyOwner.Value.GetTimeScale.Stop();
            }
        }
    }
}