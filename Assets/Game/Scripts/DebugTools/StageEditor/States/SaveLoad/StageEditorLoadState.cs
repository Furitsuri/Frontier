namespace Frontier.DebugTools.StageEditor
{
    public class StageEditorLoadState : StageEditorSaveLoadState
    {
        public override void Init( object context )
        {
            _confirmMessage[( int ) State.CONFIRM]  = "LOAD STAGE?";
            _confirmMessage[( int ) State.NOTIFY]   = "STAGE LOADED";
            _failedMessage                          = "FAILED TO LOAD STAGE";

            base.Init( context);
        }
    }
}