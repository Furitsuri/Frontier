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
        /// DI�R���e�i�̃o�C���h�Ώۂ�ݒ肵�܂�
        /// </summary>
        public override void InstallBindings()
        {
            Container.Bind<HierarchyBuilder>().FromComponentInHierarchy().AsCached();
            Container.Bind<UISystem>().FromComponentInHierarchy().AsCached();
            Container.Bind<DiInstaller>().FromInstance(this);
        }

        /// <summary>
        /// �O���N���X����DI�R���e�i�ɑΏۂ��o�C���h�ݒ肵�܂�
        /// </summary>
        /// <typeparam name="T">�o�C���h�Ώۂ̌^</typeparam>
        /// <param name="instance">�o�C���h�Ώ�</param>
        public void InstallBindings<T>( T instance )
        {
            Container.Bind<T>().FromInstance( instance ).AsCached();
        }
    }
}