using Frontier.Entities;
using Frontier.UI;
using System.Collections.Generic;
using Zenject;

namespace Frontier.CharacterEdit
{
    /// <summary>
    /// キャラクター編集画面(左メニュー+上部キャラ情報)のViewへの窓口となるPresenter。
    /// メニュー項目(レベルアップ/装備スキル設定)は固定2件でその場限りの添字にすぎないため、
    /// FieldMenuPresenterと同様にPresenter自身がCommandListで管理する
    /// (編集対象のCharacterやその一覧・現在indexといった実データはCharacterEditHandlerが持つ)。
    /// </summary>
    public class CharacterEditPresenter
    {
        [Inject] private IUiSystem _uiSystem = null;

        private CharacterEditUI _view = null;
        private CommandList _commandList = new CommandList();
        private CommandList.CommandIndexedValue _cmdIdxVal;

        /// <summary>
        /// GeneralUi.CharacterEditView(既にシーンに存在するUI)への参照を取得します(一度だけ呼び出してください)。
        /// </summary>
        public void Init()
        {
            _view = _uiSystem.GeneralUi.CharacterEditView;
        }

        /// <summary>
        /// 画面を表示し、メニューカーソルを先頭項目にリセットします。
        /// </summary>
        public void Show( Character character )
        {
            var indices = new List<int>();
            for ( int i = 0; i < ( int ) CHARACTER_EDIT_MENU_OPTION_TAG.NUM; ++i ) { indices.Add( i ); }

            _cmdIdxVal = new CommandList.CommandIndexedValue( 0, 0 );
            _commandList.Init( ref indices, CommandList.CommandDirection.VERTICAL, false, _cmdIdxVal );

            _view.SetSelectedIndex( _cmdIdxVal.index );
            _view.SetCharacterInfo( character );
            _view.Show();
        }

        public void Hide() => _view.Hide();

        /// <summary>
        /// 画面右上の所持金・部隊共有経験値(仮称)表示を更新します(TroopEdit画面と同じ位置)。
        /// </summary>
        public void SetHeaderInfo( int money, int exp ) => _view.SetHeaderInfo( money, exp );

        /// <summary>
        /// 選択中キャラクターのパラメータ表示に使うCharacterParameterUIへの参照。
        /// CharacterEditHandlerがCharacterParameterPresenterを構築する際に一度だけ取得します。
        /// </summary>
        public CharacterParameterUI CharacterParamUI => _view.CharacterParamUI;

        /// <summary>
        /// レベルアップ項目選択時に表示するLevelUpUIへの参照。
        /// CharacterEditHandlerがLevelUpHandlerを構築する際に一度だけ取得します。
        /// </summary>
        public LevelUpUI LevelUpView => _view.LevelUpView;

        /// <summary>
        /// ステータス上昇項目選択時に表示するStatusUpUIへの参照。
        /// CharacterEditHandlerがStatusUpHandlerを構築する際に一度だけ取得します。
        /// </summary>
        public StatusUpUI StatusUpView => _view.StatusUpView;

        /// <summary>
        /// 装備スキル設定項目選択時に表示するSkillEquipUIへの参照。
        /// CharacterEditHandlerがSkillEquipHandlerを構築する際に一度だけ取得します。
        /// </summary>
        public SkillEquipUI SkillEquipView => _view.SkillEquipView;

        /// <summary>
        /// L1/R1でキャラクターが切り替わった際、上部のキャラクター情報表示のみを更新します
        /// (メニューの選択状態はそのまま維持します)。
        /// </summary>
        public void RefreshCharacterInfo( Character character ) => _view.SetCharacterInfo( character );

        /// <summary>
        /// カーソルを移動します。移動できた場合はtrueを返します。
        /// </summary>
        public bool MoveSelection( Direction dir )
        {
            if ( !_commandList.OperateListCursor( dir ) ) return false;

            _view.SetSelectedIndex( _cmdIdxVal.index );

            return true;
        }

        /// <summary>
        /// 現在カーソルが当たっている項目(未確定でも、確定済みでも、その時点の値)。
        /// カーソル移動に応じて右側のプレビュー表示を切り替える際に参照します。
        /// </summary>
        public CHARACTER_EDIT_MENU_OPTION_TAG SelectedOption => ( CHARACTER_EDIT_MENU_OPTION_TAG ) _cmdIdxVal.value;

        /// <summary>
        /// 現在選択中の項目を確定し、他の項目を選択不可の見た目(グレーアウト)にします。
        /// </summary>
        public void LockMenu() => _view.SetLockedIndex( _cmdIdxVal.index );

        /// <summary>
        /// ロック状態を解除し、通常のカーソル選択表示に戻します。
        /// </summary>
        public void UnlockMenu() => _view.SetSelectedIndex( _cmdIdxVal.index );

        /// <summary>
        /// 現在選択中の項目を確定操作します。
        /// </summary>
        public CharacterEditConfirmResult ConfirmSelection()
        {
            var option = ( CHARACTER_EDIT_MENU_OPTION_TAG ) _cmdIdxVal.value;
            switch ( option )
            {
                case CHARACTER_EDIT_MENU_OPTION_TAG.LEVEL_UP:
                    return CharacterEditConfirmResult.RequestLevelUpScreen;

                case CHARACTER_EDIT_MENU_OPTION_TAG.STATUS_UP:
                    return CharacterEditConfirmResult.RequestStatusUpScreen;

                case CHARACTER_EDIT_MENU_OPTION_TAG.SKILL_EQUIP:
                    return CharacterEditConfirmResult.RequestSkillEquipScreen;

                default:
                    return CharacterEditConfirmResult.None;
            }
        }
    }
}
