using Frontier.Registries;
using Frontier.Tutorial;
using Frontier.UI;
using Zenject;

namespace Frontier.FormTroop
{
    /// <summary>
    /// RecruitScene 用の DI バインド設定。
    /// Battle専用の SequenceFacade / SkillActionReservationQueue / CombatSkillEventController は含まない。
    /// </summary>
    public class RecruitDiInstaller : MonoInstaller, IInstaller
    {
        /// <summary>
        /// DIコンテナのバインド対象を設定します
        /// </summary>
        public override void InstallBindings()
        {
            Container.Bind<ILocalizationService>().To<LocalizationService>().AsSingle();
            Container.Bind<ISaveHandler<TutorialSaveData>>().To<TutorialSaveHandler>().AsSingle();
            Container.Bind<InputFacade>().FromInstance( InputFacade.Instance ).AsCached();
            Container.Bind<TimeScaleController>().AsSingle();
            Container.Bind<TutorialFacade>().AsSingle();
            Container.Bind<CharacterFactory>().AsSingle();
            Container.Bind<UserDomain>().FromInstance( GameSession.Instance.UserDomain ).AsSingle();

            Container.Bind<IInstaller>().To<RecruitDiInstaller>().FromInstance( this );

            Container.Bind<IUiSystem>().To<UISystem>().FromComponentInHierarchy().AsCached();
            Container.Bind<FilePathRegistry>().FromComponentInHierarchy().AsCached();
            Container.Bind<HierarchyBuilderBase>().To<HierarchyBuilder>().FromComponentInHierarchy().AsCached();
            // PrefabRegistry は全シーン共通の ScriptableObject アセット(Resources/PrefabRegistry)を共有する
            Container.Bind<PrefabRegistry>().FromInstance( UnityEngine.Resources.Load<PrefabRegistry>( "PrefabRegistry" ) ).AsCached();
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
