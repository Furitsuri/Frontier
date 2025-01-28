using Frontier;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class GeneralUISystem : MonoBehaviour
    {
        public static GeneralUISystem Instance { get; private set; }

        [Header("InputGuidePresenter")]
        public InputGuidePresenter InputGuideView;  // キーガイド表示

        void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// キーガイド表示用のリストを設定します
        /// </summary>
        /// <param name="keyGuideList">表示するキーガイドリスト</param>
        public void SetInputGuideList(List<InputGuideUI.InputGuide> keyGuideList)
        {
            InputGuideView.SetGuides(keyGuideList);
        }
    }
}