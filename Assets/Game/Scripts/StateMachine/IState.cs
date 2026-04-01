public interface IState
{
    void OnEnter( object context );
    object ExitState();
}