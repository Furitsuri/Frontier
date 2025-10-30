using Frontier.Stage;
using Frontier.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.CombatPreparation
{
    public sealed class PlacementPhaseHandler : PhaseHandlerBase
    {
        [Inject] private InputFacade _inputFcd              = null;
        [Inject] private IUiSystem _uiSystem                = null;

        private int _currentMenuIndex = 0;
        private CombatPreparationPresenter _combatPreparationView = null;

        // Start is called before the first frame update
        void Start()
        {
            RegisterInputCodes();

            _stgCtrl.Init();
        }

        // Update is called once per frame
        void Update()
        {
            (var tileSData, var tileDData) = _stgCtrl.TileDataHdlr().GetCurrentTileDatas();

            _combatPreparationView.UpdateMenuCursor(_currentMenuIndex);
        }

        private void RegisterInputCodes()
        {
            _inputFcd.RegisterInputCodes(
               (GuideIcon.ALL_CURSOR, "MOVE", InputFacade.CanBeAcceptAlways, new AcceptDirectionInput( AcceptDirection ), GRID_DIRECTION_INPUT_INTERVAL, 0),
               (GuideIcon.CONFIRM, "CHANGE CHARACTER", InputFacade.CanBeAcceptAlways, new AcceptBooleanInput( AcceptConfirm ), 0.0f, 0),
               (GuideIcon.OPT2, "START COMBAT", InputFacade.CanBeAcceptAlways, new AcceptBooleanInput( AcceptOptional ), 0.0f, 0)
            );
        }


        private bool AcceptDirection( Direction dir )
        {
            return false;
        }

        /// <summary>
        /// 決定入力を受け取った際の処理を行います
        /// </summary>
        /// <param name="isInput">決定入力</param>
        /// <returns>入力実行の有無</returns>
        private bool AcceptConfirm( bool isInput )
        {
            if( isInput )
            {
                return true;
            }

            return false;
        }

        private bool AcceptOptional( bool isInput )
        {
            if( isInput )
            {
                return true;
            }

            return false;
        }
    }
}