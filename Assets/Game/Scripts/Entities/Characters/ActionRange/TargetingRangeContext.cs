using Frontier.Battle;
using Frontier.Entities;
using Frontier.Stage;

public class TargetingRangeContext
    {
        public BattleRoutineController BtlRtnCtrl;
        public BattleRoutinePresenter Presenter;
        public Character Owner;
        public StageController  StageCtrl;
        /// <summary>
        /// 移動を伴うスキルのゴースト位置を決定するリゾルバ。nullの場合はデフォルト（最遠の空きタイル）を使用します。
        /// </summary>
        public TileDataHandler.GhostTileResolver GhostResolver;

        /// <summary>
        /// ゴースト距離より遠いタイルの保持・除外を判定するスキル固有フィルタ。
        /// nullの場合はデフォルト（ゴースト距離より遠いタイルをすべて除外）を使用します。
        /// </summary>
        public TileDataHandler.RangeAdjustmentFilter RangeAdjustmentFilter;

        /// <summary>
        /// 着地先(ゴースト)の決定後、ターゲット可能範囲内のタイルを攻撃対象として残すかを判定するスキル固有フィルタ。
        /// nullの場合は、ターゲット可能範囲に残った攻撃対象をすべてそのまま残します。
        /// </summary>
        public TileDataHandler.AttackTargetFilter AttackTargetFilter;

        /// <summary>
        /// 移動を伴うスキルについて、現在のレンジより先に攻撃対象として含めるタイル数。
        /// 着地タイルの更に先の相手を攻撃するスキルで用います。
        /// </summary>
        public int TargetableRangeMargin;
    }
