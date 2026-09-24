using Frontier.Field;
using UnityEngine.SceneManagement;
using Zenject;

namespace Frontier.Shop
{
    /// <summary>
    /// ShopScene のメインフロー。RecruitRoutineController と同じ位置づけで、
    /// ShopRoutineController を駆動し、退店したらフィールドシーンへ帰還します。
    /// ショップ自体の処理・入力は ShopRoutineController 以下が担います。
    /// </summary>
    public class ShopSceneRoutine : FocusRoutineBase
    {
        private const string FieldSceneName = "FieldScene";

        // Fieldを経由せずShopSceneが直接起動された場合(開発者によるデバッグ起動)に使う、ショップを識別するID
        private const int DEBUG_INSTANCE_ID = 0;

        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        private ShopRoutineController _shop = null;

        public override void Init()
        {
            base.Init();

            // フィールドから遷移してきた場合は、到達したショップノードのIdで品揃えを決定する
            int instanceId = FieldTransitionContext.IsFromField ? FieldTransitionContext.ClearedNodeId : DEBUG_INSTANCE_ID;

            _shop = _hierarchyBld.InstantiateWithDiContainer<ShopRoutineController>( false );
            _shop.SetContext( new ShopContext( instanceId, ShopBackgroundMode.Field ) );
            _shop.Setup();
            _shop.Run();
        }

        public override void UpdateRoutine()
        {
            _shop.Update();
        }

        public override void LateUpdateRoutine()
        {
            if( !_shop.LateUpdate() ) { return; }

            if( FieldTransitionContext.IsFromField )
            {
                SceneManager.LoadScene( FieldSceneName );
            }
            else
            {
                // Fieldを経由せずShopSceneが直接起動された場合(開発者によるデバッグ起動)は、
                // 遷移先が存在しないためPlayを終了する
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
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
