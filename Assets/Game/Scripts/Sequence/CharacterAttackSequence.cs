using Frontier.Stage;
using Frontier.Battle;
using Frontier.Combat;
using Frontier.Entities;
using UnityEngine;
using Zenject;

namespace Frontier
{
    public class CharacterAttackSequence
    {
        enum Phase
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

        delegate bool UpdateAttack(in Vector3 arg1, in Vector3 arg2);

        private Phase _phase;
        private float _elapsedTime      = 0f;
        private bool _counterConditions = false;
        private BattleRoutineController _btlRtnCtrl = null;
        private BattleCameraController _btlCamCtrl  = null;
        private StageController _stageCtrl          = null;
        private IUiSystem _uiSystem                  = null;
        private Character _attackCharacter          = null;
        private Character _targetCharacter          = null;
        private Character _diedCharacter            = null;
        // Transformは遅いためキャッシュ
        private Transform _atkCharaTransform = null;
        private Transform _tgtCharaTransform = null;
        private Vector3 _departure = Vector3.zero;
        private Vector3 _destination = Vector3.zero;
        private Quaternion _atkCharaInitialRot = Quaternion.identity;
        private Quaternion _tgtCharaInitialRot = Quaternion.identity;
        private UpdateAttack _updateAttackerAttack  = null;
        private UpdateAttack _updateTargetAttack    = null;
        private CombatSkillEventController _combatSkillCtrl = null;
        private ParrySkillNotifier _parryNotifier   = null;

        [Inject]
        public void Construct(BattleRoutineController btlRtnCtrl, StageController stgCtrl, CombatSkillEventController combatSkillCtrl, IUiSystem uiSystem)
        {
            _btlRtnCtrl         = btlRtnCtrl;
            _stageCtrl          = stgCtrl;
            _combatSkillCtrl    = combatSkillCtrl;
            _uiSystem           = uiSystem;
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        public void Init()
        {
            _btlCamCtrl         = _btlRtnCtrl.GetCameraController();
            _diedCharacter      = null;
            _elapsedTime        = 0f;
            _phase              = Phase.START;
        }

        /// <summary>
        /// シーケンスを開始します
        /// </summary>
        /// <param name="attackChara">攻撃キャラクター</param>
        /// <param name="targetChara">被攻撃キャラクター</param>
        public void StartSequence(Character attackChara, Character targetChara)
        {
            _attackCharacter    = attackChara;
            _targetCharacter    = targetChara;
            _atkCharaTransform  = _attackCharacter.transform;
            _tgtCharaTransform  = _targetCharacter.transform;
            _atkCharaInitialRot = _atkCharaTransform.rotation;
            _tgtCharaInitialRot = _tgtCharaTransform.rotation;

            // 対戦相手として設定
            _attackCharacter.SetOpponentCharacter(_targetCharacter);
            _targetCharacter.SetOpponentCharacter(_attackCharacter);

            // カウンター条件の設定
            _counterConditions = _targetCharacter.IsSkillInUse(SkillsData.ID.SKILL_COUNTER);

            // 攻撃更新処理の条件別設定
            if (_counterConditions && _attackCharacter.GetBullet() != null) _counterConditions = _targetCharacter.GetBullet() != null;
            if (_attackCharacter.GetBullet() == null) _updateAttackerAttack = _attackCharacter.UpdateClosedAttack;
            else _updateAttackerAttack = _attackCharacter.UpdateRangedAttack;
            if (_targetCharacter.GetBullet() == null) _updateTargetAttack = _targetCharacter.UpdateClosedAttack;
            else _updateTargetAttack = _targetCharacter.UpdateRangedAttack;

            // 攻撃シーケンスの開始
            _btlCamCtrl.StartAttackSequenceMode(_attackCharacter, _targetCharacter);
        }

        /// <summary>
        /// 処理を更新します
        /// </summary>
        /// <returns>処理の終了</returns>
        public bool Update()
        {
            switch (_phase)
            {
                case Phase.START:
                    // START_ROTATION_TIMEが経過するまで向きを変更します
                    _elapsedTime += Time.deltaTime;
                    float t = Mathf.Clamp01(_elapsedTime / Constants.ATTACK_ROTATIION_TIME);
                    t = Mathf.SmoothStep(0f, 1f, t);

                    Quaternion destAttackerRot  = Quaternion.LookRotation(_tgtCharaTransform.position - _atkCharaTransform.position);
                    Quaternion destTargetRot    = Quaternion.LookRotation(_atkCharaTransform.position - _tgtCharaTransform.position);
                    _atkCharaTransform.rotation = Quaternion.Lerp(_atkCharaInitialRot, destAttackerRot, t);
                    _tgtCharaTransform.rotation = Quaternion.Lerp(_tgtCharaInitialRot, destTargetRot, t);

                    if (_btlCamCtrl.IsFadeAttack())
                    {
                        _elapsedTime = 0f;

                        TransitBattleField(_attackCharacter, _targetCharacter);

                        _phase = Phase.WAIT_ATTACK;
                    }
                    break;
                case Phase.WAIT_ATTACK:
                    if (Constants.ATTACK_SEQUENCE_WAIT_ATTACK_TIME < (_elapsedTime += Time.deltaTime))
                    {
                        _elapsedTime = 0f;
                        StartAttack(_attackCharacter, _targetCharacter);

                        // パリィスキル使用時はパリィ判定専用処理へ遷移
                        if (_targetCharacter.IsSkillInUse(SkillsData.ID.SKILL_PARRY))
                        {
                            _combatSkillCtrl.Register<ParrySkillHandler>();

                            _parryNotifier = _targetCharacter.GetParrySkill;
                            _phase = Phase.WAIT_PARRY_RESULT;
                        }
                        // それ以外は通常通り攻撃へ
                        else _phase = Phase.ATTACK;
                    }
                    break;
                case Phase.ATTACK:
                    if (_updateAttackerAttack(_departure, _destination))
                    {
                        // カメラ対象とカメラパラメータを変更
                        _btlCamCtrl.TransitNextPhaseCameraParam(null, _targetCharacter.transform);
                        // ダメージUIを非表示
                        _uiSystem.BattleUi.ToggleDamageUI(false);

                        // ガードスキルを使用時はガードモーションを戻す
                        if (_targetCharacter.IsSkillInUse(SkillsData.ID.SKILL_GUARD)) _targetCharacter.AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.GUARD, false);

                        // 対象が死亡している場合は死亡処理へ
                        if (_targetCharacter.characterParam.IsDead())
                        {
                            _diedCharacter = _targetCharacter;
                            _phase = Phase.DIE;
                        }
                        // カウンタースキルが登録されている場合はカウンター処理へ
                        else if (_counterConditions)
                        {
                            StartCounter(_attackCharacter, _targetCharacter);

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
                    if (_updateAttackerAttack(_departure, _destination))
                    {
                        Debug.Assert(false);
                        _phase = Phase.ATTACK;
                    }

                    ParrySkillHandler parrySkillHdlr = _combatSkillCtrl.CurrentSkillHandler as ParrySkillHandler;
                    if ( !parrySkillHdlr.IsMatchResult( ParrySkillHandler.JudgeResult.NONE ) )
                    {
                        // パリィ結果が出た場合はパリィスキルハンドラを登録解除
                        _combatSkillCtrl.Unregister<ParrySkillHandler>();

                        // パリィ失敗の場合は通常の攻撃フェーズへ移行(失敗時の被ダメージ倍率はParryControler側がパリィ判定時に処理)
                        if ( parrySkillHdlr.IsMatchResult( ParrySkillHandler.JudgeResult.FAILED ) )
                        {
                            _phase = Phase.ATTACK;
                        }
                        else
                        {
                            // パリィ用更新に切り替えます
                            ToggleParryUpdate(_attackCharacter, _targetCharacter);

                            _phase = Phase.EXEC_PARRY;
                        }
                    }
                    break;
                case Phase.EXEC_PARRY:
                    if (_updateTargetAttack(_departure, _destination))
                    {
                        // カメラ対象とカメラパラメータを変更
                        _btlCamCtrl.TransitNextPhaseCameraParam(null, _targetCharacter.transform);

                        if (_attackCharacter.characterParam.IsDead())
                        {
                            _diedCharacter = _attackCharacter;
                            _phase = Phase.DIE;
                        }
                        else
                        {
                            // ダメージUIを非表示
                            _uiSystem.BattleUi.ToggleDamageUI(false);

                            _phase = Phase.WAIT_END;
                        }
                    }
                    break;
                case Phase.COUNTER:
                    if (_updateTargetAttack(_departure, _destination))
                    {
                        // カメラ対象とカメラパラメータを変更
                        _btlCamCtrl.TransitNextPhaseCameraParam(null, _targetCharacter.transform);

                        if (_attackCharacter.characterParam.IsDead())
                        {
                            _diedCharacter = _attackCharacter;
                            _phase = Phase.DIE;
                        }
                        else
                        {
                            // ダメージUIを非表示
                            _uiSystem.BattleUi.ToggleDamageUI(false);

                            _phase = Phase.WAIT_END;
                        }
                    }
                    break;
                case Phase.DIE:
                    if (_targetCharacter.AnimCtrl.IsEndAnimationOnConditionTag(AnimDatas.AnimeConditionsTag.DIE))
                    {
                        _phase = Phase.WAIT_END;
                    }
                    break;
                case Phase.WAIT_END:
                    if (Constants.ATTACK_SEQUENCE_WAIT_END_TIME < (_elapsedTime += Time.deltaTime))
                    {
                        _elapsedTime = 0f;

                        // バトルフィールドからステージフィールドに遷移
                        TransitStageField(_attackCharacter, _targetCharacter);

                        // 攻撃シーケンス用カメラを終了
                        var info = _stageCtrl.GetGridInfo(_attackCharacter.GetCurrentGridIndex());
                        _btlCamCtrl.EndAttackSequenceMode(_attackCharacter);

                        _phase = Phase.END;
                    }
                    break;
                case Phase.END:
                    if (_btlCamCtrl.IsFadeEnd())
                    {
                        // 対戦相手設定をリセット
                        _attackCharacter.ResetOnEndOfAttackSequence();
                        _targetCharacter.ResetOnEndOfAttackSequence();

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

        /// <summary>
        /// 攻撃キャラと被攻撃キャラ間との攻撃処理を実行します
        /// </summary>
        /// <param name="attacker">攻撃キャラクター</param>
        /// <param name="target">被攻撃キャラクター</param>
        private void StartAttack(Character attacker, Character target)
        {
            if (attacker.GetBullet() != null) attacker.StartRangedAttackSequence();
            else
            {
                _departure = attacker.transform.position;
                _destination = target.transform.position + target.transform.forward;    // 対象の前方1mを目標地点にする
                attacker.StartClosedAttackSequence();
            }

            // 攻撃受け手用の設定をセット
            target.SetReceiveAttackSetting();

            // ターゲットがガードスキル使用時はガードモーションを再生
            if (target.IsSkillInUse(SkillsData.ID.SKILL_GUARD)) target.AnimCtrl.SetAnimator(AnimDatas.AnimeConditionsTag.GUARD, true);
        }

        /// <summary>
        /// 被攻撃キャラからのカウンター処理を開始します
        /// </summary>
        /// <param name="attacker">攻撃キャラクター</param>
        /// <param name="target">被攻撃キャラクター</param>
        private void StartCounter(Character attacker, Character target)
        {
            // ダメージ予測をセット
            _btlRtnCtrl.BtlCharaCdr.ApplyDamageExpect(target, attacker);

            // 攻撃キャラと被攻撃キャラを入れ替えて開始
            StartAttack(target, attacker);
        }

        /// <summary>
        /// 攻撃キャラと被攻撃キャラ間の更新処理をパリィ用のものに切り替えます
        /// </summary>
        /// <param name="attacker">攻撃キャラクター</param>
        /// <param name="target">被攻撃キャラクター</param>
        private void ToggleParryUpdate(Character attacker, Character target)
        {
            // 更新用関数を切り替え
            _updateAttackerAttack   = _attackCharacter.UpdateParryOnAttacker;
            _updateTargetAttack     = _targetCharacter.UpdateParryOnTargeter;
        }

        /// <summary>
        /// 戦闘フィールドに遷移します
        /// </summary>
        /// <param name="attacker">攻撃キャラクター</param>
        /// <param name="target">被攻撃キャラクター</param>
        private void TransitBattleField(Character attacker, Character target)
        {
            // メッシュ及びattakerとtarget以外のキャラクターを非表示に
            _stageCtrl.ToggleMeshDisplay(false);

            foreach (var player in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable(Character.CHARACTER_TAG.PLAYER))
            {
                if (player != attacker && player != target)
                {
                    player.gameObject.SetActive(false);
                }
            }

            foreach (var enemy in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable(Character.CHARACTER_TAG.ENEMY))
            {
                if (enemy != attacker && enemy != target)
                {
                    enemy.gameObject.SetActive(false);
                }
            }

            // キャラクターをステージの中心位置からそれぞれ離れた場所に立たせる
            var centralPos = _stageCtrl.GetCentralPos();

            // 味方と敵対側で分別
            Character ally = null;
            Character opponent = null;
            if (attacker.characterParam.IsMatchCharacterTag(Character.CHARACTER_TAG.PLAYER))
            {
                ally = attacker;
                opponent = target;
            }
            else
            {
                if (target.characterParam.IsMatchCharacterTag(Character.CHARACTER_TAG.PLAYER))
                {
                    ally = target;
                    opponent = attacker;
                }
                else if (target.characterParam.IsMatchCharacterTag(Character.CHARACTER_TAG.OTHER))
                {
                    ally = target;
                    opponent = attacker;
                }
                else
                {
                    ally = attacker;
                    opponent = target;
                }
            }

            // 味方は奥行手前側、敵は奥行奥側の立ち位置とする
            Transform allyTransform     = ally.transform;
            Transform opponentTransform = opponent.transform;
            allyTransform.position      = centralPos + new Vector3(0f, 0f, -_stageCtrl.BattlePosLengthFromCentral);
            opponentTransform.position  = centralPos + new Vector3(0f, 0f, _stageCtrl.BattlePosLengthFromCentral);
            allyTransform.rotation      = Quaternion.LookRotation(centralPos - allyTransform.position);
            opponentTransform.rotation  = Quaternion.LookRotation(centralPos - opponentTransform.position);
            // カメラパラメータを戦闘フィールド用に設定
            _btlCamCtrl.AdaptBattleFieldSetting();
        }

        /// <summary>
        /// ステージフィールドに遷移します
        /// </summary>
        /// <param name="attacker">攻撃キャラクター</param>
        /// <param name="target">被攻撃キャラクター</param>
        private void TransitStageField(Character attacker, Character target)
        {
            // 非表示にしていたものを表示
            _stageCtrl.ToggleMeshDisplay(true);

            foreach (var player in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable(Character.CHARACTER_TAG.PLAYER))
            {
                player.gameObject.SetActive(true);
            }

            foreach (var enemy in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable(Character.CHARACTER_TAG.ENEMY))
            {
                enemy.gameObject.SetActive(true);
            }

            // キャラクターをステージの中心位置からそれぞれ離れた場所に立たせる
            var info = _stageCtrl.GetGridInfo(attacker.GetCurrentGridIndex());
            _attackCharacter.transform.position = info.charaStandPos;
            _attackCharacter.transform.rotation = _atkCharaInitialRot;
            info = _stageCtrl.GetGridInfo(target.GetCurrentGridIndex());
            _targetCharacter.transform.position = info.charaStandPos;
            _targetCharacter.transform.rotation = _tgtCharaInitialRot;
        }
    }
}