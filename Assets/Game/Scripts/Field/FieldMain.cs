using Frontier.UI;
using UnityEngine;
using Zenject;

namespace Frontier.Field
{
    /// <summary>
    /// FieldScene のエントリポイント。
    /// InputFacade のセットアップと、デバッグデータの読み込みを担います。
    /// </summary>
    public class FieldMain : FocusRoutineController
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Inject] private UserDomain _userDomain             = null;
        [Inject] private CharacterFactory _characterFactory = null;
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

        private InputGuidePresenter _inputGuideView;

        protected override int GetRequiredRoutineCount() => (int)FocusRoutinePriority.NUM;

        void Awake()
        {
            LoadingScreenController.EnsureInstance();

            LazyInject.GetOrCreate( ref _inputGuideView, () => _hierarchyBld.InstantiateWithDiContainer<InputGuidePresenter>( false ) );

            // InputFacade はシーンを跨いで永続化されるシングルトン。
            // 入力ガイドUIはこのシーン(FieldScene)の IUiSystem に紐づくため、シーンに入る度に渡し直す
            InputFacade.Instance.Setup( _inputGuideView );
            InputFacade.Instance.Init();
        }

        void Start()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Frontier.DebugTools.DebugUserDataLoader.TryApply( _userDomain, _characterFactory );
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

            base.Init();
        }

        void Update()        => base.UpdateRoutine();
        void LateUpdate()    => base.LateUpdateRoutine();
        void FixedUpdate()   => base.FixedUpdateRoutine();
    }
}
