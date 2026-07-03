using System.Collections;
using Zenject;

namespace Frontier.Field
{
    /// <summary>
    /// FieldScene のエントリポイント。
    /// </summary>
    public class FieldMain : FocusRoutineController
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Inject] private UserDomain _userDomain             = null;
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

        protected override int GetRequiredRoutineCount() => (int)FocusRoutinePriority.NUM;

        void Start()
        {
            StartCoroutine( InitGame() );
        }

        void Update()        => base.UpdateRoutine();
        void LateUpdate()    => base.LateUpdateRoutine();
        void FixedUpdate()   => base.FixedUpdateRoutine();

        /// <summary>
        /// シーンを初期化します
        /// </summary>
        private IEnumerator InitGame()
        {
            enabled = false;    // 読込処理完了までUpdate()などを無効にする

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Frontier.DebugTools.DebugUserDataLoader.TryApply( _userDomain );
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

            yield return StartCoroutine( InitCommonRoutine() );   // InputFacade / TutorialFacade の初期化、ルーチン起動、ローディング画面解除

            enabled = true; // 読込完了したため、Update()などを有効に
        }
    }
}
