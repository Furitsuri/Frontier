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
        END
    }

    private Phase _phase;
    private Character _attackCharacter = null;
    private Character _targetCharacter = null;
    // Transformは遅いためキャッシュ
    private Transform _atkCharaTransform = null;
    private Transform _tgtCharaTransform = null;
    private Quaternion _atkCharaInitialRotation;
    private Quaternion _tgtCharaInitialRotation;
    private float _startRotarionTimer = 0f;
    private const float START_ROTATION_TIME = 0.2f;

    public void Init(Character attackChara, Character targetChara)
    {
        _attackCharacter = attackChara;
        _targetCharacter = targetChara;
        _atkCharaTransform = _attackCharacter.transform;
        _tgtCharaTransform = _targetCharacter.transform;

        _startRotarionTimer = 0f;
        _atkCharaInitialRotation = _atkCharaTransform.rotation;
        _tgtCharaInitialRotation = _tgtCharaTransform.rotation;
    }

    // Update is called once per frame
    public bool Update()
    {
        switch(_phase)
        {
            case Phase.START:
                // START_ROTATION_TIMEが経過するまで向きを変更します
                _startRotarionTimer += Time.deltaTime;
                float t = Mathf.Clamp01(_startRotarionTimer/START_ROTATION_TIME);
                t = Mathf.SmoothStep(0f, 1f, t);

                Quaternion newRotation = Quaternion.LookRotation(_tgtCharaTransform.position - _atkCharaTransform.position);
                _atkCharaTransform.rotation = Quaternion.Lerp(_atkCharaInitialRotation, newRotation, t);
                _tgtCharaTransform.rotation = Quaternion.Lerp(_tgtCharaInitialRotation, Quaternion.Inverse(newRotation), t);

                if ( START_ROTATION_TIME <= _startRotarionTimer )
                {
                    _phase = Phase.ATTACK;
                }
                break;
            case Phase.ATTACK:
                _attackCharacter.setAnimator(Character.ANIME_TAG.ANIME_TAG_ATTACK_01);
                break;
            case Phase.COUNTER:
                break;
            case Phase.END:
                return true;

        }

        return false;
    }
}
