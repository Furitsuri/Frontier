using Frontier.Combat.Skill;
using Frontier.Entities.Ai;

namespace Frontier.Entities
{
    public class PlayerBattleLogic : BattleLogicBase
    {
        public override void Init()
        {
            base.Init();

            LazyInject.GetOrCreate( ref _baseAi, () => _hierarchyBld.InstantiateWithDiContainer<AiBase>( false ) );

            _baseAi.Init( _readOnlyOwner.Value );
        }

        /// <summary>
        /// 指定のスキルの使用設定を切り替えます
        /// </summary>
        /// <param name="index">指定のスキルのインデックス番号</param>
        /// <returns>切替の有無</returns>
        public override bool ToggleUseSkillks( int index )
        {
            _readOnlyOwner.Value.RefBattleParams.TmpParam.isUseSkills[index] = !_readOnlyOwner.Value.RefBattleParams.TmpParam.isUseSkills[index];

            int skillID = ( int ) _readOnlyOwner.Value.GetStatusRef.equipSkills[index];
            var skillData = SkillsData.data[skillID];

            if( _readOnlyOwner.Value.RefBattleParams.TmpParam.isUseSkills[index] )
            {
                _readOnlyOwner.Value.GetStatusRef.consumptionActionGauge += skillData.Cost;
                _readOnlyOwner.Value.RefBattleParams.SkillModifiedParam.AtkNum += skillData.AddAtkNum;
                _readOnlyOwner.Value.RefBattleParams.SkillModifiedParam.AtkMagnification += skillData.AddAtkMag;
                _readOnlyOwner.Value.RefBattleParams.SkillModifiedParam.DefMagnification += skillData.AddDefMag;
            }
            else
            {
                _readOnlyOwner.Value.GetStatusRef.consumptionActionGauge -= skillData.Cost;
                _readOnlyOwner.Value.RefBattleParams.SkillModifiedParam.AtkNum -= skillData.AddAtkNum;
                _readOnlyOwner.Value.RefBattleParams.SkillModifiedParam.AtkMagnification -= skillData.AddAtkMag;
                _readOnlyOwner.Value.RefBattleParams.SkillModifiedParam.DefMagnification -= skillData.AddDefMag;
            }

            _uiSystem.BattleUi.GetPlayerParamSkillBox( index ).SetFlickEnabled( _readOnlyOwner.Value.RefBattleParams.TmpParam.isUseSkills[index] );

            return true;
        }
    }
}
