using Zenject;

namespace Frontier.Shop
{
    /// <summary>
    /// ショップ機能全体を、任意のホスト(戦闘側の商人会話State、Fieldのショップノード、ShopScene)から
    /// 同じ手順で起動・駆動できる自己完結したサブルーチンとして扱うクラスです。
    /// SetContext()でどのショップかを渡してRun()し、以後ホストが毎フレームUpdate()/LateUpdate()を呼びます。
    /// LateUpdate()がtrueを返した時点でショップは終了しており、ホストがその後の処理
    /// (戦闘への復帰、FieldProgress.MarkCleared等)を行います。
    /// FocusRoutine(優先度による割り込み)の仕組みは使いません。
    /// </summary>
    public sealed class ShopRoutineController : SubRoutineController
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private ShopHandler _shopHandler           = null;

        private ShopContext _context           = null;
        private ShopPhaseHandler _phaseHandler = null;
        private bool _isRunning                = false;

        /// <summary>
        /// 開くショップを指定します(Run()の前に呼んでください)
        /// </summary>
        public void SetContext( ShopContext context )
        {
            _context = context;
        }

        public override void Setup()
        {
            LazyInject.GetOrCreate( ref _phaseHandler, () => _hierarchyBld.InstantiateWithDiContainer<ShopPhaseHandler>( false ) );
        }

        public override void Init()
        {
        }

        public override void Run()
        {
            Init();

            _shopHandler.Open( _context );
            _phaseHandler.Enter();

            _isRunning = true;
        }

        public override void Update()
        {
            if( !_isRunning ) { return; }

            _phaseHandler.Update();
        }

        /// <summary>
        /// ショップが終了した場合にtrueを返します
        /// </summary>
        public override bool LateUpdate()
        {
            if( !_isRunning ) { return false; }
            if( !_phaseHandler.LateUpdate() ) { return false; }

            Close();

            return true;
        }

        public override void FixedUpdate()
        {
            if( !_isRunning ) { return; }

            _phaseHandler.FixedUpdate();
        }

        public override void Restart()
        {
            if( !_isRunning ) { return; }

            _phaseHandler.Restart();
        }

        public override void Pause()
        {
            if( !_isRunning ) { return; }

            _phaseHandler.Pause();
        }

        /// <summary>
        /// ショップの終了を待たずに強制終了します(既に終了している場合は何もしません)
        /// </summary>
        public override void Exit()
        {
            if( !_isRunning ) { return; }

            _phaseHandler.Exit();

            Close();
        }

        private void Close()
        {
            _isRunning = false;

            _shopHandler.Close();
        }
    }
}
