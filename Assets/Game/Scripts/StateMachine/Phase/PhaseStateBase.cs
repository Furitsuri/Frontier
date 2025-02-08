using Frontier.Stage;
using Frontier.Battle;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

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

        // 初期化
        virtual public void Init()
        {
            TransitIndex    = -1;
            _isBack         = false;
        }

        // 更新
        virtual public bool Update()
        {
            if (Input.GetKeyUp(KeyCode.Backspace))
            {
                Back();

                return true;
            }

            return false;
        }

        /// <summary>
        /// キーガイドを更新します
        /// </summary>
        virtual public void UpdateInputGuide()
        {

        }

        // 退避
        virtual public void Exit()
        {
            _inputFcd.ResetInputCodes();
        }

        // 戻る
        virtual public bool IsBack()
        {
            return _isBack;
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