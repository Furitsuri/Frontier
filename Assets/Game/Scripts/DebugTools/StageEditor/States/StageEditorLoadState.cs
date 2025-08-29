namespace Frontier.DebugTools.StageEditor
{
    public class StageEditorLoadState : StageEditorSaveLoadState
    {
        override public void Init()
        {
            SetWordCallback("Load Completed!");

            base.Init();
        }
    }
}