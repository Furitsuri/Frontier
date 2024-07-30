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
        private KeyGuideUI _keyGuideUI = null;

        // ガイド上に表示可能なスプライト群
        private Sprite[] sprites;

        // 各スプライトファイル名の末尾の番号
        string[] spriteTailNoString =
        // 各プラットフォーム毎に参照スプライトが異なるため、末尾インデックスも異なる
        {
#if UNITY_EDITOR
            "_alpha_250",  // UP
            "_alpha_251",  // DOWN
            "_alpha_252",  // LEFT
            "_alpha_253",  // RIGHT
            "_alpha_120",  // DECISION
            "_alpha_179",  // CANCEL
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

        // ゲーム内の現在の状況における、操作が有効となるキーとそれを押下した際の説明のリスト
        List<KeyGuide> _keyGuides;

        // Start is called before the first frame update
        void Start()
        {
            LoadSprites();
        }

        // Update is called once per frame
        void Update()
        {
            _keyGuideUI.UpdateUI();
        }

        /// <summary>
        /// スプライトのロード処理を行います
        /// </summary>
        void LoadSprites()
        {
            sprites = new Sprite[(int)KeyIcon.NUM];

            // ガイドスプライトの読み込みを行い、アサインする
            Sprite[] guideSprites = Resources.LoadAll<Sprite>(Constants.GUIDE_SPRITE_FOLDER_PASS + Constants.GUIDE_SPRITE_FILE_NAME);
            for (int i = 0; i < (int)KeyIcon.NUM; ++i)
            {
                string fileName = Constants.GUIDE_SPRITE_FILE_NAME + spriteTailNoString[i];

                foreach (Sprite sprite in guideSprites)
                {
                    if (sprite.name == fileName)
                    {
                        sprites[i] = sprite;
                        break;
                    }
                }

                if ( sprites[i] == null )
                {
                    Debug.LogError("File Not Found : " + fileName);
                }
            }
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