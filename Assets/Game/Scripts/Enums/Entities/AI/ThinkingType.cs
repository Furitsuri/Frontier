namespace Frontier.Entities
{
    /// <summary>
    /// 思考タイプ
    /// </summary>
    public enum ThinkingType
    {
        BASE        = 0,    // ベース
        AGGERESSIVE,        // 積極的に移動し、攻撃後の結果の評価値が高い対象を狙う
        WAITING,            // 自身の行動範囲に対象が入ってこない限り動かない。動き始めた後はAGGRESSIVEと同じ動作

        NUM
    }
}