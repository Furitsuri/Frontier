using Frontier.StateMachine;
using System.Collections.Generic;
using Zenject;

namespace Frontier.Shop
{
    /// <summary>
    /// ShopHandler の状態を IUiSystem.ShopUi のViewへ転送するPresenterです。
    /// 商品行の並び・購入可否によるグレー表示・カーソル位置(添字のみ)の判断はここで行い、
    /// 実データ(品揃え・在庫・所持アニマ・購入処理)はShopHandlerが持ちます。Viewは指示を反映するだけです。
    /// ShopPhaseHandlerが生成・保持し、AssignPresenterToNodes()で各Stateへ配布します。
    /// 各StateはIUiSystemに直接アクセスせず、必ずこのPresenter経由で表示更新を行ってください。
    /// </summary>
    public class ShopPresenter : PhasePresenterBase
    {
        [Inject] private ShopHandler _shopHandler = null;

        private readonly CommandList                     _commandList = new CommandList();
        private readonly CommandList.CommandIndexedValue _cmdIdxVal   = new CommandList.CommandIndexedValue( 0, 0 );

        /// <summary>
        /// ショップ画面を表示します(ShopHandler.Open()済みであること)。
        /// カーソルは先頭の商品にリセットされます。
        /// </summary>
        public void Open()
        {
            var shopUi = _uiSystem.ShopUi;
            if ( shopUi == null ) { return; }

            shopUi.Init();
            InitCursor();
            Refresh();
        }

        public void Close()
        {
            var shopUi = _uiSystem.ShopUi;
            if ( shopUi != null ) { shopUi.Exit(); }
        }

        /// <summary>
        /// カーソルを方向入力に応じて移動します(上下端はループ)。
        /// </summary>
        /// <returns>カーソル位置が変化したか</returns>
        public bool MoveSelection( Direction dir )
        {
            if ( _shopHandler.Lineup.Count <= 0 ) { return false; }

            bool moved = _commandList.OperateListCursor( dir );
            if ( moved ) { RefreshSelection(); }

            return moved;
        }

        /// <summary>
        /// 現在カーソルが指している商品を取得します。品揃えが空の場合はfalseを返します。
        /// </summary>
        public bool TryGetSelectedItem( out ShopItemRef item )
        {
            var lineup = _shopHandler.Lineup;
            if ( lineup.Count <= 0 || _cmdIdxVal.index < 0 || lineup.Count <= _cmdIdxVal.index )
            {
                item = default;
                return false;
            }

            item = lineup[_cmdIdxVal.index];
            return true;
        }

        /// <summary>
        /// 購入等でShopHandler側の状態(在庫・所持アニマによる購入可否)が変化した際に、表示全体を最新の状態へ更新します。
        /// 所持アニマの数値自体は、画面上部のヘッダー(ShopPhaseHandlerが更新)が表示します。
        /// </summary>
        public void Refresh()
        {
            var shopUi = _uiSystem.ShopUi;
            if ( shopUi == null ) { return; }

            var view   = shopUi.ShopView;
            var lineup = _shopHandler.Lineup;

            view.SetRowCount( lineup.Count );
            for ( int i = 0; i < lineup.Count; ++i )
            {
                var item = lineup[i];
                switch ( item.Category )
                {
                    case ShopItemCategory.Skill:
                        view.SetRowSkill( i, item.AsSkillID, _shopHandler.GetPrice( item ), _shopHandler.Stock[item], !_shopHandler.CanPurchase( item ) );
                        break;
                }
            }

            RefreshSelection();
        }

        private void InitCursor()
        {
            int count = _shopHandler.Lineup.Count;
            if ( count <= 0 ) { return; }

            var indices = new List<int>();
            for ( int i = 0; i < count; ++i ) { indices.Add( i ); }

            _commandList.Init( ref indices, CommandList.CommandDirection.VERTICAL, false, _cmdIdxVal );
        }

        private void RefreshSelection()
        {
            var shopUi = _uiSystem.ShopUi;
            if ( shopUi == null ) { return; }

            shopUi.ShopView.SetSelectedRow( _shopHandler.Lineup.Count <= 0 ? -1 : _cmdIdxVal.index );
        }
    }
}
