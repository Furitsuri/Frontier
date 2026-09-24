using Frontier.Registries;
using Frontier.Shop;
using Frontier.Tutorial;
using Frontier.UI;
using Zenject;

#if UNITY_EDITOR

namespace Frontier.DebugTools.ShopDebug
{
    /// <summary>
    /// ShopScene(ショップ機能のデバッグ確認用・単独起動シーン)用の DI バインド設定。
    /// RecruitDiInstallerと同じ共通基盤(入力・チュートリアル・UI)に加え、ショップ用のバインドを持つ。
    /// 本番のGameSession/UserDomainには依存せず、ダミーのUserDomainをバインドする。
    /// </summary>
    public class ShopDiInstaller : MonoInstaller, IInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<ILocalizationService>().To<LocalizationService>().AsSingle();
            Container.Bind<ISaveHandler<TutorialSaveData>>().To<TutorialSaveHandler>().AsSingle();
            Container.Bind<InputFacade>().FromInstance( InputFacade.Instance ).AsCached();
            Container.Bind<TimeScaleController>().AsSingle();
            Container.Bind<TutorialFacade>().AsSingle();
            Container.Bind<CharacterFactory>().AsSingle();
            Container.Bind<UserDomain>().FromInstance( new UserDomain() ).AsSingle();

            Container.Bind<IInstaller>().To<ShopDiInstaller>().FromInstance( this );

            Container.Bind<IUiSystem>().To<UISystem>().FromComponentInHierarchy().AsCached();
            // 戦闘エンティティ層(Character等)がShopSceneでもDI解決できるよう、DiInstaller.csと同じBindを用意する
            // (ShopScene用UISystemのBattleUiはnullを返すが、戦闘UI演出メソッドを呼ばないため問題ない)
            Container.Bind<ICharacterUiFeedback>().FromMethod( ctx => ctx.Container.Resolve<IUiSystem>().BattleUi ).AsCached();
            Container.Bind<TalkWindowPresenter>().AsSingle();
            Container.Bind<TalkWindowConfirmPresenter>().AsSingle();
            Container.Bind<GeneralHeaderPresenter>().AsSingle();
            Container.Bind<FilePathRegistry>().FromComponentInHierarchy().AsCached();
            Container.Bind<HierarchyBuilderBase>().To<HierarchyBuilder>().FromComponentInHierarchy().AsCached();
            // PrefabRegistry は全シーン共通の ScriptableObject アセット(Resources/PrefabRegistry)を共有する
            Container.Bind<PrefabRegistry>().FromInstance( UnityEngine.Resources.Load<PrefabRegistry>( "PrefabRegistry" ) ).AsCached();

            Container.Bind<ShopHandler>().AsSingle();
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

#endif // UNITY_EDITOR
