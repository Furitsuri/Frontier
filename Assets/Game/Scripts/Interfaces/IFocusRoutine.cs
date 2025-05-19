public enum FocusState
{
    NONE = -1,

    RUN,
    RESERVE,
    PAUSE,
    EXIT,
    MAX
}

public interface IFocusRoutine
{
    public void Run();

    public void Restart();

    public void Pause();

    public void Exit();

    public bool IsMatchFocusState(FocusState state) { return false; }

    public int GetPriority() { return (int)FocusRoutinePriority.NONE; }
}