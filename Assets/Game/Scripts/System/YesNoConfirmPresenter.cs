using System.Collections.Generic;
using Zenject;

namespace Frontier
{
    /// <summary>
    /// 二者択一(YES/NO)の確認ダイアログのPresenter。
    /// ConfirmPhaseStateBase(二者択一の確認画面)と同じ考え方(CommandListのHORIZONTAL方向、
    /// 誤操作を避けるためデフォルト選択はNO)を踏襲しつつ、State木構造を持たないシーン向けに
    /// 軽量な実装としている。Field/Title等、複数のシーン・複数の確認用途(ゲーム終了、セーブデータ削除等)
    /// から共通して利用される。
    /// </summary>
    public class YesNoConfirmPresenter
    {
        private enum ConfirmTag
        {
            YES = 0,
            NO,

            NUM,
        }

        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        private YesNoConfirmUI _confirmUI = null;
        private CommandList _commandList = new CommandList();
        private CommandList.CommandIndexedValue _cmdIdxVal;

        /// <summary>
        /// UIを生成します(一度だけ呼び出してください)。
        /// </summary>
        public void Init()
        {
            _confirmUI = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<YesNoConfirmUI>( true, false, nameof( YesNoConfirmUI ) );
            _confirmUI.Setup();
        }

        /// <summary>
        /// ダイアログを表示し、カーソルをNO(誤操作防止のための既定値)にリセットします。
        /// </summary>
        /// <param name="messageKey">確認メッセージのローカライズキー(例: LocKey.UI_CONFIRM_EXIT_GAME_MESSAGE)</param>
        public void Show( LocKey messageKey )
        {
            _confirmUI.SetMessage( messageKey );

            var indices = new List<int>();
            for ( int i = 0; i < ( int ) ConfirmTag.NUM; ++i ) { indices.Add( i ); }

            // ConfirmPhaseStateBaseと同様、既定選択はNOにする(誤操作防止のため)。
            // CommandList.Init()はlastNodeStart(第3引数)でカーソルの初期位置(先頭/末尾)を決定し、
            // 渡したCommandIndexedValueの初期値はその位置に応じて上書きされるため、
            // ここではlastNodeStart=trueを指定してリスト末尾(NO)から開始させる。
            _cmdIdxVal = new CommandList.CommandIndexedValue( ( int ) ConfirmTag.NO, ( int ) ConfirmTag.NO );
            _commandList.Init( ref indices, CommandList.CommandDirection.HORIZONTAL, true, _cmdIdxVal );

            _confirmUI.SetSelectedIndex( _cmdIdxVal.index );
            _confirmUI.Show();
        }

        /// <summary>
        /// ダイアログを閉じます。
        /// </summary>
        public void Hide()
        {
            _confirmUI.Hide();
        }

        /// <summary>
        /// カーソルを移動します。移動できた場合はtrueを返します。
        /// </summary>
        public bool MoveSelection( Direction dir )
        {
            if ( !_commandList.OperateListCursor( dir ) ) return false;

            _confirmUI.SetSelectedIndex( _cmdIdxVal.index );

            return true;
        }

        /// <summary>現在YESが選択されているかどうかを取得します。</summary>
        public bool IsYesSelected()
        {
            return ( ConfirmTag ) _cmdIdxVal.value == ConfirmTag.YES;
        }
    }
}
