using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Frontier.Entities;

namespace Frontier.UI
{
    public class StatusUI : UiMonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI TMPLevelValue;
        [SerializeField] private TextMeshProUGUI TMPHPValue;
        [SerializeField] private TextMeshProUGUI TMPMoveValue;
        [SerializeField] private TextMeshProUGUI TMPJumpValue;
        [SerializeField] private TextMeshProUGUI TMPActionValue;
        [SerializeField] private TextMeshProUGUI TMPAttackValue;
        [SerializeField] private TextMeshProUGUI TMPDeffenceValue;
        [SerializeField] private RawImage _characterSnapshot = null;
        [SerializeField] private SkillBoxUI[] SkillBoxes;

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

            // スキルボックスUIの表示
            for( int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                SkillBoxes[i].ApplySkill( chara, i );
            }
        }

        public ( float, float ) GetSnapshotRectSize()
        {
            if( null == _characterSnapshot )
            {
                return ( 0, 0 );
            }
            return ( _characterSnapshot.rectTransform.rect.width, _characterSnapshot.rectTransform.rect.height);
        }

        override public void Setup()
        {
            foreach( var item in SkillBoxes )
            {
                item.Setup();
            }
        }
    }
}