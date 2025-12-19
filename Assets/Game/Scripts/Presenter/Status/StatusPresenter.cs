using Frontier.Entities;
using Frontier.UI;
using UnityEngine;
using Zenject;
using Zenject.SpaceFighter;

namespace Frontier.Presenter
{
    public class StatusPresenter
    {
        [Inject] private IUiSystem _uiSystem = null;

        private StatusUI _statusUi              = null;
        private TooltipUI _tooltipUi            = null;

        public void Init()
        {
            _statusUi   = _uiSystem.GeneralUi.CharacterStatusView;
            _tooltipUi  = _uiSystem.GeneralUi.ToolTipView;

            _statusUi.gameObject.SetActive( false );
            _tooltipUi.gameObject.SetActive( false );
        }

        public void OpenCharacterStatus( Character chara )
        {
            _statusUi.gameObject.SetActive( true );
            _statusUi.AssignCharacter( chara );
        }

        public void CloseCharacterStatus()
        {
            _tooltipUi.gameObject.SetActive ( false );
            _statusUi.gameObject.SetActive( false );
        }

        public void ToggleToolTipActive()
        {
            _tooltipUi.gameObject.SetActive( !_tooltipUi.gameObject.activeSelf );
        }

        public bool IsToolTipActive()
        {
            return _tooltipUi.gameObject.activeSelf;
        }
    }
}