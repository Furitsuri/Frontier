namespace Frontier.Combat
{
    public enum COMMAND_TAG : int
    {
        NONE = -1,

        MOVE = 0,
        ATTACK,
        SKILL,
        QUEUED_SKILL,
        WAIT,

        NUM,
    }
}