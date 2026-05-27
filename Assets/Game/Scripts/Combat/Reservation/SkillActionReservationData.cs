using Frontier.Combat;
using Frontier.Entities;
using System.Collections.Generic;

namespace Frontier.Battle
{
    /// <summary>
    /// 予約されたスキルアクションの復元に必要なデータをすべて保持します。
    /// 生成後は不変であり、キューから取り出した際に PlSkillActionToTargetState の選択前状態を完全に再構築できます。
    /// </summary>
    public class SkillActionReservationData
    {
        /// <summary>攻撃者の識別キー</summary>
        public CharacterKey AttackerKey { get; }

        /// <summary>予約時点での攻撃者のタイルインデックス</summary>
        public int AttackerTileIndex { get; }

        /// <summary>予約時点でのスキル切替フラグ（コピー）</summary>
        public bool[] AttackerSkillsToggledON { get; }

        /// <summary>予約時点でのアクションゲージ消費量</summary>
        public int ActGaugeConsumption { get; }

        // --- スキル設定 ---

        /// <summary>使用スキルID</summary>
        public SkillID UseSkillID { get; }

        /// <summary>ターゲット選択モード</summary>
        public TargetingMode TargetingMode { get; }

        /// <summary>予約時点の選択射程</summary>
        public int CurrentRange { get; }

        /// <summary>スキルの最大射程</summary>
        public int MaxRange { get; }

        /// <summary>射程が調整可能か</summary>
        public bool IsAdjustableRange { get; }

        /// <summary>移動を伴うスキルか</summary>
        public bool IsMovingSkill { get; }

        // --- ターゲット情報 ---

        /// <summary>攻撃対象キャラクターキーのリスト（コピー）</summary>
        public IReadOnlyList<CharacterKey> AttackTargetCharaKeys { get; }

        /// <summary>フォーカス中のターゲットキー（対象なしの場合 CharacterKey.Invalid）</summary>
        public CharacterKey FocusedTargetCharaKey { get; }

        // --- 予測ダメージ ---

        /// <summary>攻撃者への単発予測 HP 変動量</summary>
        public int AttackerExpectedHpChange { get; }

        /// <summary>攻撃者への複数回攻撃を考慮した予測 HP 総変動量</summary>
        public int AttackerTotalExpectedHpChange { get; }

        /// <summary>ターゲットへの単発予測 HP 変動量</summary>
        public int TargetExpectedHpChange { get; }

        /// <summary>ターゲットへの複数回攻撃を考慮した予測 HP 総変動量</summary>
        public int TargetTotalExpectedHpChange { get; }

        public SkillActionReservationData(
            CharacterKey            attackerKey,
            int                     attackerTileIndex,
            bool[]                  attackerSkillsToggledON,
            int                     actGaugeConsumption,
            SkillID                 useSkillID,
            TargetingMode           targetingMode,
            int                     currentRange,
            int                     maxRange,
            bool                    isAdjustableRange,
            bool                    isMovingSkill,
            List<CharacterKey>      attackTargetCharaKeys,
            CharacterKey            focusedTargetCharaKey,
            int                     attackerExpectedHpChange,
            int                     attackerTotalExpectedHpChange,
            int                     targetExpectedHpChange,
            int                     targetTotalExpectedHpChange )
        {
            AttackerKey                   = attackerKey;
            AttackerTileIndex             = attackerTileIndex;
            AttackerSkillsToggledON       = ( bool[] ) attackerSkillsToggledON.Clone();
            ActGaugeConsumption           = actGaugeConsumption;
            UseSkillID                    = useSkillID;
            TargetingMode                 = targetingMode;
            CurrentRange                  = currentRange;
            MaxRange                      = maxRange;
            IsAdjustableRange             = isAdjustableRange;
            IsMovingSkill                 = isMovingSkill;
            AttackTargetCharaKeys         = new List<CharacterKey>( attackTargetCharaKeys );
            FocusedTargetCharaKey         = focusedTargetCharaKey;
            AttackerExpectedHpChange      = attackerExpectedHpChange;
            AttackerTotalExpectedHpChange = attackerTotalExpectedHpChange;
            TargetExpectedHpChange        = targetExpectedHpChange;
            TargetTotalExpectedHpChange   = targetTotalExpectedHpChange;
        }
    }
}
