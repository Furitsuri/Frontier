using Frontier.Option;
using Frontier.Registries;
using Frontier.Tutorial;
using Frontier.UI;
using Zenject;

namespace Frontier.Field
{
    /// <summary>
    /// FieldScene 用の DI バインド設定。
    /// </summary>
    public class FieldDiInstaller : MonoInstaller, IInstaller
    {
        /// <summary>
        /// DIコンテナのバインド対象を設定します
        /// </summary>
        public override void InstallBindings()
        {
            Container.Bind<IUiSystem>().To<UISystem>().FromComponentInHierarchy().AsCached();
            // 戦闘エンティティ層(Character等)がFieldSceneでもDI解決できるよう、DiInstaller.csと同じBindを用意する
            Container.Bind<ICharacterUiFeedback>().FromMethod( ctx => ctx.Container.Resolve<IUiSystem>().BattleUi ).AsCached();
            Container.Bind<HierarchyBuilderBase>().FromComponentInHierarchy().AsCached();
            Container.Bind<InputFacade>().FromInstance( InputFacade.Instance ).AsCached();
            Container.Bind<OptionHandler>().FromComponentInHierarchy().AsCached();

            // フィールド上にキャラクターの3Dモデルを表示する FieldPlayerCharacterView が必要とする依存関係
            // PrefabRegistry は全シーン共通の ScriptableObject アセット(Resources/PrefabRegistry)を共有する
            Container.Bind<PrefabRegistry>().FromInstance( UnityEngine.Resources.Load<PrefabRegistry>( "PrefabRegistry" ) ).AsCached();
            Container.Bind<TimeScaleController>().AsSingle();
            Container.Bind<CharacterFactory>().AsSingle();

            // FocusRoutineController共通処理(TutorialFacade)が必要とする依存関係
            Container.Bind<ILocalizationService>().To<LocalizationService>().AsSingle();
            Container.Bind<ISaveHandler<TutorialSaveData>>().To<TutorialSaveHandler>().AsSingle();
            Container.Bind<TutorialFacade>().AsSingle();
            Container.Bind<ISaveHandler<OptionSaveData>>().To<OptionSaveHandler>().AsSingle();

            // SaveLoadPresenter(セーブ画面)、DebugUserDataLoader.TryApply() 等が必要とする依存関係
            Container.Bind<UserDomain>().FromInstance( GameSession.Instance.UserDomain ).AsCached();
            Container.Bind<ISlotSaveHandler<UserSaveData>>().To<UserSaveHandler>().AsSingle();

            Container.Bind<IInstaller>().To<FieldDiInstaller>().FromInstance( this );
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
