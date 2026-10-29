using Frontier.Battle;
using Frontier.Combat;
using Frontier.Entities;
using Frontier.Stage;

public class TargetingRangeContext
    {
        public BattleRoutineController BtlRtnCtrl;
        public BattleRoutinePresenter Presenter;
        public Character Owner;
        public StageController  StageCtrl;
        /// <summary>
        /// 使用するスキル固有の範囲計算ルール(着地先の決定、射程調整時の絞り込み等)。
        /// スキル固有のルールを持たない場合は SkillRangeRules.Default を設定してください。
        /// </summary>
        public SkillRangeRules Rules = SkillRangeRules.Default;
    }
