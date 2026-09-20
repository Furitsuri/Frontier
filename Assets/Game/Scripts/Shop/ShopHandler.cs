using System;
using System.Collections.Generic;
using Zenject;

namespace Frontier.Shop
{
    /// <summary>
    /// ショップ機能の処理を担うクラスです。品揃え・在庫・購入処理等の実データを保持します。
    /// 表示は ShopPresenter に委譲し、このクラス自身は IUiSystem を直接参照しません。
    /// FieldProgress等の永続データにも直接触れず、Close時にClosedイベントを発火するのみで、
    /// 実際の反映(FieldProgress.MarkCleared等)は呼び出し元のStateが行います。
    /// 各シーンのDIInstallerでバインドされている前提です(DIInstaller.cs / FieldDiInstaller.cs)。
    /// </summary>
    public class ShopHandler
    {
        [Inject] private UserDomain _userDomain = null;

        public ShopContext CurrentContext { get; private set; } = null;
        public IReadOnlyList<ShopItemRef> Lineup => _lineup;
        public IReadOnlyDictionary<ShopItemRef, int> Stock => _stock;

        public event Action Closed;

        private readonly List<ShopItemRef>           _lineup = new List<ShopItemRef>();
        private readonly Dictionary<ShopItemRef, int> _stock  = new Dictionary<ShopItemRef, int>();

        /// <summary>
        /// ショップを開きます。品揃えは UserDomain.WorldSeed + context.InstanceId から決定論的に抽選されるため、
        /// 同一インスタンスへの再訪問・セーブからの再開では常に同じ結果になります。
        /// </summary>
        public void Open( ShopContext context )
        {
            CurrentContext = context;

            // TODO: 価格マスターデータ(ShopPriceData等)とUserDomain.WorldSeedが未実装のため、品揃え抽選は未実装
            _lineup.Clear();
            _stock.Clear();
        }

        /// <summary>
        /// ショップを閉じます。FieldProgressへの反映等は呼び出し元の責務です(Closedイベントで通知します)。
        /// </summary>
        public void Close()
        {
            CurrentContext = null;
            Closed?.Invoke();
        }

        public bool CanPurchase( ShopItemRef item )
        {
            if ( !_stock.TryGetValue( item, out int remaining ) || remaining <= 0 ) { return false; }

            return GetPrice( item ) <= _userDomain.Anima;
        }

        public PurchaseResult Purchase( ShopItemRef item )
        {
            if ( !_stock.TryGetValue( item, out int remaining ) || remaining <= 0 ) { return PurchaseResult.OutOfStock; }

            int price = GetPrice( item );
            if ( _userDomain.Anima < price ) { return PurchaseResult.InsufficientAnima; }

            // UserDomain.AddAnima()には下限チェックが無いため、購入可否は必ず事前に検証してから呼ぶこと
            _userDomain.AddAnima( -price );
            _stock[item] = remaining - 1;

            switch ( item.Category )
            {
                case ShopItemCategory.Skill:
                    _userDomain.AddSkill( item.AsSkillID, 1 );
                    break;
            }

            return PurchaseResult.Success;
        }

        // TODO: 価格マスターデータ実装後、カテゴリごとの参照先に差し替える
        private int GetPrice( ShopItemRef item )
        {
            throw new NotImplementedException( "価格マスターデータが未実装です" );
        }
    }
}
