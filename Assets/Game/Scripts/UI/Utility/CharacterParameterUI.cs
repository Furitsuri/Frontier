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
        [SerializeField] public TextMeshProUGUI TMPExpValue;
        [SerializeField] public TextMeshProUGUI TMPStatusPointValue;
        [SerializeField] public RawImage TargetImage;
        [Header( "TargetImageと同サイズのマスク内に配置する、キャラクター切替スライド演出専用のRawImage(未使用のシーンではnullで構いません)" )]
        [SerializeField] public RawImage IncomingTargetImage;
        [SerializeField] public RawImage ActGaugeElemImage;
        [SerializeField] public RectTransform PanelTransform;
        [SerializeField] public SkillBoxUI[] SkillBoxes;

        /// <summary>
        /// このUIのルートに付いている背景Image。勢力に応じた色はBattleRoutinePresenter側で決定し、
        /// このプロパティ経由でscriptから適用する(Hierarchy側では色を設定しない)
        /// </summary>
        public Image Background { get; private set; }

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

            Background = GetComponent<Image>();

            foreach( var item in SkillBoxes )
            {
                item.Setup();
            }
        }
    }
}