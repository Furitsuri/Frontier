#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    public enum EditorEnemyParamTag : int
    {
        PREFAB = 0,
        LEVEL,
        MAX_HP,
        ATK,
        DEF,
        MOVE_RANGE,
        JUMP_FORCE,
        ATK_RANGE,
        ACT_GAUGE_MAX,
        ACT_RECOVERY,
        THINK_TYPE,
        INIT_GRID_INDEX,
        INIT_DIR,
        SKILL_1,
        SKILL_2,
        SKILL_3,
        SKILL_4,

        NUM,
    }
}

#endif // UNITY_EDITOR
