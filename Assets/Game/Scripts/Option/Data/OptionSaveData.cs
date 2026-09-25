using System;

namespace Frontier.Option
{
    [Serializable]
    public class OptionSaveData
    {
        public float BgmVolume = 100f;
        public float SeVolume = 100f;
        public bool IsInputGuideVisible = true;
        public bool IsAttackCloseUpEnabled = false; // 攻撃時に戦闘フィールドへ遷移し、寄りのカメラワークで映すか(false: ステージ上でそのまま攻撃が完結する)
    }
}
