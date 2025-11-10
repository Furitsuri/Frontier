using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontier.Entities;

namespace Froniter.StateMachine
{
    /// <summary>
    /// 配置候補キャラクターを表すクラス
    /// </summary>
    public class DeploymentCandidate
    {
        public Character Character { get; }
        public bool IsDeployed { get; set; }

        public DeploymentCandidate( Character character )
        {
            Character = character;
            IsDeployed = false;
        }

        public void InitCharacterPosition( in Vector3 position )
        {
            Character.GetTransformHandler.SetPosition( position );
        }
    }
}