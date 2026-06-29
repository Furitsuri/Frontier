using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Frontier.FormTroop
{
    /// <summary>
    /// RecruitScene のメインフロー。FormTroopRoutineController を駆動し、
    /// 編成完了でフィールドシーンへ帰還します。
    /// </summary>
    public class RecruitRoutineController : FocusRoutineBase
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        private const string FieldSceneName = "FieldScene";

        private FormTroopRoutineController _formTroop = null;

        public override void Init()
        {
            base.Init();

            _formTroop = _hierarchyBld.InstantiateWithDiContainer<FormTroopRoutineController>( false );
            _formTroop.Setup();
            _formTroop.Run();
        }

        public override void UpdateRoutine()
        {
            _formTroop.Update();
        }

        public override void LateUpdateRoutine()
        {
            if( _formTroop.LateUpdate() )
            {
                SceneManager.LoadScene( FieldSceneName );
            }
        }

        public override void FixedUpdateRoutine()
        {
            _formTroop.FixedUpdate();
        }

        public override int GetPriority() { return ( int ) FocusRoutinePriority.MAIN_FLOW; }
    }
}
