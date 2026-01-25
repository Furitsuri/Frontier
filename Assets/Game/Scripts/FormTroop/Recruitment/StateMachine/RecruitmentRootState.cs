using static InputCode;
using static Constants;
using System.Collections.Generic;
using Frontier.Registries;
using Zenject;

namespace Frontier.FormTroop
{
    public sealed class RecruitmentRootState : RecruitmentPhaseStateBase
    {
        [Inject] private PrefabRegistry _prefabReg = null;

        private List<Mercenary> _mercenaries = new List<Mercenary>();

        public override void Init()
        {
            base.Init();

            SetupMercenaries();
        }

        public override bool Update()
        {
            // 基底の更新は行わない
            // if( base.Update() ) { return true; }

            return ( 0 <= TransitIndex );
        }

        public override void ExitState()
        {
            DeleteMercenaries();

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

        private void SetupMercenaries()
        {
            for( int i = 0; i < EMPLOYABLE_CHARACTERS_NUM; ++i )
            {
                Mercenary mercenary = null;
                LazyInject.GetOrCreate( ref mercenary, () => _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<Mercenary>( _prefabReg.PlayerPrefabs[0], true, false, typeof( Mercenary ).Name ) );

                mercenary.Setup( 1, _mercenaries );

                _mercenaries.Add( mercenary );
            }
        }

        private void DeleteMercenaries()
        {
            foreach( var unit in _mercenaries )
            {
                if( unit.Employed ) { continue; }

                unit.Dispose();
                _mercenaries.Remove( unit );
            }
        }
    }
}
