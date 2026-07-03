using Frontier.Registries;
using Frontier.Stage;
using Zenject;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    public class StageEditorDiInstaller : MonoInstaller, IInstaller
    {
        /// <summary>
        /// DIコンテナのバインド対象を設定します
        /// </summary>
        public override void InstallBindings()
        {
            Container.Bind<InputFacade>().FromInstance( InputFacade.Instance ).AsCached();
            Container.Bind<IStageDataProvider>().To<StageDataProvider>().AsSingle();
            Container.Bind<ILocalizationService>().To<LocalizationService>().AsSingle();

            Container.Bind<IInstaller>().To<StageEditorDiInstaller>().FromInstance(this);

            // PrefabRegistry は全シーン共通の ScriptableObject アセット(Resources/PrefabRegistry)を共有する
            Container.Bind<PrefabRegistry>().FromInstance( UnityEngine.Resources.Load<PrefabRegistry>( "PrefabRegistry" ) ).AsCached();
            Container.Bind<FilePathRegistry>().FromComponentInHierarchy().AsCached();
            Container.Bind<StageEditorController>().FromComponentInHierarchy().AsCached();
            Container.Bind<IUiSystem>().To<EditorUiSystem>().FromComponentInHierarchy().AsCached();
            Container.Bind<HierarchyBuilderBase>().To<StageEditorHierarchyBuilder>().FromComponentInHierarchy().AsCached();
            Container.Bind<CharacterFactory>().AsSingle();
            Container.Bind<TimeScaleController>().AsSingle();
        }

        /// <summary>
        /// 外部クラスからDIコンテナに対象をバインド設定します
        /// </summary>
        /// <typeparam name="T">バインド対象の型</typeparam>
        /// <param name="instance">バインド対象</param>
        public void InstallBindings<T>(T instance)
        {
            Container.Bind<T>().FromInstance(instance).AsCached();
        }

        public void Rebind<T>(T instance)
        {
            Container.Rebind<T>().FromInstance( instance ).AsCached();
        }
    }
}

#endif // UNITY_EDITOR