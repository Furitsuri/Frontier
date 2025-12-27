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
        public DeploymentCharacterSelectUI CharacterSelectUi;

        [Header( "配置完了確認用UI" )]
        public ConfirmUI ConfirmCompleted;

        [Header( "グリッドカーソルが選択中のキャラクターのパラメータUI" )]
        public CharacterParameterUI GridCursorSelectCharaParam;

        public void Init()
        {
            gameObject.SetActive( true );
            GridCursorSelectCharaParam.Setup();

            CharacterSelectUi.Init();
            ConfirmCompleted.Init();
            GridCursorSelectCharaParam.Init();

            DeployMessage.SetActive( true );
            CharacterSelectUi.gameObject.SetActive( true );
            ConfirmCompleted.gameObject.SetActive( false );
            GridCursorSelectCharaParam.gameObject.SetActive( false );
        }

        public void Exit()
        {
            gameObject.SetActive( false );

            DeployMessage.SetActive( false );
            CharacterSelectUi.gameObject.SetActive( false );
            ConfirmCompleted.gameObject.SetActive( false );
            GridCursorSelectCharaParam.gameObject.SetActive( false );
        }

        public void Setup()
        {
            CharacterSelectUi?.Setup();
            ConfirmCompleted?.Setup();
            GridCursorSelectCharaParam?.Setup();
        }
    }
}