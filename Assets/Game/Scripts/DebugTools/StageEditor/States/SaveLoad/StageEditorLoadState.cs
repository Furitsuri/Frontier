namespace Frontier.DebugTools.StageEditor
{
    public class StageEditorLoadState : StageEditorSaveLoadState
    {
        override public void Init()
        {
            _confirmMessage[( int ) State.CONFIRM]  = "LOAD STAGE?";
            _confirmMessage[( int ) State.NOTIFY]   = "STAGE LOADED";
            _failedMessage                          = "FAILED TO LOAD STAGE";

            base.Init();
        }
    }
}