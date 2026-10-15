using Frontier.Stage;
using System;
using System.Collections.Generic;

namespace Frontier.Combat
{
    /// <summary>
    /// スキル固有の範囲計算ルールをまとめたものです。
    /// 範囲の到達判定・攻撃対象の絞り込み・着地先の決定・射程調整時の絞り込みを1か所で管理し、
    /// ActionRangeController 等でスキルIDによる分岐が散らばらないようにします。
    /// 各ルールがnullの場合は、それぞれのデフォルトの処理が適用されます。
    /// </summary>
    public sealed class SkillRangeRules
    {
        /// <summary>
        /// スキル固有のルールを持たない場合に用いる、全てデフォルトのルールです
        /// </summary>
        public static readonly SkillRangeRules Default = new SkillRangeRules();

        /// <summary>
        /// 範囲を直前のタイルから次のタイルへ伸ばせるかどうかの判定
        /// </summary>
        public readonly TileDataHandler.TileReachabilityPredicate Reachability;

        /// <summary>
        /// 範囲の計算後に行う、攻撃対象の絞り込み
        /// </summary>
        public readonly TileDataHandler.AttackableDataPostProcessor PostProcessor;

        /// <summary>
        /// 移動を伴うスキルの着地先(ゴースト)の決定
        /// </summary>
        public readonly TileDataHandler.GhostTileResolver GhostResolver;

        /// <summary>
        /// 射程の調整時に、着地先より遠いタイルを残すかどうかの判定
        /// </summary>
        public readonly TileDataHandler.RangeAdjustmentFilter RangeAdjustmentFilter;

        // スキルIDごとのルール生成処理
        // 高さ情報の参照など、StageController を必要とするルールがあるため生成時に受け取る
        private static readonly Dictionary<SkillID, Func<StageController, SkillRangeRules>> _factories = new Dictionary<SkillID, Func<StageController, SkillRangeRules>>
        {
            { SkillID.DASH_SLASH, DashSlashSA.CreateRangeRules },
            { SkillID.JUMP_SLASH, JumpSlashSA.CreateRangeRules },
        };

        public SkillRangeRules(
            TileDataHandler.TileReachabilityPredicate   reachability          = null,
            TileDataHandler.AttackableDataPostProcessor postProcessor         = null,
            TileDataHandler.GhostTileResolver           ghostResolver         = null,
            TileDataHandler.RangeAdjustmentFilter       rangeAdjustmentFilter = null )
        {
            Reachability          = reachability;
            PostProcessor         = postProcessor;
            GhostResolver         = ghostResolver;
            RangeAdjustmentFilter = rangeAdjustmentFilter;
        }

        /// <summary>
        /// 指定スキルの範囲計算ルールを取得します。スキル固有のルールが無い場合は Default を返します。
        /// </summary>
        public static SkillRangeRules Get( SkillID skillID, StageController stageCtrl )
        {
            return _factories.TryGetValue( skillID, out var factory ) ? factory( stageCtrl ) : Default;
        }
    }
}
