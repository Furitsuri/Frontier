using System.Collections;
using System.Collections.Generic;
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

    Phase phase;
    private Character attackCharacter = null;
    private Character targetCharacter = null;
    // Transform‚Í’x‚¢‚½‚ßƒLƒƒƒbƒVƒ…
    private Transform atkCharaTransform = null;
    private Transform tgtCharaTransform = null;

    public CharacterAttackSequence( Character attackChara, Character targetChara )
    {
        attackCharacter = attackChara;
        targetCharacter = targetChara;
        atkCharaTransform = attackCharacter.transform;
        tgtCharaTransform = targetCharacter.transform;
    }

    // Update is called once per frame
    public bool Update()
    {
        switch(phase)
        {
            case Phase.START:
                atkCharaTransform.LookAt(tgtCharaTransform);
                tgtCharaTransform.LookAt(atkCharaTransform);
                break;
            case Phase.ATTACK:
                break;
            case Phase.COUNTER:
                break;
            case Phase.END:
                return true;

        }

        return false;
    }
}
