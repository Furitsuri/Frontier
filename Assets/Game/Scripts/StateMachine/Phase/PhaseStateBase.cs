using Frontier.Stage;
using Frontier.Battle;
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
        public void Construct( HierarchyBuilder hierarchyBld, InputFacade inputFcd, BattleRoutineController btlRtnCtrl, StageController stgCtrl, UISystem uiSystem )
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

        /// <summary>
        /// 入力を受付るかを取得します
        /// 多くのケースでこちらの関数を用いて判定します
        /// </summary>
        /// <returns>入力受付の可否</returns>
        protected bool CanAcceptDefault()
        {
            // 現在のステートから脱出する場合は入力を受け付けない
            return !IsBack();
        }

        virtual protected bool CanAcceptDirection() { return false; }

        virtual protected bool CanAcceptConfirm() { return false; }

        virtual protected bool CanAcceptCancel() { return false; }

        virtual protected bool CanAcceptOptional() { return false; }

        /// <summary>
        /// 方向入力を受け取った際の処理を行います
        /// </summary>
        /// <param name="dir">方向入力</param>
        /// <returns>入力実行の有無</returns>
        virtual protected bool AcceptDirection(Constants.Direction dir)
        {
            return false;
        }

        /// <summary>
        /// 決定入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isConfirm">決定入力</param>
        /// <returns>入力実行の有無</returns>
        virtual protected bool AcceptConfirm(bool isConfirm)
        {
            return false;
        }

        /// <summary>
        /// オプション入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isOptional">オプション入力</param>
        /// <returns>入力実行の有無</returns>
        virtual protected bool AcceptOptional(bool isOptional)
        {
            return false;
        }
    }
}