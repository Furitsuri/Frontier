using Frontier.Entities;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

#if UNITY_EDITOR

namespace Frontier.DebugTools.Battle
{
    /// <summary>
    /// キャラクターの頭上に、デバッグ用の文字列ラベルを表示します。
    /// ラベルの見た目はテンプレートのプレハブ側で設定し、ここでは生成・配置・文字列の設定のみを行います。
    /// デバッグメニューを閉じている間も表示し続けるため、DebugUI(メニューの開閉に合わせて表示が切り替わる)とは別のキャンバスに配置します
    /// </summary>
    public class DebugCharacterLabelUI : MonoBehaviour
    {
        [Header( "ラベルのテンプレート(子にTextMeshProUGUIを持つプレハブ)" )]
        [SerializeField] private GameObject _labelTemplate;

        [Header( "キャラクターの座標から、ラベルを表示する位置までのワールド空間での高さ" )]
        [SerializeField] private float _worldOffsetY = Constants.HP_GAUGE_WORLD_OFFSET_Y;

        [Header( "上記の位置から、更にずらす画面上の量(HPゲージと重ならないようにするため)" )]
        [SerializeField] private Vector2 _screenOffset = new Vector2( 0f, 28f );

        private class Label
        {
            public GameObject Root;
            public RectTransform Rect;
            public TextMeshProUGUI Text;
        }

        private RectTransform _rectTransform = null;
        private Canvas _canvas = null;
        private Dictionary<Character, Label> _labels = new Dictionary<Character, Label>();
        private HashSet<Character> _shownThisUpdate = new HashSet<Character>();
        private List<Character> _removeBuffer = new List<Character>();

        /// <summary>
        /// 表示内容の更新を開始します。BeginUpdate～EndUpdateの間にShowされなかったキャラクターのラベルは非表示になります
        /// </summary>
        public void BeginUpdate()
        {
            _shownThisUpdate.Clear();
        }

        /// <summary>
        /// 指定キャラクターの頭上にラベルを表示します
        /// </summary>
        public void Show( Character chara, string text )
        {
            if( chara == null ) { return; }

            if( !_labels.TryGetValue( chara, out var label ) )
            {
                label = CreateLabel( chara );
                _labels.Add( chara, label );
            }

            if( label.Text.text != text ) { label.Text.text = text; }
            if( !label.Root.activeSelf ) { label.Root.SetActive( true ); }

            _shownThisUpdate.Add( chara );
        }

        /// <summary>
        /// 表示内容の更新を終了します
        /// </summary>
        public void EndUpdate()
        {
            _removeBuffer.Clear();

            foreach( var pair in _labels )
            {
                if( _shownThisUpdate.Contains( pair.Key ) ) { continue; }

                // 撃破等で破棄されたキャラクターのラベルは破棄し、それ以外は非表示にする
                if( pair.Key == null )
                {
                    Destroy( pair.Value.Root );
                    _removeBuffer.Add( pair.Key );
                }
                else if( pair.Value.Root.activeSelf )
                {
                    pair.Value.Root.SetActive( false );
                }
            }

            foreach( var chara in _removeBuffer ) { _labels.Remove( chara ); }
        }

        /// <summary>
        /// 全てのラベルを非表示にします
        /// </summary>
        public void HideAll()
        {
            BeginUpdate();
            EndUpdate();
        }

        private Label CreateLabel( Character chara )
        {
            var root = Instantiate( _labelTemplate, transform );
            root.name = $"DebugLabel_{chara.name}";

            return new Label
            {
                Root    = root,
                Rect    = root.GetComponent<RectTransform>(),
                Text    = root.GetComponentInChildren<TextMeshProUGUI>( true ),
            };
        }

        void Awake()
        {
            _rectTransform  = GetComponent<RectTransform>();
            _canvas         = GetComponent<Canvas>();
        }

        void LateUpdate()
        {
            var worldCamera = Camera.main;
            if( worldCamera == null ) { return; }

            foreach( var pair in _labels )
            {
                if( pair.Key == null || !pair.Value.Root.activeSelf ) { continue; }

                var worldPos  = pair.Key.transform.position + Vector3.up * _worldOffsetY;
                var screenPos = RectTransformUtility.WorldToScreenPoint( worldCamera, worldPos ) + _screenOffset;
                RectTransformUtility.ScreenPointToLocalPointInRectangle( _rectTransform, screenPos, _canvas.worldCamera, out var localPos );
                pair.Value.Rect.localPosition = localPos;
            }
        }
    }
}

#endif // UNITY_EDITOR
