using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    /// <summary>
    /// キーガイド関連の制御を行います
    /// </summary>
    public class KeyGuideController : MonoBehaviour
    {
        /// <summary>
        /// 各キーのアイコン
        /// </summary>
        public enum KeyIcon : int
        {
            UP = 0,     // 上
            DOWN,       // 下
            LEFT,       // 左
            RIGHT,      // 右
            DECISION,   // 決定
            CANCEL,     // 戻る
        }

        /// <summary>
        /// キーのアイコンとその説明文の構造体
        /// </summary>
        public struct KeyGuide
        {
            // キーアイコン
            public KeyIcon type;
            // アイコンに対する説明文
            public string explanation;
        }

        // 現在の状況において、有効となるキーとそれを押下した際の説明
        List<KeyGuide> _keyGuides;



        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        /// <summary>
        /// 遷移先のキーガイドを設定します
        /// </summary>
        /// <param name="guides"></param>
        public void Transit(List<KeyGuide> guides )
        {
            _keyGuides = guides;
        }
    }
}