using Frontier.Stage;
using Frontier.Battle;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier
{
    public class PhaseStateBase : TreeNode<PhaseStateBase>
    {
        private bool _isBack = false;
        public int TransitIndex { get; protected set; } = -1;
        protected HierarchyBuilder _hierarchyBld        = null;
        protected InputFacade _inputFcd                 = null;
        protected BattleRoutineController _btlRtnCtrl   = null;
        protected StageController _stageCtrl            = null;
        protected UISystem _uiSystem                    = null;

        [Inject]
        public void Construct( HierarchyBuilder hierarchyBld, InputFacade inputFcd, BattleRoutineController btlRtnCtrl, StageController stgCtrl, UISystem uiSystem)
        {
            _hierarchyBld   = hierarchyBld;
            _inputFcd       = inputFcd;
            _btlRtnCtrl     = btlRtnCtrl;
            _stageCtrl      = stgCtrl;
            _uiSystem       = uiSystem;
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        virtual public void Init()
        {
            TransitIndex    = -1;
            _isBack         = false;
        }

        /// <summary>
        /// 更新します
        /// </summary>
        /// <returns>trueである場合は直前のフラグへ遷移</returns>
        virtual public bool Update()
        {
            return IsBack();
        }

        /// <summary>
        /// 現在のステートから退避します
        /// </summary>
        virtual public void Exit()
        {
            _inputFcd.ResetInputCodes();
        }

        /// <summary>
        /// 以前のステートに戻るフラグを取得します
        /// </summary>
        /// <returns>戻るフラグ</returns>
        virtual public bool IsBack()
        {
            return _isBack;
        }

        /// <summary>
        /// 入力を検知して、以前のステートに遷移するフラグをONに切り替えます
        /// </summary>
        protected void DetectRevertInput()
        {
            if (Input.GetKeyUp(KeyCode.Backspace))
            {
                Back();
            }
        }

        /// <summary>
        /// 親の遷移に戻ります
        /// </summary>
        protected void Back()
        {
            _isBack = true;
        }

        /// <summary>
        /// 死亡したキャラクターの存在を通知します
        /// </summary>
        /// <param name="characterKey">死亡したキャラクターのハッシュキー</param>
        protected void NoticeCharacterDied(CharacterHashtable.Key characterKey)
        {
            _btlRtnCtrl.BtlCharaCdr.SetDiedCharacterKey(characterKey);
        }
    }
}