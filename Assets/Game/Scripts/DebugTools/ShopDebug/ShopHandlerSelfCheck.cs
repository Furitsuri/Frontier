using System.Linq;
using System.Reflection;
using Frontier.Combat;
using Frontier.Shop;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

namespace Frontier.DebugTools.ShopDebug
{
    /// <summary>
    /// ShopHandler(品揃え抽選・在庫・購入処理といったデータ・ロジック)の自己チェックです。
    /// Editorのメニュー(Frontier/Debug/Shop/Run ShopHandler Self Check)から、Playもシーンも不要で実行できます。
    /// 専用のダミーUserDomain/ShopHandlerを生成して行うため、プレイ中のセッションデータには一切影響しません。
    /// 結果はConsoleへ「OK/NG」として出力します。表示・入力(State)のチェックは含みません。
    /// </summary>
    public static class ShopHandlerSelfCheck
    {
        private const int DUMMY_ANIMA = 1000;
        private const int INSTANCE_ID = 0;

        private static int _ngCount = 0;

        [MenuItem( "Frontier/Debug/Shop/Run ShopHandler Self Check" )]
        public static void Run()
        {
            _ngCount = 0;

            var userDomain = new UserDomain();
            userDomain.Debug_SetAnima( DUMMY_ANIMA );

            var handler = CreateHandler( userDomain );

            handler.Open( new ShopContext( INSTANCE_ID, ShopBackgroundMode.Field ) );
            Check( handler.CurrentContext != null, "Open後にCurrentContextが設定される" );

            int expectedLineupSize = Mathf.Min( Constants.SHOP_LINEUP_SIZE, ( int ) SkillID.NUM );
            Check( handler.Lineup.Count == expectedLineupSize, $"品揃え数が{expectedLineupSize}件になる(実際:{handler.Lineup.Count})" );
            Check( handler.Lineup.Distinct().Count() == handler.Lineup.Count, "品揃えに重複が無い" );
            Check( handler.Lineup.All( item => Constants.SHOP_ITEM_STOCK_MIN <= handler.Stock[item] && handler.Stock[item] <= Constants.SHOP_ITEM_STOCK_MAX ),
                "各商品の在庫が既定の範囲内に収まっている" );

            // 決定論チェック: 同じWorldSeed+InstanceIdを持つ別のShopHandlerインスタンスでも、
            // 品揃えの抽選結果が完全に一致することを確認する(セーブからの再開・戦闘リセットでの再現性の裏付け)。
            var independentHandler = CreateHandler( userDomain );
            independentHandler.Open( new ShopContext( INSTANCE_ID, ShopBackgroundMode.Field ) );
            Check( handler.Lineup.SequenceEqual( independentHandler.Lineup ), "同じWorldSeed+InstanceIdなら別インスタンスでも同じ品揃えになる(決定論)" );

            // 購入成功
            var purchaseTarget   = handler.Lineup[0];
            int price            = handler.GetPrice( purchaseTarget );
            int stockBefore      = handler.Stock[purchaseTarget];
            int animaBefore      = userDomain.Anima;
            int skillCountBefore = userDomain.GetSkillCount( purchaseTarget.AsSkillID );

            var result = handler.Purchase( purchaseTarget );
            Check( result == PurchaseResult.Success, $"在庫のある商品の購入はSuccessを返す(実際:{result})" );
            Check( handler.Stock[purchaseTarget] == stockBefore - 1, "購入後に在庫が1減る" );
            Check( userDomain.Anima == animaBefore - price, "購入後にAnimaが価格分減る" );
            Check( userDomain.GetSkillCount( purchaseTarget.AsSkillID ) == skillCountBefore + 1, "購入後に所持スキル数が1増える" );

            // 品揃えに無い商品の購入
            var notInLineup = System.Enum.GetValues( typeof( SkillID ) )
                .Cast<SkillID>()
                .Where( id => id != SkillID.NONE && id != SkillID.NUM )
                .Select( ShopItemRef.FromSkill )
                .FirstOrDefault( item => !handler.Lineup.Contains( item ) );
            Check( handler.Purchase( notInLineup ) == PurchaseResult.OutOfStock, "品揃えに無い商品の購入はOutOfStockを返す" );
            Check( !handler.CanPurchase( notInLineup ), "品揃えに無い商品はCanPurchaseがfalseを返す" );

            // アニマ不足
            var insufficientTarget = handler.Lineup[1];
            userDomain.Debug_SetAnima( 0 );
            Check( handler.Purchase( insufficientTarget ) == PurchaseResult.InsufficientAnima, "アニマ不足の購入はInsufficientAnimaを返す" );
            Check( !handler.CanPurchase( insufficientTarget ), "アニマ不足の商品はCanPurchaseがfalseを返す" );

            handler.Close();
            Check( handler.CurrentContext == null, "Close後にCurrentContextが破棄される" );

            if ( _ngCount == 0 ) { Debug.Log( "[ShopHandlerSelfCheck] 全項目OKでした" ); }
            else                 { Debug.LogError( $"[ShopHandlerSelfCheck] NGが{_ngCount}件ありました" ); }
        }

        /// <summary>
        /// DIコンテナを介さず、UserDomainだけを注入したShopHandlerを生成します。
        /// </summary>
        private static ShopHandler CreateHandler( UserDomain userDomain )
        {
            var handler = new ShopHandler();
            typeof( ShopHandler ).GetField( "_userDomain", BindingFlags.NonPublic | BindingFlags.Instance )
                .SetValue( handler, userDomain );

            return handler;
        }

        private static void Check( bool isOk, string label )
        {
            if ( isOk ) { Debug.Log( $"[ShopHandlerSelfCheck] OK: {label}" ); return; }

            ++_ngCount;
            Debug.LogError( $"[ShopHandlerSelfCheck] NG: {label}" );
        }
    }
}

#endif // UNITY_EDITOR
