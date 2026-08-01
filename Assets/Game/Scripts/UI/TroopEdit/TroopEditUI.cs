using Frontier.Entities;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Frontier.UI
{
    /// <summary>
    /// 部隊編集画面の見た目のみを担当するView。
    /// 部隊メンバー一覧を1行4体のグリッドで表示する。開閉やキャラクターデータの取得は
    /// TroopEditPresenterが行い、このクラスは表示指示(DisplayMembers/Show/Hide)を受けて
    /// 反映するだけに留める。GeneralUISystem配下にOption/SaveLoadと同様、事前にヒエラルキー
    /// を構築した状態で配置される。
    /// </summary>
    public class TroopEditUI : UiMonoBehaviour
    {
        private const string TitleLocalizationKey = "UI_CMD_TROOPS";

        [Header( "タイトルテキスト" )]
        [SerializeField] private TextMeshProUGUI _titleText;

        [Header( "キャラクターセルを並べるグリッドコンテナ(GridLayoutGroup設置済み)" )]
        [SerializeField] private Transform _gridContent;

        [Header( "キャラクター1体分のセルプレハブ(非アクティブなテンプレート)" )]
        [SerializeField] private TroopMemberCellUI _cellPrefab;

        [Header( "所持金テキスト" )]
        [SerializeField] private TextMeshProUGUI _moneyText;

        [Header( "部隊人数(現在数/上限数)テキスト" )]
        [SerializeField] private TextMeshProUGUI _memberCountText;

        [Header( "選択中のセルに追従するカーソル(GridLayoutGroupの対象外に設定済み)" )]
        [SerializeField] private RectTransform _selectCursor;

        [Header( "選択中キャラクターのパラメータ表示(カーソルの対角の角へ再配置される)" )]
        [SerializeField] private CharacterParameterUI _characterParamUI;

        [Header( "パラメータ表示上部の「Lv.名前」ヘッダーテキスト" )]
        [SerializeField] private TextMeshProUGUI _characterParamNameText;

        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private ILocalizationService _localization = null;

        // キャラクターパラメータパネルの画面端からの余白(px)。
        // 上下はタイトル/所持金・人数表示、入力ガイドバーと重ならないよう実測して決めた値。
        private const float CharacterParamSideMargin   = 40f;
        private const float CharacterParamTopMargin    = 130f;
        private const float CharacterParamBottomMargin = 120f;

        private List<TroopMemberCellUI> _cells = new List<TroopMemberCellUI>();

        public CharacterParameterUI CharacterParamUI => _characterParamUI;

        public override void Setup()
        {
            base.Setup();

            RefreshTitleText();
            _characterParamUI?.Setup();

            if ( _localization != null ) { _localization.OnLanguageChanged += RefreshTitleText; }
        }

        private void OnDestroy()
        {
            if ( _localization != null ) { _localization.OnLanguageChanged -= RefreshTitleText; }
        }

        public void Show() => gameObject.SetActive( true );

        public void Hide() => gameObject.SetActive( false );

        private void RefreshTitleText()
        {
            if ( _titleText == null ) return;

            _titleText.text = _localization != null ? _localization.Get( TitleLocalizationKey ) : TitleLocalizationKey;
        }

        /// <summary>
        /// 画面右上の所持金・部隊人数(現在数/上限数)表示を更新します。
        /// </summary>
        public void SetHeaderInfo( int money, int currentMemberNum, int maxMemberNum )
        {
            _moneyText.text = money.ToString();
            _memberCountText.text = $"{currentMemberNum}/{maxMemberNum}";
        }

        /// <summary>
        /// 渡されたキャラクター一覧をグリッド上に並べ直します。並び順はそのまま
        /// 左上から右方向へ配置され、1行4体を超えると次の行へ折り返されます
        /// (グリッドコンテナに設定済みのGridLayoutGroup: FixedColumnCount=4による)。
        /// </summary>
        public void DisplayMembers( List<Character> characters )
        {
            ClearMembers();

            for ( int i = 0; i < characters.Count; ++i )
            {
                var cell = _hierarchyBld.CreateComponentNestedParentWithDiContainer<TroopMemberCellUI>( _cellPrefab.gameObject, _gridContent.gameObject, true, false, "TroopMemberCell_" + i );
                cell.Setup();
                cell.gameObject.SetActive( true );
                cell.AssignCharacter( characters[i] );

                _cells.Add( cell );
            }

            // GridLayoutGroupによる配置はレイアウトパス(次フレーム以降)まで反映されないため、
            // SetSelectedIndex()で各セルの位置を正しく参照できるよう、ここで強制的に確定させる
            LayoutRebuilder.ForceRebuildLayoutImmediate( ( RectTransform ) _gridContent );
        }

        /// <summary>
        /// 選択カーソルを指定インデックスのセルへ移動します。
        /// index が範囲外(セルが1つも無い場合は-1を渡す)の場合はカーソルを非表示にします。
        /// </summary>
        public void SetSelectedIndex( int index )
        {
            if ( _selectCursor == null ) return;

            if ( index < 0 || index >= _cells.Count )
            {
                _selectCursor.gameObject.SetActive( false );
                return;
            }

            var targetRect = ( RectTransform ) _cells[index].transform;

            _selectCursor.gameObject.SetActive( true );
            _selectCursor.anchorMin = targetRect.anchorMin;
            _selectCursor.anchorMax = targetRect.anchorMax;
            _selectCursor.pivot = targetRect.pivot;
            _selectCursor.anchoredPosition = targetRect.anchoredPosition;
        }

        /// <summary>
        /// キャラクターパラメータパネルを、カーソルと対角となる画面の角へ再配置します。
        /// isRight/isBottomにはカーソルが位置する側と逆側(パネルを表示したい側)を渡してください。
        /// 各角のマージンは、タイトル/所持金・人数表示(上)、入力ガイドバー(下)と重ならないよう
        /// あらかじめ実測して決めています。
        /// </summary>
        public void SetCharacterParamCorner( bool isRight, bool isBottom )
        {
            if ( _characterParamUI == null ) return;

            var rect = ( RectTransform ) _characterParamUI.transform;

            float anchor = isRight ? 1f : 0f;
            float anchorY = isBottom ? 0f : 1f;
            rect.anchorMin = new Vector2( anchor, anchorY );
            rect.anchorMax = new Vector2( anchor, anchorY );
            rect.pivot = new Vector2( anchor, anchorY );

            float posX = isRight ? -CharacterParamSideMargin : CharacterParamSideMargin;
            float posY = isBottom ? CharacterParamBottomMargin : -CharacterParamTopMargin;
            rect.anchoredPosition = new Vector2( posX, posY );
        }

        /// <summary>
        /// パラメータ表示上部の「Lv.名前」ヘッダーテキストを更新します。
        /// パネルの位置がカーソルから離れた対角の角になるため、どのキャラクターの情報かを
        /// 明示するために表示します。
        /// </summary>
        public void SetCharacterParamName( string text )
        {
            if ( _characterParamNameText == null ) return;

            _characterParamNameText.text = text;
        }

        /// <summary>
        /// グリッド上に生成済みのセルをすべて破棄します。
        /// </summary>
        public void ClearMembers()
        {
            foreach ( var cell in _cells )
            {
                if ( cell == null ) continue;

                cell.Dispose();
                Destroy( cell.gameObject );
            }

            _cells.Clear();
        }
    }
}
