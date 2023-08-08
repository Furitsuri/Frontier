using System.Collections;
using System.Collections.Generic;
using System.Threading;
using TMPro.Examples;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class CharacterAttackSequence
{
    enum Phase
    {
        START,
        WAIT_ATTACK,
        ATTACK,
        PARRY,
        COUNTER,
        DIE,
        WAIT_END,
        END
    }

    delegate bool UpdateAttack(in Vector3 arg1, in Vector3 arg2);

    private Phase _phase;
    private Character _attackCharacter              = null;
    private Character _targetCharacter              = null;
    private Character _diedCharacter                = null;
    // Transformは遅いためキャッシュ
    private Transform _atkCharaTransform            = null;
    private Transform _tgtCharaTransform            = null;
    private Vector3 _departure                      = Vector3.zero;
    private Vector3 _destination                    = Vector3.zero;
    private Quaternion _atkCharaInitialRot          = Quaternion.identity;
    private Quaternion _tgtCharaInitialRot          = Quaternion.identity;
    private float _elapsedTime                      = 0f;
    private Character.ANIME_TAG[] _attackAnimTags   = new Character.ANIME_TAG[3] { Character.ANIME_TAG.SINGLE_ATTACK, Character.ANIME_TAG.DOUBLE_ATTACK, Character.ANIME_TAG.TRIPLE_ATTACK };
    private bool _CounterConditions                 = false;
    private UpdateAttack _updateAttackerAttack      = null;
    private UpdateAttack _updateTargetAttack        = null;

    public void Init(Character attackChara, Character targetChara)
    {
        _attackCharacter        = attackChara;
        _targetCharacter        = targetChara;
        _diedCharacter          = null;
        _atkCharaTransform      = _attackCharacter.transform;
        _tgtCharaTransform      = _targetCharacter.transform;
        _atkCharaInitialRot     = _atkCharaTransform.rotation;
        _tgtCharaInitialRot     = _tgtCharaTransform.rotation;
        _elapsedTime            = 0f;
        _phase                  = Phase.START;
        // 対戦相手として設定
        _attackCharacter.SetOpponentCharacter(_targetCharacter);
        _targetCharacter.SetOpponentCharacter(_attackCharacter);
        // カウンター条件の設定
        _CounterConditions      = (_targetCharacter.IsSkillInUse(SkillsData.ID.SKILL_COUNTER));
        // 攻撃更新処理の条件別設定
        if (_CounterConditions && _attackCharacter.GetBullet() != null ) _CounterConditions = _targetCharacter.GetBullet() != null;
        if (_attackCharacter.GetBullet() == null) _updateAttackerAttack = _attackCharacter.UpdateClosedAttack;
        else _updateAttackerAttack = _attackCharacter.UpdateRangedAttack;
        if (_targetCharacter.GetBullet() == null) _updateTargetAttack = _targetCharacter.UpdateClosedAttack;
        else _updateTargetAttack = _targetCharacter.UpdateRangedAttack;

        BattleCameraController.Instance.StartAttackSequenceMode( attackChara, targetChara );
    }

    // Update is called once per frame
    public bool Update()
    {
        switch(_phase)
        {
            case Phase.START:
                // START_ROTATION_TIMEが経過するまで向きを変更します
                _elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(_elapsedTime/ Constants.ATTACK_ROTATIION_TIME);
                t = Mathf.SmoothStep(0f, 1f, t);

                Quaternion destAttackerRot  = Quaternion.LookRotation(_tgtCharaTransform.position - _atkCharaTransform.position);
                Quaternion destTargetRot    = Quaternion.LookRotation(_atkCharaTransform.position - _tgtCharaTransform.position);
                _atkCharaTransform.rotation = Quaternion.Lerp(_atkCharaInitialRot, destAttackerRot, t);
                _tgtCharaTransform.rotation = Quaternion.Lerp(_tgtCharaInitialRot, destTargetRot, t);

                if( BattleCameraController.Instance.IsFadeAttack() )
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

                    // ガードスキル使用時はガードモーションを再生
                    if ( _targetCharacter.IsSkillInUse( SkillsData.ID.SKILL_GUARD ) ) _targetCharacter.setAnimator(Character.ANIME_TAG.GUARD, true);
                    // パリィスキル使用時はパリィ判定専用処理へ遷移
                    if (_targetCharacter.IsSkillInUse(SkillsData.ID.SKILL_PARRY)) _phase = Phase.PARRY;
                    else
                    {
                        _phase = Phase.ATTACK;
                    }
                }
                break;
            case Phase.ATTACK:
                if(_updateAttackerAttack(_departure, _destination))
                {
                    // カメラ対象とカメラパラメータを変更
                    BattleCameraController.Instance.TransitNextPhaseCameraParam(null, _targetCharacter.transform);
                    // ガードスキルを使用時はガードモーションを戻す
                    if (_targetCharacter.IsSkillInUse(SkillsData.ID.SKILL_GUARD)) _targetCharacter.setAnimator(Character.ANIME_TAG.GUARD, false);
                    // 対象が死亡している場合は死亡処理へ
                    if (_targetCharacter.IsDead())
                    {
                        _diedCharacter = _targetCharacter;
                        _phase = Phase.DIE;
                    }
                    else if (_CounterConditions)
                    {
                        BattleManager.Instance.ApplyDamageExpect(_targetCharacter, _attackCharacter);
                        StartAttack(_targetCharacter, _attackCharacter);

                        _phase = Phase.COUNTER;
                    }
                    else _phase = Phase.WAIT_END;
                }
                break;
            case Phase.PARRY:
                break;
            case Phase.COUNTER:
                if (_updateTargetAttack(_departure, _destination))
                {
                    // カメラ対象とカメラパラメータを変更
                    BattleCameraController.Instance.TransitNextPhaseCameraParam(null, _targetCharacter.transform);

                    if (_attackCharacter.IsDead())
                    {
                        _diedCharacter = _attackCharacter;
                        _phase = Phase.DIE;
                    }
                    else _phase = Phase.WAIT_END;
                }
                break;
            case Phase.DIE:
                if (_targetCharacter.IsEndAnimation(Character.ANIME_TAG.DIE))
                {
                    _phase = Phase.WAIT_END;
                }
                break;
            case Phase.WAIT_END:
                if (Constants.ATTACK_SEQUENCE_WAIT_END_TIME < (_elapsedTime += Time.deltaTime))
                {
                    _elapsedTime = 0f;
                    // ダメージUIを非表示
                    BattleUISystem.Instance.ToggleDamageUI(false);
                    // バトルフィールドからステージフィールドに遷移
                    TransitStageField(_attackCharacter, _targetCharacter);
                    // 攻撃シーケンス用カメラを終了
                    var info = StageGrid.Instance.GetGridInfo( _attackCharacter.tmpParam.gridIndex );
                    BattleCameraController.Instance.EndAttackSequenceMode(_attackCharacter);

                    _phase = Phase.END;
                }
                break;
            case Phase.END:
                if (BattleCameraController.Instance.IsFadeEnd())
                {
                    // 対戦相手設定をリセット
                    _attackCharacter.ResetOpponentCharacter();
                    _targetCharacter.ResetOpponentCharacter();

                    return true;
                }
                break;
        }

        return false;
    }

    /// <summary>
    /// 死亡キャラクターを返します
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
        var tag = _attackAnimTags[attacker.skillModifiedParam.AtkNum - 1];
        if (attacker.GetBullet() != null) attacker.setAnimator(tag);
        else
        {
            _departure      = attacker.transform.position;
            _destination    = target.transform.position + target.transform.forward;
            attacker.PlayClosedAttack();
        }
    }

    /// <summary>
    /// 戦闘フィールドに遷移します
    /// </summary>
    /// <param name="attacker">攻撃キャラクター</param>
    /// <param name="target">被攻撃キャラクター</param>
    private void TransitBattleField(Character attacker, Character target)
    {
        var stgGrid = StageGrid.Instance;

        // メッシュ及びattakerとtarget以外のキャラクターを非表示に
        stgGrid.ToggleMeshDisplay(false);
        foreach (var player in BattleManager.Instance.GetPlayerEnumerable())
        {
            if (player != attacker && player != target)
            {
                player.gameObject.SetActive(false);
            }
        }
        foreach (var enemy in BattleManager.Instance.GetEnemyEnumerable())
        {
            if (enemy != attacker && enemy != target)
            {
                enemy.gameObject.SetActive(false);
            }
        }

        // キャラクターをステージの中心位置からそれぞれ離れた場所に立たせる
        var centralPos = stgGrid.transform.position;

        // 味方と敵対側で分別
        Character ally  = null;
        Character opponent = null;
        if( attacker.IsPlayer())
        {
            ally        = attacker;
            opponent    = target;
        }
        else
        {
            if( target.IsPlayer() )
            {
                ally        = target;
                opponent    = attacker;
            }
            else if( target.IsOther() )
            {
                ally        = target;
                opponent    = attacker;
            }
            else
            {
                ally        = attacker;
                opponent    = target;
            }
        }
        
        // 味方は奥行手前側、敵は奥行奥側の立ち位置とする
        Transform allyTransform     = ally.transform;
        Transform opponentTransform = opponent.transform;
        allyTransform.position      = centralPos + new Vector3(0f, 0f, -stgGrid.BattlePosLengthFromCentral);
        opponentTransform.position  = centralPos + new Vector3(0f, 0f, stgGrid.BattlePosLengthFromCentral);
        allyTransform.rotation      = Quaternion.LookRotation(centralPos - allyTransform.position);
        opponentTransform.rotation  = Quaternion.LookRotation(centralPos - opponentTransform.position);
    }

    /// <summary>
    /// ステージフィールドに遷移します
    /// </summary>
    /// <param name="attacker">攻撃キャラクター</param>
    /// <param name="target">被攻撃キャラクター</param>
    private void TransitStageField(Character attacker, Character target)
    {
        var stgGrid = StageGrid.Instance;

        // 非表示にしていたものを表示
        stgGrid.ToggleMeshDisplay(true);
        foreach (var player in BattleManager.Instance.GetPlayerEnumerable())
        {
            player.gameObject.SetActive(true);
        }
        foreach (var enemy in BattleManager.Instance.GetEnemyEnumerable())
        {
            enemy.gameObject.SetActive(true);
        }

        // キャラクターをステージの中心位置からそれぞれ離れた場所に立たせる
        var info = StageGrid.Instance.GetGridInfo(attacker.tmpParam.gridIndex);
        _attackCharacter.transform.position = info.charaStandPos;
        _attackCharacter.transform.rotation = _atkCharaInitialRot;
        info = StageGrid.Instance.GetGridInfo(target.tmpParam.gridIndex);
        _targetCharacter.transform.position = info.charaStandPos;
        _targetCharacter.transform.rotation = _tgtCharaInitialRot;
    }
}
