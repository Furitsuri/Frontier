using System.Collections;
using Zenject;

namespace Frontier.FormTroop
{
    /// <summary>
    /// RecruitScene のエントリポイント。GameMain の Battle専用処理を取り除いた軽量版。
    /// </summary>
    public class RecruitMain : FocusRoutineController
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Inject] private UserDomain _userDomain             = null;
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

        void Start()
        {
            StartCoroutine( InitGame() );
        }

        void Update()
        {
            base.UpdateRoutine();
        }

        void LateUpdate()
        {
            base.LateUpdateRoutine();
        }

        void FixedUpdate()
        {
            base.FixedUpdateRoutine();
        }

        /// <summary>
        /// シーンを初期化します
        /// </summary>
        private IEnumerator InitGame()
        {
            enabled = false;    // 読込処理完了までUpdate()などを無効にする

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // セーブデータをロードして遷移してきた場合は、デバッグ用の初期値で上書きしない
            if ( !GameSession.Instance.IsResumedFromSave )
            {
                Frontier.DebugTools.DebugUserDataLoader.TryApply( _userDomain );
            }
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

            yield return StartCoroutine( InitCommonRoutine() );   // InputFacade / TutorialFacade の初期化、ルーチン起動、ローディング画面解除

            enabled = true; // 読込完了したため、Update()などを有効に
        }
    }
}
