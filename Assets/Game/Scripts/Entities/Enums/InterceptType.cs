namespace Frontier.Entities
{
    /// <summary>
    /// 思考タイプがINTERCEPTである場合の、待ち構え方のタイプ
    /// </summary>
    public enum InterceptType
    {
        SOLO    = 0,    // 単独型。自身の行動範囲(攻撃が届く範囲)に攻撃対象が入ってきた場合に行動を開始する
        GROUP,          // 集団型。同じ集団IDを持つ構成員のうち、誰かの行動範囲に攻撃対象が入ってきた場合に、構成員の全てが行動を開始する

        NUM
    }
}
