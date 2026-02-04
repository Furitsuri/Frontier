public abstract class SubRoutineController
{
    public abstract void Setup();
    public abstract void Init();
    public abstract void Update();
    public abstract bool LateUpdate();
    public abstract void FixedUpdate();
    public abstract void Run();
    public abstract void Restart();
    public abstract void Pause();
    public abstract void Exit();   
}