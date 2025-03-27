/// <summary>
/// キャラクターの基本アニメーション(モーション)から起動する関数を実装します
/// </summary>
public interface IBasicAnimationEvent
{
    /// <summary>
    /// 死亡処理を開始します
    /// </summary>
    public void DieOnAnimEvent();

    /// <summary>
    /// キャラクターに設定されている弾を発射します
    /// </summary>
    public void FireBulletOnAnimEvent();

    /// <summary>
    /// 相手を攻撃した際の処理を開始します
    /// </summary>
    public void AttackOpponentOnAnimEvent();
}