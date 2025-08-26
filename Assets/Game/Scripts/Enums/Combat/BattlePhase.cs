namespace Frontier.Battle
{
    /// <summary>
    /// バトル状態の遷移
    /// </summary>
    enum BattlePhase
    {
        BATTLE_START = 0,
        BATTLE_PLAYER_COMMAND,
        BATTLE_RESULT,
        BATTLE_END,
    }
}