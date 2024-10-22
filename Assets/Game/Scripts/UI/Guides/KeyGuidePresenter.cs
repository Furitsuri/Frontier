using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Frontier
{
    /// <summary>
    /// キーガイド関連の表示制御を行います
    /// </summary>
    public class KeyGuidePresenter : MonoBehaviour
    {
        /// <summary>
        /// フェード中の各モード
        /// </summary>
        public enum FadeMode
        {
            NEUTRAL = 0,
            FADE,
        }

        [Header("ガイドUIのプレハブ")]
        [SerializeField]
        public GameObject GuideUIPrefab;

        [Header("背景リサイズ開始から終了までの時間")]
        [SerializeField]
        public float ResizeTime = 0.33f;

        // キーガイドバーの入出状態
        private FadeMode _fadeMode = FadeMode.NEUTRAL;
        // 前状態のガイドUIリストの数
        private int _prevGuideUIListCount = 0;
        // 現在の背景の幅
        private float _currentBackGroundWidth = 0f;
        // ガイドが遷移する以前の背景の幅
        private float _prevTransitBackGroundWidth = 0f;
        // 更新する際に目標とする背景の幅
        private float _targetBackGroundWidth = 0f;
        // 現在の時間
        private float _fadeTime = 0f;
        // ガイド上に表示可能なスプライト群
        private Sprite[] _sprites;
        // 背景に該当するTransform
        private RectTransform _rectTransform;
        // ガイドの位置調整に用いるレイアウトグループ
        private HorizontalLayoutGroup _layoutGrp;
        // ゲーム内の現在の状況における各キーガイドのリスト
        List<KeyGuideUI.KeyGuide> _keyGuideList;
        // ゲーム内の現在の状況における、操作が有効となるキーとそれを押下した際の説明のUIリスト
        List<KeyGuideUI> _keyGuideUIList;
        // 各スプライトファイル名の末尾の番号
        private static readonly string[] spriteTailNoString =
        // 各プラットフォーム毎に参照スプライトが異なるため、末尾インデックスも異なる
        {
#if UNITY_EDITOR
            "_alpha_308",  // ALL_CURSOR
            "_alpha_250",  // UP
            "_alpha_251",  // DOWN
            "_alpha_252",  // LEFT
            "_alpha_253",  // RIGHT
            "_alpha_201",  // DECISION
            "_alpha_260",  // CANCEL
            "_alpha_259",  // ESCAPE
#elif UNITY_STANDALONE_WIN
            "_***", // ALL_CURSOR
            "_10",  // UP
            "_11",  // DOWN
            "_12",  // LEFT
            "_13",  // RIGHT
            "_20",  // DECISION
            "_21",  // CANCEL
            "_21",  // CANCEL
#else
#endif
        };

        // Start is called before the first frame update
        void Awake()
        {
            _keyGuideUIList         = new List<KeyGuideUI>();
            _rectTransform          = GetComponent<RectTransform>();
            _layoutGrp              = GetComponent<HorizontalLayoutGroup>();
            _prevGuideUIListCount   = 0;

            Debug.Assert(_rectTransform != null, "GetComponent of \"RectTransform\" failed.");
            Debug.Assert(_layoutGrp != null, "GetComponent of \"HorizontalLayoutGroup\" failed.");

            LoadSprites();
        }

        // Update is called once per frame
        void Update()
        {
            if( UpdateFadeUI() )
            {
                _prevTransitBackGroundWidth = _targetBackGroundWidth;
                _fadeMode = FadeMode.NEUTRAL;
                _fadeTime = 0f;
            }
        }

        /// <summary>
        /// ガイドUIのフェード処理を行います
        /// </summary>
        /// <returns>更新が完了したか</returns>
        private bool UpdateFadeUI()
        {
            var completeUpdate = false;

            switch (_fadeMode)
            {
                case FadeMode.FADE:
                    _fadeTime += Time.deltaTime;
                    _currentBackGroundWidth = Mathf.Lerp(_prevTransitBackGroundWidth, _targetBackGroundWidth, _fadeTime / ResizeTime);

                    if( Mathf.Abs(_targetBackGroundWidth - _currentBackGroundWidth) < Mathf.Epsilon )
                    {
                        _currentBackGroundWidth = _targetBackGroundWidth;
                        completeUpdate = true;
                    }

                    _rectTransform.sizeDelta = new Vector2(_currentBackGroundWidth, _rectTransform.sizeDelta.y);
                    
                    break;

                default:
                    // NEUTRAL時は何もしない
                    break;
            }

            return completeUpdate;
        }

        /// <summary>
        /// 背景の幅を更新します
        /// </summary>
        private float CalcurateBackGroundWidth()
        {
            // レイアウトグループの設定を反映
            var taregtWidth = _layoutGrp.padding.left + _layoutGrp.padding.right + _layoutGrp.spacing * (_keyGuideUIList.Count - 1);

            // ガイドUIのそれぞれの幅を加算
            foreach ( var keyGuideUI in _keyGuideUIList )
            {
                var keyGuideUIRectTransform = keyGuideUI.gameObject.GetComponent<RectTransform>();
                Debug.Assert(keyGuideUIRectTransform != null, "GetComponent of \"RectTransform of KeyGuideUI\" failed.");

                taregtWidth += keyGuideUIRectTransform.sizeDelta.x;
            }

            _rectTransform.sizeDelta = new Vector2(taregtWidth, _rectTransform.sizeDelta.y);

            return taregtWidth;
        }

        /// <summary>
        /// ガイドバーのフェード処理を行います
        /// </summary>
        private void TransitFadeMode()
        {
            _fadeMode = FadeMode.NEUTRAL;

            if ( 0 < Mathf.Abs(_keyGuideUIList.Count - _prevGuideUIListCount) )
            {
                _fadeMode = FadeMode.FADE;
                // ガイドの登録に合わせ、ガイドを納める背景の幅を求める
                _targetBackGroundWidth = CalcurateBackGroundWidth();
            }
        }

        /// <summary>
        /// スプライトのロード処理を行います
        /// </summary>
        void LoadSprites()
        {
            _sprites = new Sprite[(int)Constants.KeyIcon.NUM_MAX];

            // ガイドスプライトの読み込みを行い、アサインする
            Sprite[] guideSprites = Resources.LoadAll<Sprite>(Constants.GUIDE_SPRITE_FOLDER_PASS + Constants.GUIDE_SPRITE_FILE_NAME);
            for (int i = 0; i < (int)Constants.KeyIcon.NUM_MAX; ++i)
            {
                string fileName = Constants.GUIDE_SPRITE_FILE_NAME + spriteTailNoString[i];

                foreach (Sprite sprite in guideSprites)
                {
                    if (sprite.name == fileName)
                    {
                        _sprites[i] = sprite;
                        break;
                    }
                }

                if ( _sprites[i] == null )
                {
                    Debug.LogError("File Not Found : " + fileName);
                }
            }
        }

        /// <summary>
        /// 遷移先のキーガイドを設定します
        /// </summary>
        /// <param name="guides">表示するキーガイドのリスト</param>
        public void Transit( List<KeyGuideUI.KeyGuide> keyGuideList )
        {
            // 前回の表示ガイドUI数を保存
            _prevGuideUIListCount = _keyGuideUIList.Count;

            _keyGuideUIList.Clear();
            _keyGuideList = keyGuideList;

            // オブジェクトをインスタンス化して登録
            Transform parentTransform = this.transform;
            foreach (KeyGuideUI.KeyGuide guide in _keyGuideList)
            {
                GameObject keyGuideObject = Instantiate(GuideUIPrefab);
                if( keyGuideObject != null )
                {
                    // このインスタンスの子インスタンスとして生成
                    keyGuideObject.transform.SetParent(parentTransform);

                    KeyGuideUI keyGuideUI = keyGuideObject.GetComponent<KeyGuideUI>();
                    if (keyGuideUI == null) continue;

                    keyGuideUI.Regist( _sprites, guide );

                    _keyGuideUIList.Add(keyGuideUI);
                }
            }

            // フェード状態の遷移
            TransitFadeMode();
        }
    }
}