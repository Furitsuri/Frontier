using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Frontier.FormTroop
{
    /// <summary>
    /// RecruitScene のメインフロー。RecruitPhaseHandler を駆動し、
    /// 編成完了でフィールドシーンへ帰還します。
    /// </summary>
    public class RecruitRoutineController : FocusRoutineBase
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        private const string FieldSceneName = "FieldScene";

        private RecruitPhaseHandler _handler = null;

        public override void Init()
        {
            base.Init();

            _handler = _hierarchyBld.InstantiateWithDiContainer<RecruitPhaseHandler>( true );
            _handler.Enter();
        }

        public override void UpdateRoutine()
        {
            _handler.Update();
        }

        public override void LateUpdateRoutine()
        {
            if( _handler.LateUpdate() )
            {
                SceneManager.LoadScene( FieldSceneName );
            }
        }

        public override void FixedUpdateRoutine()
        {
            _handler.FixedUpdate();
        }

        public override int GetPriority() { return ( int ) FocusRoutinePriority.MAIN_FLOW; }
    }
}
