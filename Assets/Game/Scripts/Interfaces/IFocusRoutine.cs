public interface IFocusRoutine
{
    public void Update();

    public void Restart();

    public void Exit();

    public bool IsRunning();

    public int GetPriority() { return (int)FocusRoutinePriority.NONE; }
}