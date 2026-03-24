using Frontier.Combat;
using Frontier.Combat.Skill;
using Frontier.Entities;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Frontier.UI
{
    public class CharacterParameterUI : UiMonoBehaviour
    {
        [SerializeField] public int _layerMaskIndex = 0;
        [SerializeField] public float _cameraAngleY;
        [SerializeField] public float BlinkingDuration;
        [SerializeField] public TextMeshProUGUI TMPMaxHPValue;
        [SerializeField] public TextMeshProUGUI TMPCurHPValue;
        [SerializeField] public TextMeshProUGUI TMPAtkValue;
        [SerializeField] public TextMeshProUGUI TMPDefValue;
        [SerializeField] public TextMeshProUGUI TMPMovValue;
        [SerializeField] public TextMeshProUGUI TMPJmpValue;
        [SerializeField] public TextMeshProUGUI TMPAddAtkValue;
        [SerializeField] public TextMeshProUGUI TMPAddDefValue;
        [SerializeField] public TextMeshProUGUI TMPDiffHPValue;
        [SerializeField] public TextMeshProUGUI TMPActRecoveryValue;
        [SerializeField] public RawImage TargetImage;
        [SerializeField] public RawImage ActGaugeElemImage;
        [SerializeField] public RectTransform PanelTransform;
        [SerializeField] public SkillBoxUI[] SkillBoxes;

        /// <summary>
        /// テキストの色を反映します
        /// </summary>
        /// <param name="changeHP">HPの変動量</param>
        public void ApplyTextColor( int changeHP )
        {
            if( changeHP < 0 )
            {
                TMPDiffHPValue.color = Color.red;
            }
            else if( 0 < changeHP )
            {
                TMPDiffHPValue.color = Color.green;
            }
        }

        public override void Setup()
        {
            base.Setup();

            foreach( var item in SkillBoxes )
            {
                item.Setup();
            }
        }
    }
}