using Frontier;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class GeneralUISystem : MonoBehaviour
    {
        public static GeneralUISystem Instance { get; private set; }

        [Header("KeyGuidePresenter")]
        public KeyGuidePresenter KeyGuideView;  // キーガイド表示

        void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// キーガイド表示の遷移処理を行います
        /// </summary>
        /// <param name="keyGuideList">表示するキーガイドリスト</param>
        public void TransitKeyGuide(List<KeyGuideUI.KeyGuide> keyGuideList)
        {
            KeyGuideView.Transit(keyGuideList);
        }
    }
}