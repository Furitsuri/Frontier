using Frontier.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        [Header( "Option" )]
        public OptionUI OptionView;                 // オプションUI

        [Header( "SaveLoad" )]
        public SaveLoadUI SaveLoadView;              // セーブ/ロードUI

        [Header( "TroopEdit" )]
        public TroopEditUI TroopEditView;            // 部隊編集UI

        [Header( "CharacterEdit" )]
        public CharacterEditUI CharacterEditView;    // キャラクター編集UI

        [Header( "FieldHeader" )]
        public FieldHeaderUI FieldHeaderView;        // フィールド画面右上の常時表示HUD

        [Header( "TalkWindow" )]
        public TalkWindowUI TalkWindowView;          // 話者名+セリフ+ポートレートを表示する汎用ウィンドウ

        private Dictionary<string, TalkEntryData> _talkEntries = null;

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
            OptionView?.Setup();
            SaveLoadView?.Setup();
            TroopEditView?.Setup();
            CharacterEditView?.Setup();
            FieldHeaderView?.Setup();
            TalkWindowView?.Setup();
        }

        public Vector2 GetScreenSize()
        {
            RectTransform canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            return canvasRect.sizeDelta;
        }

        /// <summary>
        /// 現在のシーンのトークデータ(Assets/Resources/TalkData/{シーン名}.json)から
        /// 指定IDの発言をトークウィンドウに表示します。
        /// </summary>
        public void ShowTalk( string entryId )
        {
            _talkEntries ??= TalkWindowDataLoader.Load( SceneManager.GetActiveScene().name );

            if( !_talkEntries.TryGetValue( entryId, out var entry ) )
            {
                Debug.LogWarning( $"[GeneralUISystem] トークデータが見つかりません: {entryId}" );
                return;
            }

            Sprite portrait = string.IsNullOrEmpty( entry.PortraitKey ) ? null : Resources.Load<Sprite>( $"Sprites/Portraits/{entry.PortraitKey}" );
            TalkWindowView.Show( entry.SpeakerName, entry.Message, portrait );
        }

        public void HideTalk()
        {
            TalkWindowView.Hide();
        }
    }
}