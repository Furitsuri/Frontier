namespace Frontier.Entities
{
    /// <summary>
    /// キャラクターの状態異常のカテゴリーを表す列挙型
    /// </summary>
    public enum StatusEffectCategory
    {
        Parameter = 0,  // パラメーター変化
        Action,         // 行動関連
        Move,           // 移動関連
        Etc,            // その他

        NUM
    }
}