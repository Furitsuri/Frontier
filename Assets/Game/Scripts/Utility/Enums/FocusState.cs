/// <summary>
/// IFocusRoutineにおける現在の状態を表す列挙型です
/// </summary>
public enum FocusState
{
    NONE = -1,

    RUN_SCHEDULED,
    RUN,
    PAUSE,
    EXIT_SCHEDULED,
    EXIT,
    MAX
}