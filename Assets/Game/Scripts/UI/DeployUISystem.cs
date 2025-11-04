using Frontier.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.UI
{
    /// <summary>
    /// 配置UIシステム
    /// </summary>
    public class DeployUISystem : MonoBehaviour
    {
        [Header("DeployMessage")]
        public GameObject DeployMessage;              // 配置メッセージ

        [Header( "ConfirmCompleted" )]
        public ConfirmTurnEndUI ConfirmCompleted;         // 配置完了確認UI

        public void Init()
        {
            gameObject.SetActive( true );

            DeployMessage.SetActive( true );
            ConfirmCompleted.gameObject.SetActive( false );
        }

        public void Exit()
        {
            gameObject.SetActive( false );

            DeployMessage.SetActive( false );
            ConfirmCompleted.gameObject.SetActive( false );
        }

        public void SetActiveConfirmUis( bool isActive )
        {
            DeployMessage.SetActive( !isActive );   // 配置メッセージはConfirmUIが表示されている間は非表示にする
            ConfirmCompleted.gameObject.SetActive( isActive );
        }

        public void ApplyTextColor2ConfirmCompleted( int selectIndex )
        {
            ConfirmCompleted.ApplyTextColor( selectIndex );
        }
    }
}