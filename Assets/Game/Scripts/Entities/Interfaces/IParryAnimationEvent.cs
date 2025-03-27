/// <summary>
/// キャラクターの基本アニメーション(モーション)から起動する関数を実装します
/// </summary>
public interface IParryAnimationEvent
{
    /// <summary>
    /// パリィイベントを開始します
    /// </summary>
    public void StartParryOnAnimEvent();

    /// <summary>
    /// 敵の攻撃を弾く動作を行います
    /// </summary>
    public void ParryAttackOnAnimEvent();

    /// <summary>
    /// パリィ動作を停止させます
    /// </summary>
    public void StopParryAnimationOnAnimEvent();
}