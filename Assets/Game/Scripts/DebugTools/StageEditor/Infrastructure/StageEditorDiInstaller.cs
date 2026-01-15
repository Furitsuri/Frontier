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
            Container.Bind<InputFacade>().AsSingle();
            Container.Bind<IStageDataProvider>().To<StageDataProvider>().AsSingle();

            Container.Bind<IInstaller>().To<StageEditorDiInstaller>().FromInstance(this);

            Container.Bind<StageEditorController>().FromComponentInHierarchy().AsCached();
            Container.Bind<IUiSystem>().To<EditorUiSystem>().FromComponentInHierarchy().AsCached();
            Container.Bind<HierarchyBuilderBase>().To<StageEditorHierarchyBuilder>().FromComponentInHierarchy().AsCached();
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