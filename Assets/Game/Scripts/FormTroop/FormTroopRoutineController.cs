using Zenject;

namespace Frontier.FormTroop
{
    public sealed class FormTroopRoutineController : SubRoutineController
    {
        [Inject] HierarchyBuilderBase _hierarchyBld = null;

        private RecruitmentPhaseHandler _handler = null;

        public override void Init()
        {
            LazyInject.GetOrCreate( ref _handler, () => _hierarchyBld.InstantiateWithDiContainer<RecruitmentPhaseHandler>( true ) );

            _handler.Init();
        }

        public override void Update()
        {
            _handler.Update();
        }

        public override void LateUpdate()
        {
            _handler.LateUpdate();
        }

        public override void FixedUpdate()
        {
            _handler.FixedUpdate();
        }

        public override void Run()
        {
            _handler.Run();
        }

        public override void Restart()
        {
            _handler.Restart();
        }

        public override void Pause()
        {
            _handler.Pause();
        }


        public override void Exit()
        {
            _handler.Exit();
        }
    }
}