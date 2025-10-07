using Frontier.Stage;
using Frontier.Battle;
using Zenject;
using static Constants;

namespace Frontier
{
    public class PhaseStateBase : StateBase
    {
        protected HierarchyBuilderBase _hierarchyBld    = null;
        protected BattleRoutineController _btlRtnCtrl   = null;
        protected StageController _stageCtrl            = null;
        protected IUiSystem _uiSystem                   = null;
        private TutorialFacade _tutorialFcd             = null;

        [Inject]
        public void Construct( HierarchyBuilderBase hierarchyBld, InputFacade inputFcd, BattleRoutineController btlRtnCtrl, StageController stgCtrl, IUiSystem uiSystem, TutorialFacade tutorialFcd )
        {
            _hierarchyBld   = hierarchyBld;
            _inputFcd       = inputFcd;
            _btlRtnCtrl     = btlRtnCtrl;
            _stageCtrl      = stgCtrl;
            _uiSystem       = uiSystem;
            _tutorialFcd    = tutorialFcd;
        }

        /// <summary>
        /// 死亡したキャラクターの存在を通知します
        /// </summary>
        /// <param name="characterKey">死亡したキャラクターのハッシュキー</param>
        protected void NorifyCharacterDied( in CharacterKey characterKey )
        {
            _btlRtnCtrl.BtlCharaCdr.SetDiedCharacterKey( characterKey );
            _btlRtnCtrl.BtlCharaCdr.RemoveCharacterFromList( characterKey );
        }

        /// <summary>
        /// 入力コードのハッシュ値を取得します
        /// </summary>
        /// <returns>入力コードのハッシュ値</returns>
        protected int GetInputCodeHash()
        {
            return Hash.GetStableHash(GetType().Name);
        }

        /// <summary>
        /// 現在のステートを実行します
        /// </summary>
        override public void RunState()
        {
            base.RunState();
            RegisterInputCodes();
        }

        /// <summary>
        /// 現在のステートを再開します
        /// </summary>
        override public void RestartState()
        {
            base.RestartState();
            RegisterInputCodes();
        }

        /// <summary>
        /// 現在のステートを中断します
        /// </summary>
        override public void PauseState()
        {
            base.PauseState();
            UnregisterInputCodes(Hash.GetStableHash(GetType().Name));
        }

        /// <summary>
        /// 現在のステートから退避します
        /// </summary>
        override public void ExitState()
        {
            base.ExitState();
            UnregisterInputCodes(Hash.GetStableHash(GetType().Name));

            // 表示すべきチュートリアルがある場合はチュートリアル遷移に移行
            _tutorialFcd.TryShowTutorial();
        }

        /// <summary>
        /// 入力コードを登録します
        /// </summary>
        virtual public void RegisterInputCodes() { }

        /// <summary>
        /// 指定するハッシュ値の入力コードを登録解除します
        /// </summary>
        /// <param name="hashCode">ハッシュ値</param>
        virtual public void UnregisterInputCodes( int hashCode )
        {
            _inputFcd.UnregisterInputCodes( hashCode );
        }

        /// <summary>
        /// 入力を受付るかを取得します
        /// 多くのケースでこちらの関数を用いて判定します
        /// </summary>
        /// <returns>入力受付の可否</returns>
        virtual protected bool CanAcceptDefault()
        {
            // 現在のステートから脱出する場合は入力を受け付けない
            return !IsBack();
        }

        virtual protected bool CanAcceptDirection() { return false; }

        virtual protected bool CanAcceptConfirm() { return false; }

        virtual protected bool CanAcceptCancel() { return false; }

        virtual protected bool CanAcceptOptional() { return false; }

        virtual protected bool CanAcceptSub1() { return false; }

        virtual protected bool CanAcceptSub2() { return false; }

        virtual protected bool CanAcceptSub3() { return false; }

        virtual protected bool CanAcceptSub4() { return false; }

        /// <summary>
        /// 方向入力を受け取った際の処理を行います
        /// </summary>
        /// <param name="dir">方向入力</param>
        /// <returns>入力実行の有無</returns>
        virtual protected bool AcceptDirection(Constants.Direction dir) { return false; }

        /// <summary>
        /// 決定入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isConfirm">決定入力</param>
        /// <returns>入力実行の有無</returns>
        virtual protected bool AcceptConfirm(bool isInput) { return false; }

        /// <summary>
        /// キャンセル入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isCancel">キャンセル入力</param>
        /// <returns>入力実行の有無</returns>
        virtual protected bool AcceptCancel(bool isCancel) { return false; }

        /// <summary>
        /// オプション入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isOptional">オプション入力</param>
        /// <returns>入力実行の有無</returns>
        virtual protected bool AcceptOptional(bool isOptional) { return false; }

        virtual protected bool AcceptSub1( bool isInput ) { return false; }

        virtual protected bool AcceptSub2(bool isInput) { return false; }

        virtual protected bool AcceptSub3(bool isInput) { return false; }

        virtual protected bool AcceptSub4(bool isInput) { return false; }
    }
}