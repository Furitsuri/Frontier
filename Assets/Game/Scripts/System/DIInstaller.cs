using Frontier.Stage;
using Frontier.UI;
using Frontier.Battle;
using Frontier.Combat.Skill;
using Frontier.DebugTools.DebugMenu;
using Zenject;
using Froniter.Registries;
using Frontier.Tutorial;

namespace Frontier
{
    public class DiInstaller : MonoInstaller, IInstaller
    {
        /// <summary>
        /// DIコンテナのバインド対象を設定します
        /// </summary>
        override public void InstallBindings()
        {
            Container.Bind<InputFacade>().AsSingle();
            Container.Bind<TutorialFacade>().AsSingle();
            Container.Bind<ISaveHandler<TutorialSaveData>>().To<TutorialSaveHandler>().AsSingle();
            Container.Bind<IStageDataProvider>().To<StageDataProvider>().AsSingle();

            Container.Bind<IInstaller>().To<DiInstaller>().FromInstance(this);

            Container.Bind<IUiSystem>().To<UISystem>().FromComponentInHierarchy().AsCached();
            Container.Bind<HierarchyBuilderBase>().To<HierarchyBuilder>().FromComponentInHierarchy().AsCached();
            Container.Bind<PrefabRegistry>().FromComponentInHierarchy().AsCached();
            Container.Bind<BattleRoutineController>().FromComponentInHierarchy().AsCached();
            Container.Bind<CombatSkillEventController>().FromComponentInHierarchy().AsCached();
            Container.Bind<TutorialHandler>().FromComponentInHierarchy().AsCached();
            Container.Bind<StageController>().FromComponentInHierarchy().AsCached();
#if UNITY_EDITOR
            Container.Bind<DebugEditorMonoDriver>().FromComponentInHierarchy().AsCached();
            Container.Bind<DebugMenuHandler >().FromComponentInHierarchy().AsCached();
            Container.Bind<DebugMenuPresenter>().FromComponentInHierarchy().AsCached();
#endif // UNITY_EDITOR
        }

        /// <summary>
        /// 外部クラスからDIコンテナに対象をバインド設定します
        /// </summary>
        /// <typeparam name="T">バインド対象の型</typeparam>
        /// <param name="instance">バインド対象</param>
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