namespace Frontier.Combat.Skill
{
    /// <summary>
    /// パリィ判定の種類
    /// </summary>
    public enum JudgeResult
    {
        NONE = -1,
        SUCCESS,    // 成功
        FAILED,     // 失敗
        JUST,       // ジャスト成功

        MAX,
    }
}