/// <summary>
/// 指定のルーチンに対して優先度を設定した上で、優先度順に駆動させるためのインターフェースです
/// </summary>
public interface IFocusRoutine
{
    public void UpdateRoutine() {}
    
    public void LateUpdateRoutine() {}

    public void FixedUpdateRoutine() {}

    public void ScheduleRun();

    public void Run();

    public void Restart();

    public void Pause();

    public void ScheduleExit();

    public void Exit();

    public bool IsMatchFocusState(FocusState state);

    public int GetPriority();
}