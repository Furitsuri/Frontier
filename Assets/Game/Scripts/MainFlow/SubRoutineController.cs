public abstract class SubRoutineController
{
    public abstract void Setup();
    public abstract void Init();
    public abstract void Update();
    /// <summary>
    /// 表示(Presenter等)の更新を行います。親のFocusRoutineが一時停止中でも呼ばれる場合があるため、
    /// ゲームの状態は変更せず見た目の更新のみを行ってください。
    /// </summary>
    public virtual void UpdatePresentation() { }
    public abstract bool LateUpdate();
    public abstract void FixedUpdate();
    public abstract void Run();
    public abstract void Restart();
    public abstract void Pause();
    public abstract void Exit();   
}