using DG.Tweening;
using Frontier.Entities;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using static Constants;

namespace Frontier.UI
{
    /// <summary>
    /// 部隊編集画面の見た目のみを担当するView。
    /// 部隊メンバー一覧を、常時TROOP_EDIT_VISIBLE_ROWS行(ScrollRect+Viewportでクリップ)ずつ
    /// 表示するグリッドで表示し、選択行が範囲外に出るとScrollToRow()でスクロールする。開閉や
    /// キャラクターデータの取得はTroopEditPresenterが行い、このクラスは表示指示
    /// (DisplayMembers/Show/Hide)を受けて反映するだけに留める。GeneralUISystem配下に
    /// Option/SaveLoadと同様、事前にヒエラルキーを構築した状態で配置される。
    /// </summary>
    public class TroopEditUI : UiMonoBehaviour
    {
        [Header( "キャラクターセルを並べるグリッドコンテナ(GridLayoutGroup設置済み)" )]
        [SerializeField] private Transform _gridContent;

        [Header( "キャラクター1体分のセルプレハブ(非アクティブなテンプレート)" )]
        [SerializeField] private TroopMemberCellUI _cellPrefab;

        [Header( "選択中のセルに追従するカーソル(GridLayoutGroupの対象外に設定済み)" )]
        [SerializeField] private RectTransform _selectCursor;

        [Header( "選択中キャラクターのパラメータ表示(ウィンドウ下部中央に固定配置済み)" )]
        [SerializeField] private CharacterParameterUI _characterParamUI;

        [Header( "パラメータ表示上部の「Lv.名前」ヘッダーテキスト" )]
        [SerializeField] private TextMeshProUGUI _characterParamNameText;

        [Header( "画面全体を覆う背景。部隊編集画面(FieldScene)では表示したままにし、\n" +
                 "呼び出し元の画面デザインに応じてSetBackgroundVisible()で非表示にできる" )]
        [SerializeField] private Image _background;

        [Header( "キャラクターグリッドの縦スクロールを担うScrollRect(Viewport/Scrollbar設置済み)" )]
        [SerializeField] private ScrollRect _scrollRect;

        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        // 画面中央下部(通常のグリッド画面)/画面左下(雇用・解雇完了確認画面)のパラメータパネルX座標(実測値)
        private const float CharacterParamCenterX = 0f;
        private const float CharacterParamLeftX   = -315f;

        private List<TroopMemberCellUI> _cells = new List<TroopMemberCellUI>();
        private GridLayoutGroup _gridLayoutGroup;
        // GridLayoutGroup.cellSize.y + spacing.yから求めた、行1つ分の高さ(px)
        private float _rowHeight;
        private Tweener _scrollTween;
        private Tweener _panelTween;

        public CharacterParameterUI CharacterParamUI => _characterParamUI;

        public override void Setup()
        {
            base.Setup();

            _characterParamUI?.Setup();

            _gridLayoutGroup = _gridContent.GetComponent<GridLayoutGroup>();
            _rowHeight = _gridLayoutGroup.cellSize.y + _gridLayoutGroup.spacing.y;
        }

        public void Show() => gameObject.SetActive( true );

        public void Hide() => gameObject.SetActive( false );

        /// <summary>
        /// 画面全体を覆う背景の表示・非表示を切り替えます。呼び出し元の画面が独自の背景
        /// (会話ウィンドウ等)を持ち、この画面固有の背景を重ねたくない場合に非表示にします。
        /// </summary>
        public void SetBackgroundVisible( bool isVisible )
        {
            _background?.gameObject.SetActive( isVisible );
        }

        /// <summary>
        /// 渡されたキャラクター一覧をグリッド上に並べ直します。並び順はそのまま
        /// 左上から右方向へ配置され、TROOP_EDIT_GRID_COLUMNS体を超えると次の行へ折り返されます
        /// (グリッドコンテナに設定済みのGridLayoutGroup: FixedColumnCountによる)。
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

            // セルは_selectCursorと同じ親(_gridContent)の子として追加されるため、
            // 描画順(Hierarchy順)でセルより後ろに回りキャラクターの3Dモデルに隠れてしまう。
            // 常にカーソルが手前に描画されるよう、セル生成のたびに最後尾へ移動させる
            _selectCursor?.SetAsLastSibling();

            // GridLayoutGroupによる配置はレイアウトパス(次フレーム以降)まで反映されないため、
            // SetSelectedIndex()で各セルの位置を正しく参照できるよう、ここで強制的に確定させる
            LayoutRebuilder.ForceRebuildLayoutImmediate( ( RectTransform ) _gridContent );
        }

        /// <summary>
        /// 指定インデックスのセルに報酬アニマ量を表示します(解雇画面等の呼び出し元専用)。
        /// nullを渡すと非表示に戻ります。
        /// </summary>
        public void SetRewardAnima( int index, int? amount )
        {
            if ( index < 0 || index >= _cells.Count ) return;

            _cells[index].SetRewardAnima( amount );
        }

        /// <summary>
        /// 指定インデックスのセルに雇用コストを表示します(雇用画面専用)。
        /// nullを渡すと非表示に戻ります。
        /// </summary>
        public void SetCost( int index, int? cost )
        {
            if ( index < 0 || index >= _cells.Count ) return;

            _cells[index].SetCost( cost );
        }

        /// <summary>
        /// 指定インデックスのセルのチェックマーク表示を切り替えます
        /// (雇用画面での雇用チェック・解雇画面での解雇チェック共通)。
        /// </summary>
        public void SetChecked( int index, bool isChecked )
        {
            if ( index < 0 || index >= _cells.Count ) return;

            _cells[index].SetChecked( isChecked );
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
        /// グリッドの表示行を指定行(topRow)から始まるようスクロールします。選択中の行が
        /// 常時表示2行(TROOP_EDIT_VISIBLE_ROWS)に収まるよう、呼び出し元(TroopGridController)が
        /// 選択インデックスから求めたtopRowを渡します。ビューポート外に出る行のセルは
        /// カメラ描画を止め、描画コストを避けます。
        /// ScrollRectはLateUpdateで自らContentの位置を管理しているため、Content.anchoredPositionを
        /// 直接書き換えても次フレームで上書きされてしまう。そのためScrollRect.verticalNormalizedPosition
        /// 経由で操作する。
        /// </summary>
        /// <param name="topRow">表示範囲の先頭行インデックス</param>
        /// <param name="animate">true の場合はアニメーションで滑らかにスクロールします</param>
        public void ScrollToRow( int topRow, bool animate )
        {
            float scrollableHeight = ( ( RectTransform ) _gridContent ).rect.height - _scrollRect.viewport.rect.height;
            float targetNormalized = scrollableHeight > 0.01f
                ? Mathf.Clamp01( 1f - ( topRow * _rowHeight ) / scrollableHeight )
                : 1f;

            _scrollTween?.Kill();
            if ( animate )
            {
                _scrollTween = DOTween.To( () => _scrollRect.verticalNormalizedPosition, x => _scrollRect.verticalNormalizedPosition = x, targetNormalized, TROOP_EDIT_SCROLL_DURATION );
            }
            else
            {
                _scrollRect.verticalNormalizedPosition = targetNormalized;
            }

            for ( int i = 0; i < _cells.Count; ++i )
            {
                int row = i / TROOP_EDIT_GRID_COLUMNS;
                bool isVisible = row >= topRow && row < topRow + TROOP_EDIT_VISIBLE_ROWS;
                _cells[i].SetCameraActive( isVisible );
            }
        }

        /// <summary>
        /// 雇用/解雇完了確認画面へ入る際、チェックマークが付いているセル(checkedIndices)のみを
        /// 残して非表示にし、残ったセルを詰めてアニメーションつきで再配置します。選択カーソルは
        /// 詰め直した後の先頭セルへ移動します。非表示にするだけでキャラクターやセル自体は破棄しない
        /// ため、AnimateRestoreDisplay()で元に戻せます。
        /// </summary>
        public void AnimateFilterToChecked( List<int> checkedIndices )
        {
            var checkedSet = new HashSet<int>( checkedIndices );
            for ( int i = 0; i < _cells.Count; ++i )
            {
                _cells[i].gameObject.SetActive( checkedSet.Contains( i ) );
            }

            // 絞り込み後は表示数が少ないため、行単位の表示制御(ScrollToRow)は行わず、
            // 表示中のセルは全てカメラ描画を有効にする。AnimateReflow()内でGridLayoutGroupを
            // 一時的に無効化する前に済ませておく(ScrollRectの内部処理がレイアウト再計算を
            // 誘発し、アニメーション開始直後の位置を上書きしてしまうのを避けるため)
            _scrollRect.verticalNormalizedPosition = 1f;
            foreach ( var cell in _cells )
            {
                cell.SetCameraActive( cell.gameObject.activeSelf );
            }

            AnimateReflow();
        }

        /// <summary>
        /// AnimateFilterToChecked()で非表示にしたセルを再表示し、remainingCharactersに
        /// 含まれなくなったキャラクター(雇用/解雇が確定して消滅したキャラクター)のセルは破棄した上で、
        /// 残ったセルをアニメーションつきで元の配置に戻します。スクロール位置・カメラ描画の制御は
        /// 呼び出し元(TroopGridController)がUpdateScroll()経由で行います。
        /// </summary>
        public void AnimateRestoreDisplay( List<Character> remainingCharacters )
        {
            for ( int i = 0; i < _cells.Count; ++i )
            {
                _cells[i].gameObject.SetActive( true );
            }

            for ( int i = _cells.Count - 1; i >= 0; --i )
            {
                if ( remainingCharacters.Contains( _cells[i].AssignedCharacter ) ) { continue; }

                _cells[i].Dispose();
                DestroyImmediate( _cells[i].gameObject );
                _cells.RemoveAt( i );
            }

            AnimateReflow();
        }

        /// <summary>
        /// 現在アクティブなセルの位置を、直前の位置からGridLayoutGroup再計算後の新しい位置へ
        /// アニメーションで遷移させます。選択カーソルは最初のアクティブセルへ追従させます。
        /// GridLayoutGroupは有効なままだと毎レイアウトパスで子の位置を計算値へ戻してしまい、
        /// DOTweenで設定した中間位置を上書きしてアニメーションが瞬間移動に見えてしまうため、
        /// 目標位置を計算した後はアニメーション再生中だけ無効化する。
        /// </summary>
        private void AnimateReflow()
        {
            var contentRect = ( RectTransform ) _gridContent;

            var oldPositions = new Vector2[_cells.Count];
            for ( int i = 0; i < _cells.Count; ++i )
            {
                oldPositions[i] = ( ( RectTransform ) _cells[i].transform ).anchoredPosition;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate( contentRect );

            var newPositions = new Vector2[_cells.Count];
            for ( int i = 0; i < _cells.Count; ++i )
            {
                newPositions[i] = ( ( RectTransform ) _cells[i].transform ).anchoredPosition;
            }

            _gridLayoutGroup.enabled = false;
            DOVirtual.DelayedCall( TROOP_EDIT_FOCUS_ANIM_DURATION, () => _gridLayoutGroup.enabled = true );

            Vector2? cursorTargetPos = null;
            for ( int i = 0; i < _cells.Count; ++i )
            {
                var rect = ( RectTransform ) _cells[i].transform;

                if ( cursorTargetPos == null && _cells[i].gameObject.activeSelf )
                {
                    cursorTargetPos = newPositions[i];
                }

                rect.anchoredPosition = oldPositions[i];
                rect.DOKill();
                rect.DOAnchorPos( newPositions[i], TROOP_EDIT_FOCUS_ANIM_DURATION );
            }

            if ( _selectCursor == null ) { return; }

            if ( !cursorTargetPos.HasValue )
            {
                _selectCursor.gameObject.SetActive( false );
                return;
            }

            _selectCursor.gameObject.SetActive( true );
            _selectCursor.DOKill();
            _selectCursor.DOAnchorPos( cursorTargetPos.Value, TROOP_EDIT_FOCUS_ANIM_DURATION );
        }

        /// <summary>
        /// キャラクターパラメータパネルを、画面下部中央(isLeft:false)または画面下部左側(isLeft:true)へ
        /// 移動します(雇用/解雇完了確認画面への遷移用)。Y座標は変えず、X座標のみをアニメーションさせます。
        /// </summary>
        public void SetPanelHorizontalMode( bool isLeft, bool animate, System.Action onComplete = null )
        {
            if ( _characterParamUI == null ) { onComplete?.Invoke(); return; }

            var rect = ( RectTransform ) _characterParamUI.transform;
            float targetX = isLeft ? CharacterParamLeftX : CharacterParamCenterX;

            _panelTween?.Kill();
            if ( animate )
            {
                _panelTween = rect.DOAnchorPosX( targetX, TROOP_EDIT_FOCUS_ANIM_DURATION )
                    .OnComplete( () => onComplete?.Invoke() );
            }
            else
            {
                rect.anchoredPosition = new Vector2( targetX, rect.anchoredPosition.y );
                onComplete?.Invoke();
            }
        }

        /// <summary>
        /// パラメータ表示上部の「Lv.名前」ヘッダーテキストを更新します。
        /// パネルの位置がカーソルから離れた場所になり得るため、どのキャラクターの情報かを
        /// 明示するために表示します。
        /// </summary>
        public void SetCharacterParamName( string text )
        {
            if ( _characterParamNameText == null ) return;

            _characterParamNameText.text = text;
        }

        /// <summary>
        /// グリッド上に生成済みのセルをすべて破棄します。
        /// DisplayMembers()内でこの直後に新しいセルを生成するため、Destroy()(破棄がフレーム末尾まで
        /// 遅延する)ではなくDestroyImmediate()を使い、GridLayoutGroupが新旧セル混在の状態で
        /// レイアウトを確定してしまわないようにする。
        /// </summary>
        public void ClearMembers()
        {
            foreach ( var cell in _cells )
            {
                if ( cell == null ) continue;

                cell.Dispose();
                DestroyImmediate( cell.gameObject );
            }

            _cells.Clear();
        }
    }
}
