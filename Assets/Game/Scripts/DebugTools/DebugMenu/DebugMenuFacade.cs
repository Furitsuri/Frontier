
using UnityEngine;
using Zenject;

#if UNITY_EDITOR

namespace Frontier.DebugTools.DebugMenu
{
    public sealed class DebugMenuFacade
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private DebugMenuHandler _handler          = null;

        private DebugMenuPresenter _presenter   = null;
        private GameObject _debugUi             = null;

        /// <summary>
        /// 初期化します
        /// </summary>
        public void Init( InputCode.EnableCallback canAcceptCb, AcceptBooleanInput.AcceptBooleanInputCallback acceptInputCb )
        {
            LazyInject.GetOrCreate( ref _debugUi, () => GameObjectFinder.FindInSceneEvenIfInactive( "DebugUI" ) );
            LazyInject.GetOrCreate( ref _presenter, () => _hierarchyBld.InstantiateWithDiContainer<DebugMenuPresenter>( false ) );

            _debugUi.SetActive( false ); // 初期状態では非表示

            _presenter.Init();
            _handler.Init( _presenter, ToggleDebugCallback, canAcceptCb, acceptInputCb );
        }

        /// <summary>
        /// デバッグメニューを開きます
        /// </summary>
        public void OpenDebugMenu()
        {
            _handler.ScheduleRun();
        }

        /// <summary>
        /// デバッグモードを切り替える際のコールバック関数です。
        /// </summary>
        public void ToggleDebugCallback()
        {
            _debugUi.SetActive( !_debugUi.activeSelf );
        }

        /// <summary>
        /// デバッグメニュー処理を行うハンドラを取得します
        /// </summary>
        /// <returns>ハンドラ</returns>
        public IFocusRoutine GetFocusRoutine()
        {
            return _handler;
        }
    }
}

#endif // UNITY_EDITOR