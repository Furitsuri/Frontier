using Frontier.Stage;
using Frontier.Battle;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using Zenject;

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
            Container.Bind<StageData>().AsSingle();
            Container.Bind<ISaveHandler<TutorialSaveData>>().To<TutorialSaveHandler>().AsSingle();
            Container.Bind<IInstaller>().To<DiInstaller>().FromInstance(this);
            Container.Bind<HierarchyBuilderBase>().To<HierarchyBuilder>().FromComponentInHierarchy().AsCached();
            Container.Bind<BattleRoutineController>().FromComponentInHierarchy().AsCached();
            Container.Bind<TutorialHandler>().FromComponentInHierarchy().AsCached();
            Container.Bind<TutorialPresenter>().FromComponentInHierarchy().AsCached();
            Container.Bind<StageController>().FromComponentInHierarchy().AsCached();
            Container.Bind<IUiSystem>().To<UISystem>().FromComponentInHierarchy().AsCached();
#if UNITY_EDITOR
            Container.Bind<DebugEditorMonoDriver>().FromComponentInHierarchy().AsCached();
            Container.Bind<DebugMenuHandler>().FromComponentInHierarchy().AsCached();
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
    }
}