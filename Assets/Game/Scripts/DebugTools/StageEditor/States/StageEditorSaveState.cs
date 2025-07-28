namespace Frontier.DebugTools.StageEditor
{
    public class StageEditorSaveState : StageEditorSaveLoadState
    {
        public override void Init()
        {
            SetWordCallback("Save Completed!");

            base.Init();
        }
    }
}