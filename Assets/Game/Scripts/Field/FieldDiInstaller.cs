using Frontier.Registries;
using Frontier.UI;
using UnityEngine;
using Zenject;

namespace Frontier.Field
{
    /// <summary>
    /// FieldScene 用の DI バインド設定。
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
            Container.Bind<InputFacade>().FromInstance( InputFacade.Instance ).AsCached();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // DebugUserDataLoader.TryApply() が必要とする依存関係
            Container.Bind<UserDomain>().FromInstance( GameSession.Instance.UserDomain ).AsCached();
            Container.Bind<TimeScaleController>().AsSingle();
            Container.Bind<CharacterFactory>().AsSingle();
            Container.Bind<PrefabRegistry>().FromInstance( Resources.Load<PrefabRegistry>( "PrefabRegistry" ) ).AsCached();
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

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
