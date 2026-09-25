using Frontier.Battle;
using Frontier.Combat;
using Frontier.Entities;
using UnityEngine;
using Zenject;

namespace Frontier.Sequences
{
    /// <summary>
    /// 1 対 1 の攻撃シーケンスの共通処理です。
    /// 向きの変更 → 攻撃 → (パリィ / カウンター / 死亡) → 終了 という流れはここで管理し、
    /// 演出(カメラ・キャラクターの配置・近接攻撃時の移動有無)の違いは派生クラスで実装します。
    /// </summary>
    public abstract class CharacterAttackSequenceBase : ISequence
    {
        protected enum Phase
        {
            START,
            WAIT_ATTACK,
            ATTACK,
            WAIT_PARRY_RESULT,
            EXEC_PARRY,
            COUNTER,
            DIE,
            WAIT_END,
            END
        }

        [Inject] protected IUiSystem _uiSystem = null;
        [Inject] protected BattleRoutineController _btlRtnCtrl = null;
        [Inject] private CombatSkillEventController _combatSkillCtrl = null;

        delegate bool UpdateAttack( in Vector3 arg1, in Vector3 arg2 );

        private Phase _phase;
        private bool _counterConditions = false;
        private Character _diedCharacter = null;
        private Vector3 _departure = Vector3.zero;
        private Vector3 _destination = Vector3.zero;
        private UpdateAttack _updateAttackerAttack = null;
        private UpdateAttack _updateTargetAttack = null;
        private ParrySkillNotifier _parryNotifier = null;

        protected float _elapsedTime = 0f;
        protected Character _attackCharacter = null;
        protected Character _targetCharacter = null;
        protected Quaternion _atkCharaInitialRot = Quaternion.identity;
        protected Quaternion _tgtCharaInitialRot = Quaternion.identity;

        public CharacterAttackSequenceBase( Character attackChara, Character targetChara )
        {
            _attackCharacter = attackChara;
            _targetCharacter = targetChara;
        }

        public void Start()
        {
            _diedCharacter  = null;
            _elapsedTime    = 0f;
            _phase          = Phase.START;

            _atkCharaInitialRot = _attackCharacter.GetRotation();
            _tgtCharaInitialRot = _targetCharacter.GetRotation();

            // 対戦相手として設定
            _attackCharacter.BattleLogic.SetOpponentCharacter( _targetCharacter );
            _targetCharacter.BattleLogic.SetOpponentCharacter( _attackCharacter );

            _counterConditions = ( 0 <= _targetCharacter.BattleLogic.GetUsingSkillSlotIndexById( SkillID.COUNTER ) ); // カウンター条件の設定

            // 攻撃更新処理の条件別設定
            if( _counterConditions && _attackCharacter.GetBullet() != null ) _counterConditions = _targetCharacter.GetBullet() != null;
            // キャラクターの攻撃タイプによって動作するアニメーションを変更する
            _attackCharacter.BattleLogic.RegisterCombatAnimation( _attackCharacter.GetBullet() == null ? ClosedAnimationType : COMBAT_ANIMATION_TYPE.RANGED );
            _updateAttackerAttack = _attackCharacter.BattleLogic.CombatAnimSeq.UpdateSequence;
            _targetCharacter.BattleLogic.RegisterCombatAnimation( _targetCharacter.GetBullet() == null ? ClosedAnimationType : COMBAT_ANIMATION_TYPE.RANGED );
            _updateTargetAttack = _targetCharacter.BattleLogic.CombatAnimSeq.UpdateSequence;

            OnStart();
        }

        public void End() { }

        /// <summary>
        /// 処理を更新します
        /// </summary>
        /// <returns>処理の終了</returns>
        public bool Update()
        {
            switch( _phase )
            {
                case Phase.START:
                    // START_ROTATION_TIMEが経過するまで向きを変更します
                    _elapsedTime += DeltaTimeProvider.DeltaTime;
                    float t = Mathf.Clamp01( _elapsedTime / Constants.ATTACK_ROTATIION_TIME );
                    t = Mathf.SmoothStep( 0f, 1f, t );

                    Quaternion destAttackerRot  = Quaternion.LookRotation( _targetCharacter.GetPosition() - _attackCharacter.GetPosition() );
                    Quaternion destTargetRot    = Quaternion.LookRotation( _attackCharacter.GetPosition() - _targetCharacter.GetPosition() );
                    _attackCharacter.SetRotation( Quaternion.Lerp( _atkCharaInitialRot, destAttackerRot, t ) );
                    _targetCharacter.SetRotation( Quaternion.Lerp( _tgtCharaInitialRot, destTargetRot, t ) );

                    if( IsReadyToAttack( t ) )
                    {
                        _elapsedTime = 0f;

                        OnReadyToAttack();

                        _phase = Phase.WAIT_ATTACK;
                    }
                    break;
                case Phase.WAIT_ATTACK:
                    if( WaitAttackTime <= ( _elapsedTime += DeltaTimeProvider.DeltaTime ) )
                    {
                        _elapsedTime = 0f;
                        StartAttack( _attackCharacter, _targetCharacter );

                        // パリィスキル使用時はパリィ判定専用処理へ遷移
                        if( 0 <= _targetCharacter.BattleLogic.GetUsingSkillSlotIndexById( SkillID.PARRY ) )
                        {
                            _combatSkillCtrl.Register<ParrySkillHandler>();

                            _parryNotifier = _targetCharacter.BattleLogic.GetSkillNotifier<ParrySkillNotifier>( SkillID.PARRY );
                            NullCheck.AssertNotNull( _parryNotifier, nameof( _parryNotifier ) );
                            _phase = Phase.WAIT_PARRY_RESULT;
                        }
                        // それ以外は通常通り攻撃へ
                        else _phase = Phase.ATTACK;
                    }
                    break;
                case Phase.ATTACK:
                    if( _updateAttackerAttack( _departure, _destination ) )
                    {
                        OnAttackTurnFinished();
                        // ダメージUIを非表示
                        _uiSystem.BattleUi.HideDamageOnCharacter( _targetCharacter );

                        // ガードスキルを使用時はガードモーションを戻す
                        int guardSkillIdx = _targetCharacter.BattleLogic.GetUsingSkillSlotIndexById( SkillID.GUARD );
                        if( 0 <= guardSkillIdx ) { _targetCharacter.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.GUARD, false ); }

                        // 対象が死亡している場合は死亡処理へ
                        if( _targetCharacter.GetStatusRef.IsDead() )
                        {
                            _diedCharacter = _targetCharacter;
                            _phase = Phase.DIE;
                        }
                        // カウンタースキルが登録されている場合はカウンター処理へ
                        else if( _counterConditions )
                        {
                            StartCounter( _attackCharacter, _targetCharacter );

                            _phase = Phase.COUNTER;
                        }
                        else
                        {
                            _phase = Phase.WAIT_END;
                        }
                    }
                    break;
                case Phase.WAIT_PARRY_RESULT:
                    // パリィイベント開始まで更新
                    // 攻撃側キャラクターの攻撃モーションからパリィ開始メソッドが呼ばれるため、
                    // 開始されない(parryCtrl.IsActiveがfalse)まま、攻撃更新が行われることは想定外
                    if( _updateAttackerAttack( _departure, _destination ) )
                    {
                        Debug.Assert( false );
                        _phase = Phase.ATTACK;
                    }

                    ParrySkillHandler parrySkillHdlr = _combatSkillCtrl.CurrentSkillHandler as ParrySkillHandler;
                    if( !parrySkillHdlr.IsMatchResult( JudgeResult.NONE ) )
                    {
                        // パリィ結果が出た場合はパリィスキルハンドラを登録解除
                        _combatSkillCtrl.Unregister<ParrySkillHandler>();

                        // パリィ失敗の場合は通常の攻撃フェーズへ移行(失敗時の被ダメージ倍率はParryControler側がパリィ判定時に処理)
                        if( parrySkillHdlr.IsMatchResult( JudgeResult.FAILED ) )
                        {
                            _phase = Phase.ATTACK;
                        }
                        else
                        {
                            // パリィ用更新に切り替えます
                            ToggleParryUpdate( _attackCharacter, _targetCharacter );

                            _phase = Phase.EXEC_PARRY;
                        }
                    }
                    break;
                case Phase.EXEC_PARRY:
                    if( _updateTargetAttack( _departure, _destination ) )
                    {
                        OnAttackTurnFinished();

                        if( _attackCharacter.GetStatusRef.IsDead() )
                        {
                            _diedCharacter = _attackCharacter;
                            _phase = Phase.DIE;
                        }
                        else
                        {
                            // ダメージUIを非表示（パリィ成功 → 攻撃キャラがダメージを受けた）
                            _uiSystem.BattleUi.HideDamageOnCharacter( _attackCharacter );

                            _phase = Phase.WAIT_END;
                        }
                    }
                    break;
                case Phase.COUNTER:
                    if( _updateTargetAttack( _departure, _destination ) )
                    {
                        OnAttackTurnFinished();

                        if( _attackCharacter.GetStatusRef.IsDead() )
                        {
                            _diedCharacter = _attackCharacter;
                            _phase = Phase.DIE;
                        }
                        else
                        {
                            _uiSystem.BattleUi.HideDamageOnCharacter( _attackCharacter );  // ダメージUIを非表示（カウンター → 攻撃キャラがダメージを受けた）

                            _phase = Phase.WAIT_END;
                        }
                    }
                    break;
                case Phase.DIE:
                    if( _targetCharacter.AnimCtrl.IsEndAnimationOnConditionTag( AnimDatas.AnimeConditionsTag.DIE ) )
                    {
                        _phase = Phase.WAIT_END;
                    }
                    break;
                case Phase.WAIT_END:
                    if( WaitEndTime <= ( _elapsedTime += DeltaTimeProvider.DeltaTime ) )
                    {
                        _elapsedTime = 0f;

                        OnFinishAttack();

                        _phase = Phase.END;
                    }
                    break;
                case Phase.END:
                    if( UpdateFinishing() )
                    {
                        // 対戦相手設定をリセット
                        _attackCharacter?.BattleLogic.ResetOnEndOfAttackSequence();
                        _targetCharacter?.BattleLogic.ResetOnEndOfAttackSequence();

                        return true;
                    }
                    break;
            }

            return false;
        }

        /// <summary>
        /// 死亡キャラクターを取得します
        /// </summary>
        /// <returns>死亡キャラクター</returns>
        public Character GetDiedCharacter() { return _diedCharacter; }

        // =========================================================
        // 派生クラスで演出を実装するためのフック
        // =========================================================

        /// <summary>近接攻撃時に使用する戦闘アニメーションの種別</summary>
        protected abstract COMBAT_ANIMATION_TYPE ClosedAnimationType { get; }

        /// <summary>攻撃準備が整ってから、実際に攻撃を開始するまでの待ち時間(秒)</summary>
        protected virtual float WaitAttackTime => Constants.ATTACK_SEQUENCE_WAIT_ATTACK_TIME;

        /// <summary>全ての攻撃動作が終わってから、OnFinishAttack を呼ぶまでの待ち時間(秒)</summary>
        protected virtual float WaitEndTime => Constants.ATTACK_SEQUENCE_WAIT_END_TIME;

        /// <summary>シーケンス開始時に呼ばれます</summary>
        protected abstract void OnStart();

        /// <summary>
        /// 向きの変更中に毎フレーム呼ばれ、攻撃を開始してよいかを返します
        /// </summary>
        /// <param name="rotateRate">向きの変更の進捗(0～1)</param>
        protected abstract bool IsReadyToAttack( float rotateRate );

        /// <summary>IsReadyToAttack が true を返した時点で一度だけ呼ばれます</summary>
        protected virtual void OnReadyToAttack() { }

        /// <summary>攻撃・パリィ・カウンターのそれぞれの動作が終了するたびに呼ばれます</summary>
        protected virtual void OnAttackTurnFinished() { }

        /// <summary>全ての攻撃動作が終わり、終了待ちの時間が経過した時点で一度だけ呼ばれます</summary>
        protected abstract void OnFinishAttack();

        /// <summary>OnFinishAttack 後に毎フレーム呼ばれ、シーケンスを終了してよいかを返します</summary>
        protected abstract bool UpdateFinishing();

        // =========================================================
        // 内部処理
        // =========================================================

        /// <summary>
        /// 攻撃キャラと被攻撃キャラ間との攻撃処理を実行します
        /// </summary>
        /// <param name="attacker">攻撃キャラクター</param>
        /// <param name="target">被攻撃キャラクター</param>
        private void StartAttack( Character attacker, Character target )
        {
            // キャラクターの攻撃タイプによって動作するアニメーションを変更する
            _departure = attacker.transform.position;
            _destination = target.transform.position + target.transform.forward;
            attacker.BattleLogic.CombatAnimSeq.StartSequence();

            // 攻撃受け手用の設定をセット
            target.BattleLogic.SetReceiveAttackSetting();

            // ターゲットがガードスキル使用時はガードモーションを再生
            if( 0 <= target.BattleLogic.GetUsingSkillSlotIndexById( SkillID.GUARD ) ) target.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.GUARD, true );
        }

        /// <summary>
        /// 被攻撃キャラからのカウンター処理を開始します
        /// </summary>
        /// <param name="attacker">攻撃キャラクター</param>
        /// <param name="target">被攻撃キャラクター</param>
        private void StartCounter( Character attacker, Character target )
        {
            // ダメージ予測をセット
            _btlRtnCtrl.BtlCharaCdr.ApplyDamageExpect( target, attacker );

            // 攻撃キャラと被攻撃キャラを入れ替えて開始
            StartAttack( target, attacker );
        }

        /// <summary>
        /// 攻撃キャラと被攻撃キャラ間の更新処理をパリィ用のものに切り替えます
        /// </summary>
        /// <param name="attacker">攻撃キャラクター</param>
        /// <param name="target">被攻撃キャラクター</param>
        private void ToggleParryUpdate( Character attacker, Character target )
        {
            // 更新用関数を切り替え
            _updateAttackerAttack = ( in Vector3 arg1, in Vector3 arg2 ) => false;  // 攻撃側は何もしない
            _updateTargetAttack = _targetCharacter.BattleLogic.CombatAnimSeq.UpdateSequence;
        }
    }
}
