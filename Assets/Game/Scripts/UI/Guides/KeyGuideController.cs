using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;

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

            NUM,
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

        [SerializeField]
        [Header("UIスクリプト")]
        private KeyGuideUI _ui = null;

        // 各スプライトファイル名の末尾の番号
        string[] spriteTailString =
        // 各プラットフォーム毎に参照スプライトが異なるため、末尾インデックスも異なる
        {
#if UNITY_EDITOR
            "_256",  // UP
            "_257",  // DOWN
            "_258",  // LEFT
            "_259",  // RIGHT
            "_120",  // DECISION
            "_179",  // CANCEL
#elif UNITY_STANDALONE_WIN
            "_10",  // UP
            "_11",  // DOWN
            "_12",  // LEFT
            "_13",  // RIGHT
            "_20",  // DECISION
            "_21",  // CANCEL
#else
#endif
        };

        // 現在の状況において、有効となるキーとそれを押下した際の説明
        List<KeyGuide> _keyGuides;
        // キーガイドを表示するクラス
        KeyGuideUI _keyGuideUI;

        // Start is called before the first frame update
        void Start()
        {
            // ガイドスプライトの読み込みを行い、アサインする
            Sprite guideSprite = Resources.Load<Sprite>(Constants.GUIDE_SPRITE_FOLDER_PASS + Constants.GUIDE_SPRITE_FILE_NAME);
            if( guideSprite != null )
            {
                // キーガイドとスプライトの紐づけを行う
                for (int i = 0; i < (int)KeyIcon.NUM; ++i)
                {

                }
            }
            else
            {
                Debug.Log("Failed load guide sprites!");
            }
        }

        // Update is called once per frame
        void Update()
        {
            // _keyGuideUI.UpdateUI();
        }

        /// <summary>
        /// 遷移先のキーガイドを設定します
        /// </summary>
        /// <param name="guides">表示するキーガイドのリスト</param>
        public void Transit( List<KeyGuide> guides )
        {
            _keyGuides = guides;

            _keyGuideUI.RegistKey(_keyGuides);
        }
    }
}