/// <summary>
/// 指定のルーチンに対して優先度を設定した上で、優先度順に駆動させるためのインターフェースです
/// </summary>
public interface IFocusRoutine
{
    public void UpdateRoutine() {}
    
    public void LateUpdateRoutine() {}

    public void FixedUpdateRoutine() {}

    /// <summary>
    /// 表示(Presenter等)の更新を行います。ゲームの状態は変更せず、見た目の更新のみを行ってください。
    /// 実行中のルーチンは毎フレーム呼ばれます。一時停止中のルーチンは、実行中のルーチンの
    /// KeepsPausedPresentationがtrueの場合のみ呼ばれます。
    /// </summary>
    public void UpdatePresentation() {}

    /// <summary>
    /// このルーチンの実行中も、一時停止させた他のルーチンの表示更新(UpdatePresentation)を続けるかどうか。
    /// 画面の一部として溶け込む割り込み(戦闘シーケンス等)はtrue、画面を覆う割り込み(チュートリアル等)はfalseとします。
    /// </summary>
    public bool KeepsPausedPresentation => false;

    public void ScheduleRun();

    public void Run();

    public void Restart();

    public void Pause();

    public void ScheduleExit();

    public void Exit();

    public bool IsMatchFocusState(FocusState state);

    public int GetPriority();
}