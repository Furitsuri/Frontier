using System;
using System.Collections.Generic;
using Frontier.Combat;
using UnityEngine;
using Zenject;

namespace Frontier.Shop
{
    /// <summary>
    /// ショップ機能の処理を担うクラスです。品揃え・在庫・購入処理等の実データを保持します。
    /// 表示は ShopPresenter に委譲し、このクラス自身は IUiSystem を直接参照しません。
    /// フレーム駆動やState遷移は持たず(ShopRoutineController/ShopPhaseHandlerが担う)、
    /// FieldProgress等の永続データにも直接触れません(実際の反映は呼び出し元が行います)。
    /// State/Presenterから注入されるため、各シーンのDIInstallerでバインドされている前提です
    /// (DIInstaller.cs / FieldDiInstaller.cs)。
    /// </summary>
    public class ShopHandler
    {
        [Inject] private UserDomain _userDomain = null;

        public ShopContext CurrentContext { get; private set; } = null;

        public IReadOnlyList<ShopItemRef> Lineup =>
            CurrentContext != null && _lineupByInstance.TryGetValue( CurrentContext.InstanceId, out var lineup ) ? lineup : _emptyLineup;

        public IReadOnlyDictionary<ShopItemRef, int> Stock =>
            CurrentContext != null && _stockByInstance.TryGetValue( CurrentContext.InstanceId, out var stock ) ? stock : _emptyStock;

        private static readonly List<ShopItemRef>            _emptyLineup = new List<ShopItemRef>();
        private static readonly Dictionary<ShopItemRef, int> _emptyStock  = new Dictionary<ShopItemRef, int>();

        // InstanceId(ステージ+商人インデックス、またはFieldノードID)ごとの品揃え・在庫。
        // 同一インスタンスへの再訪問(戦闘中に同じ商人へ何度も話しかける等)で品揃え・売り切れ状態を維持するため、
        // ShopHandlerのライフタイム(DIコンテナ=シーンの生存期間)内でキャッシュする。
        private readonly Dictionary<int, List<ShopItemRef>>            _lineupByInstance = new Dictionary<int, List<ShopItemRef>>();
        private readonly Dictionary<int, Dictionary<ShopItemRef, int>> _stockByInstance  = new Dictionary<int, Dictionary<ShopItemRef, int>>();

        /// <summary>
        /// ショップを開きます。品揃え・初期在庫は UserDomain.WorldSeed + context.InstanceId から決定論的に抽選されるため、
        /// 同一インスタンスへのセーブからの再開・戦闘リセットでは常に同じ結果になります。
        /// 同一インスタンスへの再訪問時は抽選をやり直さず、キャッシュ済みの品揃え・売り切れ状態をそのまま使います。
        /// </summary>
        public void Open( ShopContext context )
        {
            CurrentContext = context;

            if ( !_lineupByInstance.ContainsKey( context.InstanceId ) )
            {
                RollLineup( context.InstanceId );
            }
        }

        /// <summary>
        /// ショップを閉じます。FieldProgressへの反映等は呼び出し元の責務です
        /// (ShopRoutineController.LateUpdate()がtrueを返した時点で、呼び出し元が終了を検知します)。
        /// </summary>
        public void Close()
        {
            CurrentContext = null;
        }

        public bool CanPurchase( ShopItemRef item )
        {
            return 1 <= GetMaxPurchasableQuantity( item );
        }

        /// <summary>
        /// 在庫と所持アニマの両方の制約から、現在この商品を一度に購入できる最大個数を返します(購入できない場合は0)。
        /// </summary>
        public int GetMaxPurchasableQuantity( ShopItemRef item )
        {
            if ( CurrentContext == null ) { return 0; }

            var stock = _stockByInstance[CurrentContext.InstanceId];
            if ( !stock.TryGetValue( item, out int remaining ) || remaining <= 0 ) { return 0; }

            int price = GetPrice( item );
            if ( price <= 0 ) { return remaining; }

            return Mathf.Min( remaining, _userDomain.Anima / price );
        }

        /// <summary>
        /// 指定した個数を購入します。個数分の価格をまとめて支払い、在庫が足りない場合は何も購入しません。
        /// </summary>
        public PurchaseResult Purchase( ShopItemRef item, int quantity = 1 )
        {
            if ( CurrentContext == null || quantity < 1 ) { return PurchaseResult.OutOfStock; }

            var stock = _stockByInstance[CurrentContext.InstanceId];
            if ( !stock.TryGetValue( item, out int remaining ) || remaining < quantity ) { return PurchaseResult.OutOfStock; }

            int totalPrice = GetPrice( item ) * quantity;
            if ( _userDomain.Anima < totalPrice ) { return PurchaseResult.InsufficientAnima; }

            // UserDomain.AddAnima()には下限チェックが無いため、購入可否は必ず事前に検証してから呼ぶこと
            _userDomain.AddAnima( -totalPrice );
            stock[item] = remaining - quantity;

            switch ( item.Category )
            {
                case ShopItemCategory.Skill:
                    _userDomain.AddSkill( item.AsSkillID, quantity );
                    break;
            }

            return PurchaseResult.Success;
        }

        private void RollLineup( int instanceId )
        {
            int seed = ComputeSeed( _userDomain.WorldSeed, instanceId );
            var random = new System.Random( seed );

            var pool = new List<SkillID>( ( int ) SkillID.NUM );
            for ( int i = 0; i < ( int ) SkillID.NUM; ++i ) { pool.Add( ( SkillID ) i ); }

            var lineup = new List<ShopItemRef>();
            var stock  = new Dictionary<ShopItemRef, int>();

            int lineupSize = Mathf.Min( Constants.SHOP_LINEUP_SIZE, pool.Count );
            for ( int i = 0; i < lineupSize; ++i )
            {
                int pick = random.Next( pool.Count );
                var item = ShopItemRef.FromSkill( pool[pick] );
                pool.RemoveAt( pick );

                lineup.Add( item );
                stock[item] = random.Next( Constants.SHOP_ITEM_STOCK_MIN, Constants.SHOP_ITEM_STOCK_MAX + 1 );
            }

            _lineupByInstance[instanceId] = lineup;
            _stockByInstance[instanceId]  = stock;
        }

        // TODO: スキル以外のカテゴリを追加したら、ここに参照先を追加する
        public int GetPrice( ShopItemRef item )
        {
            switch ( item.Category )
            {
                case ShopItemCategory.Skill:
                    return SkillShopPriceData.Price[( int ) item.AsSkillID];
                default:
                    throw new NotImplementedException( $"未対応のカテゴリです: {item.Category}" );
            }
        }

        /// <summary>
        /// System.HashCode.Combine はプロセスごとにランダム化され再起動のたびに結果が変わってしまうため使用できない。
        /// ここでは単純な多項式ハッシュで、同じ入力からは常に同じ値を返すようにする。
        /// </summary>
        private static int ComputeSeed( int worldSeed, int instanceId )
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + worldSeed;
                hash = hash * 31 + instanceId;
                return hash;
            }
        }
    }
}
