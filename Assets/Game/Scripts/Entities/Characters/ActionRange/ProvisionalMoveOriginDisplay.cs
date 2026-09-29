using Zenject;

namespace Frontier.Entities
{
    /// <summary>
    /// グリッドカーソルを合わせているキャラクターが暫定的に移動している(移動が確定していない)場合に、
    /// その移動前の位置を示す目印(ProvisionalMoveOriginMarker)を表示するための共有シングルトンです。
    /// HoveredCharacterRangeDisplayと同様、カーソル移動の度にRefreshを呼び出してください。
    /// 目印は複数キャラクター分を同時には表示せず、カーソルを合わせたキャラクターの分のみを表示します。
    /// 一度表示した目印は、対象キャラクターの移動が確定・巻き戻しされた時点で目印自身が非表示にします。
    /// </summary>
    public class ProvisionalMoveOriginDisplay
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        private ProvisionalMoveOriginMarker _marker = null;

        [Inject]
        public ProvisionalMoveOriginDisplay() { }

        /// <summary>
        /// カーソルが指しているキャラクターに応じて表示を更新します。
        /// 対象なし、または暫定的に移動していないキャラクターの場合は表示を消去します。
        /// </summary>
        public void Refresh( Character hoveredCharacter )
        {
            Player player = hoveredCharacter as Player;
            if( null == player || !player.IsProvisionallyMoved() )
            {
                Clear();
                return;
            }

            LazyInject.GetOrCreate( ref _marker, () =>
            {
                var marker = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<ProvisionalMoveOriginMarker>( true, false, "ProvisionalMoveOriginMarker" );
                marker.Setup();
                return marker;
            } );

            if( _marker.isActiveAndEnabled && _marker.Target == player ) { return; }

            _marker.Show( player );
        }

        /// <summary>
        /// 現在表示中の目印を非表示にします
        /// </summary>
        public void Clear()
        {
            if( null != _marker ) { _marker.Hide(); }
        }
    }
}
