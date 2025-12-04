namespace Frontier.DebugTools.StageEditor
{
    public class StageEditorSaveState : StageEditorSaveLoadState
    {
        override public void Init()
        {
            _confirmMessage[( int ) State.CONFIRM]  = "SAVE THIS STAGE?";
            _confirmMessage[( int ) State.NOTIFY]   = "STAGE SAVED";
            _failedMessage                          = "FAILED TO SAVE STAGE";

            base.Init();
        }
    }
}