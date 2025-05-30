﻿using Frontier.Stage;
using Frontier.Battle;
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
        override public void InstallBindings()
        {
            Container.Bind<InputFacade>().AsSingle();
            Container.Bind<TutorialFacade>().AsSingle();
            Container.Bind<ISaveHandler<TutorialSaveData>>()
                 .To<TutorialSaveHandler>()
                 .AsSingle();
            Container.Bind<DiInstaller>().FromInstance(this);
            Container.Bind<HierarchyBuilder>().FromComponentInHierarchy().AsCached();
            Container.Bind<BattleRoutineController>().FromComponentInHierarchy().AsCached();
            Container.Bind<StageController>().FromComponentInHierarchy().AsCached();
            Container.Bind<UISystem>().FromComponentInHierarchy().AsCached();
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