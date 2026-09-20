using Frontier.Combat;
using Frontier.Shop;
using UnityEngine;
using Zenject;

#if UNITY_EDITOR

namespace Frontier.DebugTools.ShopDebug
{
    /// <summary>
    /// ShopScene(ショップ機能のデバッグ確認用・単独起動シーン)のエントリポイントです。
    /// ダミーの所持データを用意してShopPresenterを直接起動し、結果をConsoleへ出力するだけの役割で、
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

            var notInStock = ShopItemRef.FromSkill( default( SkillID ) );
            Check( _handler.Purchase( notInStock ) == PurchaseResult.OutOfStock, "在庫に無い商品の購入はOutOfStockを返す" );
            Check( !_handler.CanPurchase( notInStock ), "在庫に無い商品はCanPurchaseがfalseを返す" );

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
