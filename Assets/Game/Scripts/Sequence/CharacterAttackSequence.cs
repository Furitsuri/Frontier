using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class CharacterAttackSequence
{
    enum Phase
    {
        START,
        ATTACK,
        COUNTER,
        DIE,
        END
    }

    private Phase _phase;
    private Character _attackCharacter  = null;
    private Character _targetCharacter  = null;
    private Character _diedCharacter    = null;
    // Transformは遅いためキャッシュ
    private Transform _atkCharaTransform = null;
    private Transform _tgtCharaTransform = null;
    private Quaternion _atkCharaInitialRot;
    private Quaternion _tgtCharaInitialRot;
    private float _startRotarionTimer = 0f;
    private float _elapsedTime = 0f;

    public void Init(Character attackChara, Character targetChara)
    {
        _attackCharacter    = attackChara;
        _targetCharacter    = targetChara;
        _diedCharacter      = null;
        _atkCharaTransform  = _attackCharacter.transform;
        _tgtCharaTransform  = _targetCharacter.transform;

        _startRotarionTimer = 0f;
        _atkCharaInitialRot = _atkCharaTransform.rotation;
        _tgtCharaInitialRot = _tgtCharaTransform.rotation;

        // 対戦相手として設定
        _attackCharacter.SetOpponentCharacter(_targetCharacter);
        _targetCharacter.SetOpponentCharacter(_attackCharacter);

        _elapsedTime    = 0f;
        _phase          = Phase.START;
    }

    // Update is called once per frame
    public bool Update()
    {
        switch(_phase)
        {
            case Phase.START:
                // START_ROTATION_TIMEが経過するまで向きを変更します
                _startRotarionTimer += Time.deltaTime;
                float t = Mathf.Clamp01(_startRotarionTimer/ Constants.ATTACK_ROTATIION_TIME);
                t = Mathf.SmoothStep(0f, 1f, t);

                Quaternion destAttackerRot  = Quaternion.LookRotation(_tgtCharaTransform.position - _atkCharaTransform.position);
                Quaternion destTargetRot    = Quaternion.LookRotation(_atkCharaTransform.position - _tgtCharaTransform.position);
                _atkCharaTransform.rotation = Quaternion.Lerp(_atkCharaInitialRot, destAttackerRot, t);
                _tgtCharaTransform.rotation = Quaternion.Lerp(_tgtCharaInitialRot, destTargetRot, t);

                if ( Constants.ATTACK_ROTATIION_TIME <= _startRotarionTimer )
                {
                    _attackCharacter.setAnimator(Character.ANIME_TAG.ATTACK_01);

                    _phase = Phase.ATTACK;
                }
                break;
            case Phase.ATTACK:
                if (_attackCharacter.IsEndAnimation(Character.ANIME_TAG.ATTACK_01))
                {
                    if (_targetCharacter.IsDead())
                    {
                        _diedCharacter = _targetCharacter;
                        _phase = Phase.DIE;
                    }
                    else {
                        // ダメージUIを非表示
                        BattleUISystem.Instance.ToggleDamageUI(false);
                        _phase = Phase.COUNTER;
                    }
                }
                
                break;
            case Phase.COUNTER:
                if( Constants.ATTACK_SEQUENCE_WAIT_TIME < (_elapsedTime += Time.deltaTime) )
                {
                    if (_attackCharacter.IsDead())
                    {
                        _diedCharacter = _attackCharacter;
                        _phase = Phase.DIE;
                    }
                    else
                    {
                        _phase = Phase.END;
                    }
                }
                break;
            case Phase.DIE:
                if (_targetCharacter.IsEndAnimation(Character.ANIME_TAG.DIE))
                {
                    _phase = Phase.END;
                }
                break;
            case Phase.END:
                // ダメージUIを非表示
                BattleUISystem.Instance.ToggleDamageUI(false);
                return true;

        }

        return false;
    }

    /// <summary>
    /// 死亡キャラクターを返します
    /// </summary>
    /// <returns>死亡キャラクター</returns>
    public Character GetDiedCharacter() { return _diedCharacter; }
}
