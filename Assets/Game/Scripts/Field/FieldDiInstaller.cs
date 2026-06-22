using Frontier.UI;
using Zenject;

namespace Frontier.Field
{
    /// <summary>
    /// FieldScene 用の最小限の DI バインド設定。
    /// 入力ガイドUI(InputGuidePresenter)が依存する IUiSystem / HierarchyBuilderBase のみを提供する。
    /// </summary>
    public class FieldDiInstaller : MonoInstaller, IInstaller
    {
        /// <summary>
        /// DIコンテナのバインド対象を設定します
        /// </summary>
        public override void InstallBindings()
        {
            Container.Bind<IUiSystem>().To<UISystem>().FromComponentInHierarchy().AsCached();
            Container.Bind<HierarchyBuilderBase>().FromComponentInHierarchy().AsCached();

            Container.Bind<IInstaller>().To<FieldDiInstaller>().FromInstance( this );
        }

        /// <summary>
        /// 外部クラスからDIコンテナに対象をバインド設定します
        /// </summary>
        public void InstallBindings<T>( T instance )
        {
            Container.Bind<T>().FromInstance( instance ).AsCached();
        }

        public void Rebind<T>( T instance )
        {
            Container.Rebind<T>().FromInstance( instance ).AsCached();
        }
    }
}
