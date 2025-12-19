using Frontier.UI;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class GeneralUISystem : MonoBehaviour
    {
        [Header( "InputGuidePresenter" )]
        public InputGuidePresenter InputGuideView;  // 入力ガイド表示

        [Header( "TutorialPresenter" )]
        public TutorialPresenter TutorialView;      // チュートリアル表示

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

            TutorialView.Init();
        }

        public void Setup()
        {
            InputGuideView.Setup();
            TutorialView.Setup();
            CharacterStatusView.Setup();
            ToolTipView.Setup();
        }
    }
}