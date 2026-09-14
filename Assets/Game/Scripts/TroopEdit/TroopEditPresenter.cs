using Frontier.Entities;
using Frontier.UI;
using System.Collections.Generic;
using Zenject;

namespace Frontier.TroopEdit
{
    /// <summary>
    /// 部隊編集画面のViewへの唯一の窓口となるPresenter。
    /// 選択インデックスやキャラクターの生成・破棄といった実データの管理はTroopEditHandlerが担う
    /// (このクラス自身はそれらの実データを保持しない)。このクラスはTroopEditUIへの指示を
    /// そのまま転送するだけの薄い層であり、どの行を表示するか等の判断はTroopGridControllerが行う。
    /// </summary>
    public class TroopEditPresenter
    {
        [Inject] private IUiSystem _uiSystem = null;

        private TroopEditUI _view = null;

        /// <summary>
        /// GeneralUi.TroopEditView(既にシーンに存在するUI)への参照を取得します(一度だけ呼び出してください)。
        /// </summary>
        public void Init()
        {
            _view = _uiSystem.GeneralUi.TroopEditView;
        }

        public void Show() => _view.Show();

        public void Hide() => _view.Hide();

        /// <summary>
        /// 画面全体を覆う背景の表示・非表示を切り替えます。呼び出し元の画面が独自の背景を
        /// 持ち、この画面固有の背景を重ねたくない場合に非表示にします。
        /// </summary>
        public void SetBackgroundVisible( bool isVisible ) => _view.SetBackgroundVisible( isVisible );

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
        /// 指定インデックスのセルのチェックマーク表示を切り替えます
        /// (雇用画面での雇用チェック・解雇画面での解雇チェック共通)。
        /// </summary>
        public void SetChecked( int index, bool isChecked ) => _view.SetChecked( index, isChecked );

        /// <summary>
        /// 選択中キャラクターのパラメータ表示に使うCharacterParameterUIへの参照。
        /// TroopEditHandlerがCharacterParameterPresenterを構築する際に一度だけ取得します。
        /// </summary>
        public CharacterParameterUI CharacterParamUI => _view.CharacterParamUI;

        public void SetCharacterParamName( string text ) => _view.SetCharacterParamName( text );

        /// <summary>
        /// グリッドの表示行を指定行(topRow)から始まるようスクロールします。
        /// </summary>
        public void ScrollToRow( int topRow, bool animate ) => _view.ScrollToRow( topRow, animate );
    }
}
