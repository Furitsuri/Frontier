using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using Zenject;

namespace Frontier
{
    public class DiInstaller : MonoInstaller
    {
        /// <summary>
        /// DIコンテナのバインド対象を設定します
        /// </summary>
        public override void InstallBindings()
        {
            Container.Bind<HierarchyBuilder>().FromComponentInHierarchy().AsCached();
            Container.Bind<UISystem>().FromComponentInHierarchy().AsCached();
            Container.Bind<DiInstaller>().FromInstance(this);
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