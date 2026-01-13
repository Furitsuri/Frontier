using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Frontier.Entities;
using Zenject;

namespace Froniter.StateMachine
{
    /// <summary>
    /// 配置候補キャラクターを表すクラス
    /// </summary>
    public class DeploymentCandidate
    {
        private Character _character;
        private Texture2D _candidateImg;
        public bool IsDeployed { get; set; }
        public Character Character => _character;
        public Texture2D CandidateImg => _candidateImg;

        public void Init( Character character, Texture2D img )
        {
            _character      = character;
            _candidateImg   = img;
            IsDeployed      = false;
        }
    }
}