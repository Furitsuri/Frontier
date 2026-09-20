using Frontier.UI;
using Zenject;

namespace Frontier.Shop
{
    /// <summary>
    /// ShopHandler の状態を IUiSystem.ShopUi へ薄く転送するPresenterです。
    /// 各StateやHandlerはIUiSystemに直接アクセスせず、必ずこのPresenter経由で開閉を行ってください。
    /// 各シーンのDIInstallerでバインドされている前提です(DIInstaller.cs / FieldDiInstaller.cs)。
    /// </summary>
    public class ShopPresenter
    {
        [Inject] private IUiSystem   _uiSystem    = null;
        [Inject] private ShopHandler _shopHandler = null;

        public void Open( ShopContext context )
        {
            _shopHandler.Open( context );

            // TODO: ShopUi側のView実装後、品揃え・在庫の表示反映を行う
            _uiSystem.ShopUi?.Init();
        }

        public void Close()
        {
            _shopHandler.Close();

            _uiSystem.ShopUi?.Exit();
        }
    }
}
