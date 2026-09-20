using Frontier.Shop;
using Frontier.UI;
using Zenject;

#if UNITY_EDITOR

namespace Frontier.DebugTools.ShopDebug
{
    /// <summary>
    /// ShopScene(ショップ機能のデバッグ確認用・単独起動シーン)用の DI バインド設定。
    /// 本番のGameSession/UserDomainには依存せず、ダミーのUserDomainをバインドします。
    /// </summary>
    public class ShopDiInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<UserDomain>().FromInstance( new UserDomain() ).AsSingle();
            Container.Bind<IUiSystem>().To<UISystem>().FromComponentInHierarchy().AsCached();
            Container.Bind<ShopHandler>().AsSingle();
            Container.Bind<ShopPresenter>().AsSingle();
        }
    }
}

#endif // UNITY_EDITOR
