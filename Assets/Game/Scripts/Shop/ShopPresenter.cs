using Frontier.StateMachine;
using System.Collections.Generic;
using UnityEngine;
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

        // 個数選択中か、その個数と、選択できる上限(在庫とアニマから決まる、個数選択の開始時点の値)
        private bool _isSelectingQuantity = false;
        private int  _quantity            = 1;
        private int  _maxQuantity         = 1;

        /// <summary>
        /// 個数選択で現在選ばれている個数
        /// </summary>
        public int SelectedQuantity => _quantity;

        /// <summary>
        /// 個数選択中の、購入予定(選択中の商品×選択中の個数)による所持アニマの増減差分です(支払いのため0以下)。
        /// 個数選択中でなければ0を返します。ヘッダーのアニマ数値の下へ表示するため、ShopPhaseHandlerが毎フレーム参照します。
        /// </summary>
        public int PendingAnimaDiff
        {
            get
            {
                if ( !_isSelectingQuantity || !TryGetSelectedItem( out var item ) ) { return 0; }

                return -_shopHandler.GetPrice( item ) * _quantity;
            }
        }

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
        /// 現在カーソルが指している商品について、個数選択を開始します(個数は1から。上限は一度に購入できる最大個数)。
        /// 個数選択パネルは、カーソルが指している商品行の右隣に表示します。
        /// </summary>
        /// <returns>個数選択を開始できたか(選択中の商品が無い場合はfalse)</returns>
        public bool BeginQuantitySelection()
        {
            var shopUi = _uiSystem.ShopUi;
            if ( shopUi == null || !TryGetSelectedItem( out var item ) ) { return false; }

            _isSelectingQuantity = true;
            _maxQuantity         = Mathf.Max( 1, _shopHandler.GetMaxPurchasableQuantity( item ) );
            _quantity            = 1;

            shopUi.ShopView.ShowQuantityPanel( _cmdIdxVal.index, _quantity );

            return true;
        }

        /// <summary>
        /// 個数を方向入力に応じて増減します(上で増加、下で減少。上限・下限を超える場合は反対側へループ)。
        /// </summary>
        /// <returns>個数が変化したか</returns>
        public bool MoveQuantity( Direction dir )
        {
            int delta = dir == Direction.FORWARD ? 1 : dir == Direction.BACK ? -1 : 0;
            if ( delta == 0 || _maxQuantity <= 1 ) { return false; }

            var shopUi = _uiSystem.ShopUi;
            if ( shopUi == null ) { return false; }

            _quantity = ( _quantity - 1 + delta + _maxQuantity ) % _maxQuantity + 1;
            shopUi.ShopView.SetQuantity( _quantity );

            return true;
        }

        public void EndQuantitySelection()
        {
            _isSelectingQuantity = false;

            var shopUi = _uiSystem.ShopUi;
            if ( shopUi != null ) { shopUi.ShopView.HideQuantityPanel(); }
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
