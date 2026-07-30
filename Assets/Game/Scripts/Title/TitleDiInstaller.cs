using Frontier.Option;
using Frontier.Registries;
using Frontier.UI;
using Zenject;

namespace Frontier.Title
{
    /// <summary>
    /// Titleシーン用のDIバインド設定。
    /// </summary>
    public class TitleDiInstaller : MonoInstaller, IInstaller
    {
        /// <summary>
        /// DIコンテナのバインド対象を設定します
        /// </summary>
        public override void InstallBindings()
        {
            Container.Bind<HierarchyBuilderBase>().FromComponentInHierarchy().AsCached();
            Container.Bind<InputFacade>().FromInstance( InputFacade.Instance ).AsCached();
            Container.Bind<ILocalizationService>().To<LocalizationService>().AsSingle();
            Container.Bind<ISlotSaveHandler<UserSaveData>>().To<UserSaveHandler>().AsSingle();
            // FocusRoutineController.Awake()が構築するInputGuidePresenter(入力ガイドバー)が必要とする依存関係
            Container.Bind<IUiSystem>().To<UISystem>().FromComponentInHierarchy().AsCached();
            // タイトルメニューのOPTION項目(OptionHandler)が必要とする依存関係
            Container.Bind<ISaveHandler<OptionSaveData>>().To<OptionSaveHandler>().AsSingle();
            Container.Bind<OptionHandler>().FromComponentInHierarchy().AsCached();
            // PrefabRegistry は全シーン共通の ScriptableObject アセット(Resources/PrefabRegistry)を共有する
            Container.Bind<PrefabRegistry>().FromInstance( UnityEngine.Resources.Load<PrefabRegistry>( "PrefabRegistry" ) ).AsCached();

            Container.Bind<IInstaller>().To<TitleDiInstaller>().FromInstance( this );
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
