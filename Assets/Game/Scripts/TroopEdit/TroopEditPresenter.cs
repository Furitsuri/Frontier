using Frontier.Entities;
using Frontier.UI;
using System.Collections.Generic;
using Zenject;

namespace Frontier.TroopEdit
{
    /// <summary>
    /// 部隊編集画面のViewへの唯一の窓口となるPresenter。
    /// 選択インデックスやキャラクターの生成・破棄といった実データの管理はTroopEditHandlerが担い、
    /// このクラスはTroopEditUIへの単純な転送のみを行う(View自身の状態を持たない薄い層)。
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

        public void DisplayMembers( List<Character> characters ) => _view.DisplayMembers( characters );

        public void ClearMembers() => _view.ClearMembers();

        public void SetSelectedIndex( int index ) => _view.SetSelectedIndex( index );

        public void SetHeaderInfo( int money, int currentMemberNum, int maxMemberNum ) => _view.SetHeaderInfo( money, currentMemberNum, maxMemberNum );
    }
}
