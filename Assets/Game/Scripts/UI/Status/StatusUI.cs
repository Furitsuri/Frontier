using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Frontier.Entities;

namespace Frontier.UI
{
    public class StatusUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI TMPLevelValue;
        [SerializeField] private TextMeshProUGUI TMPHPValue;
        [SerializeField] private TextMeshProUGUI TMPMoveValue;
        [SerializeField] private TextMeshProUGUI TMPJumpValue;
        [SerializeField] private TextMeshProUGUI TMPActionValue;
        [SerializeField] private TextMeshProUGUI TMPAttackValue;
        [SerializeField] private TextMeshProUGUI TMPDeffenceValue;
        [SerializeField] private RawImage _characterSnapshot = null;

        public void AssignCharacter( Character chara )
        {
            var charaParam = chara.Params.CharacterParam;

            TMPLevelValue.text      = charaParam.Level.ToString();
            TMPHPValue.text         = $"{charaParam.CurHP} / {charaParam.MaxHP}";
            TMPMoveValue.text       = charaParam.moveRange.ToString();
            TMPJumpValue.text       = charaParam.jumpForce.ToString();
            TMPActionValue.text     = charaParam.maxActionGauge.ToString();
            TMPAttackValue.text     = charaParam.Atk.ToString();
            TMPDeffenceValue.text   = charaParam.Def.ToString();

            _characterSnapshot.texture = chara.Snapshot;
        }
    }
}