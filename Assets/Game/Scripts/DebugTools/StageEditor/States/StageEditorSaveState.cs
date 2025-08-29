namespace Frontier.DebugTools.StageEditor
{
    public class StageEditorSaveState : StageEditorSaveLoadState
    {
        override public void Init()
        {
            SetWordCallback("Save Completed!");

            base.Init();
        }
    }
}