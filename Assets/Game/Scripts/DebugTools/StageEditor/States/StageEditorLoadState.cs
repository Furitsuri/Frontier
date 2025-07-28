namespace Frontier.DebugTools.StageEditor
{
    public class StageEditorLoadState : StageEditorSaveLoadState
    {
        public override void Init()
        {
            SetWordCallback("Load Completed!");

            base.Init();
        }
    }
}