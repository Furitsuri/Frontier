using Frontier.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// 配置UIシステム
    /// </summary>
    public class DeploymentUISystem : MonoBehaviour
    {
        [Header( "キャラクターの配置指示メッセージUI" )]
        public GameObject DeployMessage;

        [Header( "配置キャラクター選択UI" )]
        public CharacterSelectionUI CharacterSelectUI;

        [Header( "配置完了確認用UI" )]
        public ConfirmUI ConfirmCompletedUI;

        [Header( "グリッドカーソルが選択中のキャラクターのパラメータUI" )]
        public CharacterParameterUI GridCursorSelectCharaParam;

        public void Init()
        {
            gameObject.SetActive( true );
            GridCursorSelectCharaParam.Setup();

            CharacterSelectUI.Init( CharacterSelectionDisplayMode.Texture );
            ConfirmCompletedUI.Init();
            GridCursorSelectCharaParam.Init();

            DeployMessage.SetActive( true );
            CharacterSelectUI.gameObject.SetActive( true );
            ConfirmCompletedUI.gameObject.SetActive( false );
            GridCursorSelectCharaParam.gameObject.SetActive( false );
        }

        public void Exit()
        {
            gameObject.SetActive( false );

            DeployMessage.SetActive( false );
            CharacterSelectUI.gameObject.SetActive( false );
            ConfirmCompletedUI.gameObject.SetActive( false );
            GridCursorSelectCharaParam.gameObject.SetActive( false );
        }

        public void Setup()
        {
            CharacterSelectUI?.Setup();
            ConfirmCompletedUI?.Setup();
            GridCursorSelectCharaParam?.Setup();
        }
    }
}