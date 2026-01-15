using static InputCode;
using static Constants;

namespace Frontier.FormTroop
{
    public sealed class RecruitmentRootState : RecruitmentPhaseStateBase
    {
        public override void Init()
        {
            base.Init();
        }

        public override bool Update()
        {
            // 基底の更新は行わない
            // if( base.Update() ) { return true; }

            return ( 0 <= TransitIndex );
        }

        public override void ExitState()
        {
            base.ExitState();
        }

        public override void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (GuideIcon.ALL_CURSOR,   "SELECT UNIT",  CanAcceptDefault, new AcceptDirectionInput( AcceptDirection ), GRID_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.CONFIRM,      "RECRUIT UNIT", CanAcceptConfirm, new AcceptBooleanInput( AcceptConfirm ), 0.0f, hashCode),
               (GuideIcon.TOOL,         "HIRED UNIT",   CanAcceptTool, new AcceptBooleanInput( AcceptTool ), 0.0f, hashCode),
               (GuideIcon.INFO,         "STATUS",       CanAcceptInfo, new AcceptBooleanInput( AcceptInfo ), 0.0f, hashCode),
               (GuideIcon.OPT2,         "COMPLETE",     CanAcceptOptional, new AcceptBooleanInput( AcceptOptional ), 0.0f, hashCode)
            );
        }
    }
}
