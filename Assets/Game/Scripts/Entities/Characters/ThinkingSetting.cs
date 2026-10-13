namespace Frontier.Entities
{
    /// <summary>
    /// NPCの思考(AI)に関する設定です。
    /// 配置データを読み込んだ時点ではAIがまだ生成されていないため、AIの生成時に渡すまでの間、Npcが保持します。
    /// 思考タイプ毎の設定値そのものは、生成された各AIが自身のフィールドとして保持します
    /// </summary>
    public struct ThinkingSetting
    {
        public ThinkingType Type;               // 思考タイプ
        public InterceptType InterceptType;     // 思考タイプがINTERCEPTである場合の待ち構え方
        public int InterceptGroupId;            // 待ち構え方が集団型である場合の集団ID
    }
}
