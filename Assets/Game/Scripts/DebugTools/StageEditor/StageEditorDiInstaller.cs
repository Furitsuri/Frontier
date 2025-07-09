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
    public class StageEditorDiInstaller : MonoInstaller, IInstaller
    {
        /// <summary>
        /// DIコンテナのバインド対象を設定します
        /// </summary>
        override public void InstallBindings()
        {
            Container.Bind<InputFacade>().AsSingle();
            Container.Bind<IInstaller>().To<StageEditorDiInstaller>().FromInstance(this);
            Container.Bind<IUiSystem>().To<EditorUiSystem>().FromComponentInHierarchy().AsCached();
            Container.Bind<HierarchyBuilderBase>().To<SEditorHierarchyBuilder>().FromComponentInHierarchy().AsCached();
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
    }
}