using Frontier.Shop;
using UnityEngine;
using Zenject;

#if UNITY_EDITOR

namespace Frontier.DebugTools.ShopDebug
{
    /// <summary>
    /// ShopScene(ショップ機能のデバッグ確認用・単独起動シーン)のメインフロー。RecruitRoutineControllerと同じ位置づけで、
    /// ShopRoutineControllerを駆動するだけの役割です(ショップ自体の処理・入力は本番のクラスが担います)。
    /// ショップが終了した(退店した)ら、遷移先が存在しないためPlayを終了します。
    /// </summary>
    public class ShopSceneRoutine : FocusRoutineBase
    {
        private const int DUMMY_INSTANCE_ID = 1;

        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        private ShopRoutineController _shop = null;

        public override void Init()
        {
            base.Init();

            _shop = _hierarchyBld.InstantiateWithDiContainer<ShopRoutineController>( false );
            _shop.SetContext( new ShopContext( DUMMY_INSTANCE_ID, ShopBackgroundMode.Field ) );
            _shop.Setup();
            _shop.Run();
        }

        public override void UpdateRoutine()
        {
            _shop.Update();
        }

        public override void LateUpdateRoutine()
        {
            if( _shop.LateUpdate() )
            {
                Debug.Log( "[ShopSceneRoutine] ショップが終了しました。Playを終了します" );
                UnityEditor.EditorApplication.isPlaying = false;
            }
        }

        public override void FixedUpdateRoutine()
        {
            _shop.FixedUpdate();
        }

        public override void Restart()
        {
            base.Restart();

            _shop.Restart();
        }

        public override void Pause()
        {
            _shop.Pause();

            base.Pause();
        }

        public override void Exit()
        {
            _shop.Exit();

            base.Exit();
        }

        public override int GetPriority() { return ( int ) FocusRoutinePriority.MAIN_FLOW; }
    }
}

#endif // UNITY_EDITOR
