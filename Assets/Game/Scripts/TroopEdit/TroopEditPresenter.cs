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
        // パネルとタイトル/入力ガイドバーとの間に確保する最低限の余白(px)。
        private const float CharacterParamEdgeGap = 8f;
        // タイトルテキストの下端・入力ガイドバーの上端のY座標(px、キャンバス中心基準)。実測して決めた値。
        private const float CharacterParamTitleBottomY = 250f;
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

        public void SetHeaderInfo( int money, int exp, int currentMemberNum, int maxMemberNum ) => _view.SetHeaderInfo( money, exp, currentMemberNum, maxMemberNum );

        /// <summary>
        /// 選択中キャラクターのパラメータ表示に使うCharacterParameterUIへの参照。
        /// TroopEditHandlerがCharacterParameterPresenterを構築する際に一度だけ取得します。
        /// </summary>
        public CharacterParameterUI CharacterParamUI => _view.CharacterParamUI;

        /// <summary>
        /// キャラクターパラメータパネルを画面左側へ再配置します。常に入力ガイドバーぎりぎりの
        /// 下側へ配置することを優先し(グリッド最終行との間に余白を確保できる場合)、
        /// タイトルとグリッド1行目の間はほぼ余白が無いため、下側に収まらない場合のみ
        /// タイトルぎりぎりの上側へ配置します。Viewからは事実(セル位置・パネル高さ)のみを取得し、
        /// どこに置くかの判断はここで行います。
        /// </summary>
        /// <param name="lastIndex">グリッド最終セルのインデックス(-1の場合は要素なし)</param>
        public void SetCharacterParamCorner( int lastIndex )
        {
            float panelHeight = _view.GetCharacterParamPanelHeight();
            float? lastRowBottomY = _view.GetCellBottomY( lastIndex );

            float guideFloor   = CharacterParamGuideTopY + CharacterParamEdgeGap;
            float titleCeiling = CharacterParamTitleBottomY - CharacterParamEdgeGap;

            bool fitsBelowGrid = lastRowBottomY.HasValue && ( guideFloor + panelHeight ) <= ( lastRowBottomY.Value - CharacterParamRowGap );

            float bottomY = fitsBelowGrid ? guideFloor : titleCeiling - panelHeight;

            _view.SetCharacterParamPosition( CharacterParamSideMargin, bottomY );
        }

        public void SetCharacterParamName( string text ) => _view.SetCharacterParamName( text );
    }
}
