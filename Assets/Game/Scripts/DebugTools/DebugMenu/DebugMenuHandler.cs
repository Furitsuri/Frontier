using System;
using System.Collections.ObjectModel;
using TMPro;
using Zenject;
using static Constants;

#if UNITY_EDITOR

namespace Frontier.DebugTools.DebugMenu
{
    public class DebugMenuHandler : BaseHandlerExtendedFocusRoutine
    {
        private InputFacade _inputFcd = null;
        private DebugMenuPresenter _debugMenuView = null;
        private IDebugLauncher[] _debugLhr = null;
        // 選択中のメニューインデックス
        private int _currentMenuIndex = 0;
        private int _inputHashCode = -1;
        private ReadOnlyCollection<TextMeshProUGUI> _menuTexts;
        // デバッグON/OFFのコールバック
        public delegate void ToggleDebugCallback();
        // デバッグメニューへの遷移の入力可否コールバック
        private InputCode.EnableCallback _canAcceptDebugTransitionCb = null;
        // デバッグメニューへの遷移入力のコールバック
        private AcceptBooleanInput.AcceptBooleanInputCallback _acceptDebugTransitionCb = null;

        public ToggleDebugCallback _toggleDebugCb = null;

        [Inject]
        public void Construct(InputFacade inputFcd)
        {
            _inputFcd = inputFcd;
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        public void Init(DebugMenuPresenter debugMenuView, ToggleDebugCallback cb, InputCode.EnableCallback canAcceptCb, AcceptBooleanInput.AcceptBooleanInputCallback acceptInputCb)
        {
            base.Init();

            _debugMenuView = debugMenuView;
            _menuTexts = _debugMenuView.MenuTexts;
            _currentMenuIndex = 0;
            _toggleDebugCb = cb;
            _debugLhr = new IDebugLauncher[(int)DebugMainMenuTag.MAX];
            _inputHashCode = Hash.GetStableHash(GetType().Name);
            _canAcceptDebugTransitionCb = canAcceptCb;
            _acceptDebugTransitionCb = acceptInputCb;
        }

        /// <summary>
        /// デバッグ画面全体とデバッグメニューの表示・非表示を切り替えます
        /// </summary>
        private void ToggleDebugView()
        {
            _toggleDebugCb?.Invoke();
            _debugMenuView.ToggleMenuVisibility();
        }

        private void RegisterInputCodes()
        {
            int hashCode = Hash.GetStableHash(GetType().Name);

            _inputFcd.RegisterInputCodes(
                (GuideIcon.VERTICAL_CURSOR, "SELECT", CanAcceptDirection, new AcceptDirectionInput(AcceptDirection), MENU_DIRECTION_INPUT_INTERVAL, hashCode),
                (GuideIcon.CONFIRM, "CONFIRM", CanAcceptConfirm, new AcceptBooleanInput(AcceptConfirm), 0.0f, hashCode),
                (GuideIcon.CANCEL, "EXIT", CanAcceptCancel, new AcceptBooleanInput(AcceptCancel), 0.0f, hashCode)
            );
        }

        /// <summary>
        /// 指定のIndexに対応するデバッグメニューを起動します
        /// </summary>
        /// <param name="menuIdx">指定するIndex値</param>
        private void LaunchDebugMenu(int menuIdx)
        {
            if (_debugLhr[menuIdx] == null)
            {
                switch (menuIdx)
                {
                    case (int)DebugMainMenuTag.BATTLE:
                        break;
                    case (int)DebugMainMenuTag.TUTORIAL:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(menuIdx), "Invalid menu index for debug launcher.");
                }

                NullCheck.AssertNotNull(_debugLhr[menuIdx], "_debugLhr[menuIdx]");
            }

            // 現在の入力コードを抹消
            _inputFcd.UnregisterInputCodes(_inputHashCode);

            _debugLhr[menuIdx].Init();
            _debugLhr[menuIdx].LaunchEditor();
        }

        private bool CanAcceptDirection()
        {
            return true;
        }

        private bool CanAcceptConfirm()
        {
            return true;
        }

        private bool CanAcceptCancel()
        {
            return true;
        }

        private bool AcceptDirection(Direction dir)
        {
            if (dir == Direction.FORWARD)
            {
                // 前のメニューへ
                _currentMenuIndex = (_currentMenuIndex - 1 + _menuTexts.Count) % _menuTexts.Count;

                return true;
            }
            else if (dir == Direction.BACK)
            {
                // 次のメニューへ
                _currentMenuIndex = (_currentMenuIndex + 1) % _menuTexts.Count;

                return true;
            }

            return false;
        }

        /// <summary>
        /// 決定入力を受け取った際の処理を行います
        /// </summary>
        /// <param name="isInput">決定入力</param>
        /// <returns>入力実行の有無</returns>
        private bool AcceptConfirm(bool isInput)
        {
            if (isInput)
            {
                Pause();
                LaunchDebugMenu(_currentMenuIndex);

                return true;
            }

            return false;
        }

        /// <summary>
        /// オプション入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isInput">オプション入力</param>
        /// <returns>入力実行の有無</returns>
        private bool AcceptCancel(bool isInput)
        {
            if (isInput)
            {
                ScheduleExit();

                return true;
            }

            return false;
        }

        // =========================================================
        // IFocusRoutine 実装
        // =========================================================

        #region IFocusRoutine Implementation

        /// <summary>
        /// 更新を行います
        /// </summary>
        override public void UpdateRoutine()
        {
            _debugMenuView.UpdateMenuCursor(_currentMenuIndex);
        }

        override public void Run()
        {
            base.Run();

            ToggleDebugView();
            _inputFcd.UnregisterInputCodes();
            RegisterInputCodes();
        }

        override public void Restart()
        {
            base.Restart();

            _debugMenuView.ToggleMenuVisibility();
            _inputFcd.UnregisterInputCodes(_inputHashCode);
            RegisterInputCodes();
        }

        override public void Pause()
        {
            base.Pause();

            _debugMenuView.ToggleMenuVisibility();
            _inputFcd.UnregisterInputCodes(_inputHashCode);
        }

        override public void Exit()
        {
            base.Exit();

            ToggleDebugView();
            _inputFcd.UnregisterInputCodes(_inputHashCode);
            int hashCode = Hash.GetStableHash(Constants.DEBUG_TRANSION_INPUT_HASH_STRING);
            _inputFcd.RegisterInputCodes((Constants.GuideIcon.DEBUG_MENU, "DEBUG", _canAcceptDebugTransitionCb, new AcceptBooleanInput(_acceptDebugTransitionCb), 0.0f, hashCode));
        }

        override public int GetPriority() { return (int)FocusRoutinePriority.DEBUG_MENU; }

        #endregion  // IFocusRoutine 実装
    }
}

#endif // UNITY_EDITOR