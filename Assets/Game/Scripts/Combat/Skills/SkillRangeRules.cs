using Frontier.Entities;
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

        /// <summary>
        /// 着地先の決定後に、ターゲット可能範囲内のタイルを攻撃対象として残すかどうかの判定
        /// </summary>
        public readonly TileDataHandler.AttackTargetFilter AttackTargetFilter;

        /// <summary>
        /// 出発タイルから求める、スキル固有の最大レンジ(移動を伴うスキルでは着地可能な最大距離)。
        /// nullの場合はスキルデータの RangeValue が用いられます。
        /// </summary>
        public readonly Func<int, int> MaxRangeResolver;

        /// <summary>
        /// 最大レンジより先に、攻撃対象として含めるタイル数。
        /// 着地タイルの更に先の相手を攻撃するスキル等で、着地可能な距離と攻撃が届く距離を区別するために用います。
        /// </summary>
        public readonly int TargetableRangeMargin;

        // スキルIDごとのルール生成処理
        // 高さ情報やジャンプ力の参照など、StageController・使用者を必要とするルールがあるため生成時に受け取る
        private static readonly Dictionary<SkillID, Func<StageController, Character, SkillRangeRules>> _factories = new Dictionary<SkillID, Func<StageController, Character, SkillRangeRules>>
        {
            { SkillID.DASH_SLASH, DashSlashSA.CreateRangeRules },
            { SkillID.JUMP_SLASH, JumpSlashSA.CreateRangeRules },
        };

        public SkillRangeRules(
            TileDataHandler.TileReachabilityPredicate   reachability          = null,
            TileDataHandler.AttackableDataPostProcessor postProcessor         = null,
            TileDataHandler.GhostTileResolver           ghostResolver         = null,
            TileDataHandler.RangeAdjustmentFilter       rangeAdjustmentFilter = null,
            TileDataHandler.AttackTargetFilter          attackTargetFilter    = null,
            Func<int, int>                              maxRangeResolver      = null,
            int                                         targetableRangeMargin = 0 )
        {
            Reachability          = reachability;
            PostProcessor         = postProcessor;
            GhostResolver         = ghostResolver;
            RangeAdjustmentFilter = rangeAdjustmentFilter;
            AttackTargetFilter    = attackTargetFilter;
            MaxRangeResolver      = maxRangeResolver;
            TargetableRangeMargin = targetableRangeMargin;
        }

        /// <summary>
        /// 指定スキルの範囲計算ルールを取得します。スキル固有のルールが無い場合は Default を返します。
        /// </summary>
        /// <param name="owner">スキルの使用者</param>
        public static SkillRangeRules Get( SkillID skillID, StageController stageCtrl, Character owner )
        {
            return _factories.TryGetValue( skillID, out var factory ) ? factory( stageCtrl, owner ) : Default;
        }
    }
}
