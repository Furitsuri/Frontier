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
        [Header( "DeployMessage" )]
        public GameObject DeployMessage;                // 配置メッセージ

        [Header( "CharacterSelect" )]
        public DeploymentCharacterSelectUI CharacterSelectUi; // キャラクター選択UI

        [Header( "ConfirmCompleted" )]
        public ConfirmTurnEndUI ConfirmCompleted;       // 配置完了確認UI

        public void Init()
        {
            gameObject.SetActive( true );

            DeployMessage.SetActive( true );
            CharacterSelectUi.gameObject.SetActive( true );
            ConfirmCompleted.gameObject.SetActive( false );
        }

        public void Exit()
        {
            gameObject.SetActive( false );

            DeployMessage.SetActive( false );
            CharacterSelectUi.gameObject.SetActive( false );
            ConfirmCompleted.gameObject.SetActive( false );
        }
    }
}