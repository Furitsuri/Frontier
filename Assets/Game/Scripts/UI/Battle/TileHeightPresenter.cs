using Frontier.Stage;
using Frontier.StateMachine;
using System.Globalization;
using Zenject;

namespace Frontier.UI
{
    /// <summary>
    /// グリッドカーソルが選択しているタイルの高さを表示するPresenterです。
    /// 表示するか否か・どのタイルの高さを表示するかの判断はこのクラスが行い、TileHeightUIは指示された内容の表示のみを担います。
    /// グリッドカーソルの移動に追従させるため、BattleRoutinePresenter経由で毎フレームUpdate()が呼ばれます。
    /// </summary>
    public class TileHeightPresenter : PhasePresenterBase
    {
        [Inject] private StageController _stageCtrl = null;

        private bool _isEnabled          = false;       // タイルの高さ表示を行う場面であるか
        private float _displayedHeight   = float.NaN;   // 表示中の高さ(同値の場合は文字列の再生成を省く)
        private int _targetTileIndex     = -1;          // グリッドカーソルの代わりに高さを表示するタイル(-1の場合はグリッドカーソルの対象タイル)

        /// <summary>
        /// タイルの高さ表示を行うか否かを設定します。
        /// 有効にしている間、グリッドカーソルが表示されていれば(またはSetTargetTileで指定がある場合は)高さを表示します。
        /// </summary>
        public void SetEnabled( bool isEnabled )
        {
            _isEnabled = isEnabled;

            Refresh();
        }

        /// <summary>
        /// グリッドカーソルの対象タイルの代わりに、高さを表示するタイルを指定します。
        /// 移動を伴うスキルの着地先など、グリッドカーソルの対象とは別のタイルの高さを見せたい場合に使用します。
        /// -1を指定すると、グリッドカーソルの対象タイルの高さ表示に戻ります。
        /// </summary>
        public void SetTargetTile( int tileIndex )
        {
            _targetTileIndex = tileIndex;

            Refresh();
        }

        public void Update()
        {
            Refresh();
        }

        /// <summary>
        /// グリッドカーソルが選択しているタイル(SetTargetTileで指定がある場合はそのタイル)の高さ表示を更新します
        /// </summary>
        private void Refresh()
        {
            var tileHeightUi    = _uiSystem.BattleUi.TileHeight;
            bool hasTargetTile  = 0 <= _targetTileIndex;
            bool isVisible      = _isEnabled && ( hasTargetTile || _stageCtrl.IsActiveGridCursor() );

            tileHeightUi.SetVisible( isVisible );
            if( !isVisible ) { return; }

            int tileIndex = hasTargetTile ? _targetTileIndex : _stageCtrl.GetCurrentGridIndex();
            float height  = _stageCtrl.GetTileStaticData( tileIndex ).Height;
            if( height == _displayedHeight ) { return; }

            _displayedHeight = height;
            tileHeightUi.SetHeightText( "HEIGHT : " + height.ToString( "0.##", CultureInfo.InvariantCulture ) );
        }
    }
}
