namespace Frontier.Stage
{
    public enum MoveDirectionType
    {
        ARROW_STRAIGHT = 0,     // 先端に矢印がある直線(移動目標地点直前のタイルで配置する)
        ARROW_TURN_LEFT,        // 先端に矢印がある左折(移動目標地点直前のタイルで配置する)
        ARROW_TURN_RIGHT,       // 先端に矢印がある右折(移動目標地点直前のタイルで配置する)
        ARROW_BODY,             // 矢印の胴体部分の直線(移動目標地点直前より前のタイルで配置する)
        ARROW_BODY_TURN_LEFT,   // 矢印の胴体部分で左折(移動目標地点直前より前のタイルで配置する)
        ARROW_BODY_TURN_RIGHT,  // 矢印の胴体部分で右折(移動目標地点直前より前のタイルで配置する)

        NUM
    }
}