using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier.CombatPreparation
{
    public sealed class CombatPreparationHandler : MonoBehaviour
    {
        [Inject] HierarchyBuilderBase _hierarchyBld = null;
        [Inject] IUiSystem _uiSystem                = null;

        private int _currentMenuIndex = 0;
        private CombatPreparationPresenter _combatPreparationView = null;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            _combatPreparationView.UpdateMenuCursor(_currentMenuIndex);
        }
    }
}