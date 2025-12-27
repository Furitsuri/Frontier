using Frontier.UI;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class GeneralUISystem : MonoBehaviour
    {
        [Header( "InputGuide" )]
        public InputGuideBarUI InputGuideView;     // 入力ガイド表示

        [Header( "Tutorial" )]
        public TutorialUI TutorialView;             // チュートリアルUI

        [Header( "CharacterStatuts" )]
        public StatusUI CharacterStatusView;        // キャラクターステータスUI

        [Header( "ToolTip" )]
        public TooltipUI ToolTipView;               // ツールチップUI

        void Awake()
        {
            if( null == GetComponent<Canvas>() )
            {
                LogHelper.LogError( "Canvas component is missing on GeneralUISystem GameObject." );
            }
        }

        public void Setup()
        {
            InputGuideView?.Setup();
            TutorialView?.Setup();
            CharacterStatusView?.Setup();
            ToolTipView?.Setup();
        }
    }
}