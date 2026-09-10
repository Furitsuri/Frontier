using Frontier.Entities;
using Frontier.UI;
using System.Collections.Generic;
using Zenject;

namespace Frontier.TroopEdit
{
    /// <summary>
    /// 部隊編集画面のViewへの唯一の窓口となるPresenter。
    /// 選択インデックスやキャラクターの生成・破棄といった実データの管理はTroopEditHandlerが担う
    /// (このクラス自身はそれらの実データを保持しない)。一方で、画面のどこに何を表示するかという
    /// 表示方針の判断(パラメータパネルの配置先など)はこのクラスが行い、TroopEditUIは指示された
    /// 内容をそのまま適用するだけの薄い層とする。
    /// </summary>
    public class TroopEditPresenter
    {
        [Inject] private IUiSystem _uiSystem = null;

        private TroopEditUI _view = null;

        // キャラクターパラメータパネルの画面左端からの余白(px)。
        private const float CharacterParamSideMargin = 40f;
        // パネルとグリッド最終行との間に確保する余白(px)。
        private const float CharacterParamRowGap = 8f;
        // パネルと画面上部ヘッダー/入力ガイドバーとの間に確保する最低限の余白(px)。
        private const float CharacterParamEdgeGap = 8f;
        // 画面上部ヘッダー(GeneralHeaderUI)の下端・入力ガイドバーの上端のY座標
        // (px、キャンバス中心基準。実測して決めた値)。パネルを画面上側へ配置する場合、
        // ヘッダーと重ならないようこの値を上限とする。
        private const float CharacterParamHeaderBottomY = 305f;
        private const float CharacterParamGuideTopY    = -305f;

        /// <summary>
        /// GeneralUi.TroopEditView(既にシーンに存在するUI)への参照を取得します(一度だけ呼び出してください)。
        /// </summary>
        public void Init()
        {
            _view = _uiSystem.GeneralUi.TroopEditView;
        }

        public void Show() => _view.Show();

        public void Hide() => _view.Hide();

        public void DisplayMembers( List<Character> characters ) => _view.DisplayMembers( characters );

        public void ClearMembers() => _view.ClearMembers();

        public void SetSelectedIndex( int index ) => _view.SetSelectedIndex( index );

        /// <summary>
        /// 指定インデックスのセルに報酬アニマ量を表示します(解雇画面等の呼び出し元専用)。
        /// </summary>
        public void SetRewardAnima( int index, int? amount ) => _view.SetRewardAnima( index, amount );

        /// <summary>
        /// 指定インデックスのセルに雇用コストを表示します(雇用画面専用)。
        /// </summary>
        public void SetCost( int index, int? cost ) => _view.SetCost( index, cost );

        /// <summary>
        /// 指定インデックスのセルの雇用チェックマーク表示を切り替えます(雇用画面専用)。
        /// </summary>
        public void SetEmployed( int index, bool isEmployed ) => _view.SetEmployed( index, isEmployed );

        /// <summary>
        /// 選択中キャラクターのパラメータ表示に使うCharacterParameterUIへの参照。
        /// TroopEditHandlerがCharacterParameterPresenterを構築する際に一度だけ取得します。
        /// </summary>
        public CharacterParameterUI CharacterParamUI => _view.CharacterParamUI;

        /// <summary>
        /// キャラクターパラメータパネルを画面左側へ再配置します。選択中セルが乗っている行を基準に、
        /// 入力ガイドバーぎりぎりの下側へ配置することを優先し(その行との間に余白を確保できる場合)、
        /// 下側に収まらない場合は画面上部ヘッダーぎりぎりの上側へ配置します。グリッドが複数行ある場合、
        /// 選択中の行以外の行とはパネルが重なり得ますが、カーソル・選択中キャラクターとは
        /// 常に重ならないことを優先します。Viewからは事実(セル位置・パネル高さ)のみを取得し、
        /// どこに置くかの判断はここで行います。
        /// </summary>
        /// <param name="selectedIndex">選択中セルのインデックス(-1の場合は要素なし)</param>
        public void SetCharacterParamCorner( int selectedIndex )
        {
            float panelHeight = _view.GetCharacterParamPanelHeight();
            float? selectedRowBottomY = _view.GetCellBottomY( selectedIndex );

            float guideFloor    = CharacterParamGuideTopY + CharacterParamEdgeGap;
            float headerCeiling = CharacterParamHeaderBottomY - CharacterParamEdgeGap;

            bool fitsBelowSelectedRow = selectedRowBottomY.HasValue && ( guideFloor + panelHeight ) <= ( selectedRowBottomY.Value - CharacterParamRowGap );

            float bottomY = fitsBelowSelectedRow ? guideFloor : headerCeiling - panelHeight;

            _view.SetCharacterParamPosition( CharacterParamSideMargin, bottomY );
        }

        public void SetCharacterParamName( string text ) => _view.SetCharacterParamName( text );
    }
}
