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
    }
