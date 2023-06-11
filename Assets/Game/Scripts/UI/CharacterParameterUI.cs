using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CharacterParameterUI : MonoBehaviour
{
    public TextMeshProUGUI m_TMPMaxHPValue;
    public TextMeshProUGUI m_TMPCurHPValue;
    public TextMeshProUGUI m_TMPAtkValue;
    public TextMeshProUGUI m_TMPDefValue;

    // Update is called once per frame
    void Update()
    {
        Character selectCharacter = BattleManager.instance.SearchCharacterFromCharaIndex(BattleManager.instance.SelectCharacterIndex);
        if (selectCharacter == null)
        {
            // TODO : ASSERTèàóù
            return;
        }
        var param = selectCharacter.param;

        m_TMPMaxHPValue.text = $"{param.MaxHP}";
        m_TMPCurHPValue.text = $"{param.CurHP}";
        m_TMPAtkValue.text = $"{param.Atk}";
        m_TMPDefValue.text = $"{param.Def}";
    }
}
