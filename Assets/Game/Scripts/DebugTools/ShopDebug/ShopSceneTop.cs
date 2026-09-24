using System.Linq;
using Frontier.Combat;
using Frontier.Shop;
using UnityEngine;
using Zenject;

#if UNITY_EDITOR

namespace Frontier.DebugTools.ShopDebug
{
    /// <summary>
    /// ShopScene(ショップ機能のデバッグ確認用・単独起動シーン)のエントリポイントです。
    /// ダミーの所持データを用意してShopPresenter/ShopHandlerを直接動かし、結果をConsoleへ出力するだけの役割で、
    /// 本番導線(Field/Battle)からは一切参照されません。
    /// </summary>
    public class ShopSceneTop : MonoBehaviour
    {
        private const int DUMMY_ANIMA       = 1000;
        private const int DUMMY_INSTANCE_ID = 0;

        [Inject] private UserDomain    _userDomain = null;
        [Inject] private IUiSystem     _uiSystem   = null;
        [Inject] private ShopHandler   _handler    = null;
        [Inject] private ShopPresenter _presenter  = null;

        private void Start()
        {
            _userDomain.Debug_SetAnima( DUMMY_ANIMA );

            bool closedRaised = false;
            _handler.Closed += () => closedRaised = true;

            _presenter.Open( new ShopContext( DUMMY_INSTANCE_ID, ShopBackgroundMode.Field ) );
            Check( _handler.CurrentContext != null, "Open後にCurrentContextが設定される" );
            Check( _uiSystem.ShopUi != null && _uiSystem.ShopUi.gameObject.activeSelf, "Open後にShopUiが表示される" );

            int expectedLineupSize = Mathf.Min( Constants.SHOP_LINEUP_SIZE, ( int ) SkillID.NUM );
            Check( _handler.Lineup.Count == expectedLineupSize, $"品揃え数が{expectedLineupSize}件になる(実際:{_handler.Lineup.Count})" );
            Check( _handler.Lineup.Distinct().Count() == _handler.Lineup.Count, "品揃えに重複が無い" );
            Check( _handler.Lineup.All( item => Constants.SHOP_ITEM_STOCK_MIN <= _handler.Stock[item] && _handler.Stock[item] <= Constants.SHOP_ITEM_STOCK_MAX ),
                "各商品の在庫が既定の範囲内に収まっている" );

            // 決定論チェック: 同じWorldSeed+InstanceIdを持つ別のShopHandlerインスタンスでも、
            // 品揃えの抽選結果が完全に一致することを確認する(セーブからの再開・戦闘リセットでの再現性の裏付け)。
            var independentHandler = new ShopHandler();
            typeof( ShopHandler ).GetField( "_userDomain", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance )
                .SetValue( independentHandler, _userDomain );
            independentHandler.Open( new ShopContext( DUMMY_INSTANCE_ID, ShopBackgroundMode.Field ) );
            bool sameLineup = _handler.Lineup.SequenceEqual( independentHandler.Lineup );
            Check( sameLineup, "同じWorldSeed+InstanceIdなら別インスタンスでも同じ品揃えになる(決定論)" );

            // 購入成功
            var purchaseTarget = _handler.Lineup[0];
            int priceBefore  = SkillShopPriceData.Price[( int ) purchaseTarget.AsSkillID];
            int stockBefore  = _handler.Stock[purchaseTarget];
            int animaBefore  = _userDomain.Anima;
            int skillCountBefore = _userDomain.GetSkillCount( purchaseTarget.AsSkillID );

            var result = _handler.Purchase( purchaseTarget );
            Check( result == PurchaseResult.Success, $"在庫のある商品の購入はSuccessを返す(実際:{result})" );
            Check( _handler.Stock[purchaseTarget] == stockBefore - 1, "購入後に在庫が1減る" );
            Check( _userDomain.Anima == animaBefore - priceBefore, "購入後にAnimaが価格分減る" );
            Check( _userDomain.GetSkillCount( purchaseTarget.AsSkillID ) == skillCountBefore + 1, "購入後に所持スキル数が1増える" );

            // 在庫に無い商品(品揃え外)の購入
            var soldOutOfLineup = System.Enum.GetValues( typeof( SkillID ) )
                .Cast<SkillID>()
                .Where( id => id != SkillID.NONE && id != SkillID.NUM )
                .Select( ShopItemRef.FromSkill )
                .FirstOrDefault( item => !_handler.Lineup.Contains( item ) );
            Check( _handler.Purchase( soldOutOfLineup ) == PurchaseResult.OutOfStock, "品揃えに無い商品の購入はOutOfStockを返す" );
            Check( !_handler.CanPurchase( soldOutOfLineup ), "品揃えに無い商品はCanPurchaseがfalseを返す" );

            // アニマ不足
            var insufficientTarget = _handler.Lineup[1];
            _userDomain.Debug_SetAnima( 0 );
            Check( _handler.Purchase( insufficientTarget ) == PurchaseResult.InsufficientAnima, "アニマ不足の購入はInsufficientAnimaを返す" );
            Check( !_handler.CanPurchase( insufficientTarget ), "アニマ不足の商品はCanPurchaseがfalseを返す" );

            _presenter.Close();
            Check( closedRaised, "Close時にClosedイベントが発火する" );
            Check( _handler.CurrentContext == null, "Close後にCurrentContextが破棄される" );
            Check( !_uiSystem.ShopUi.gameObject.activeSelf, "Close後にShopUiが非表示になる" );
        }

        private void Check( bool isOk, string label )
        {
            if ( isOk ) { Debug.Log( $"[ShopSceneTop] OK: {label}" ); }
            else        { Debug.LogError( $"[ShopSceneTop] NG: {label}" ); }
        }
    }
}

#endif // UNITY_EDITOR
