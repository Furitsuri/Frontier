namespace Frontier
{
    /// <summary>
    /// 演出中のカメラ挙動を表すインターフェース。
    /// FOLLOWING 状態以外のカメラモードはこのインターフェースを実装し、
    /// BattleCameraController が毎フレームの Update を委譲します。
    /// </summary>
    public interface IBattleCameraSequence
    {
        void Update();
    }
}
