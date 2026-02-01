using Frontier.Entities;
using Frontier.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static Constants;

namespace Frontier.UI
{
    public sealed class EmploymentSelectionUI : CharacterSelectionUI
    {
        private EmploymentSelectionDisplay[] _employmentSelectionDisplays = new EmploymentSelectionDisplay[SHOWABLE_SELECTION_CHARACTERS_NUM];

        public override void Setup()
        {
            base.Setup();

            for( int i = 0; i < SHOWABLE_SELECTION_CHARACTERS_NUM; ++i )
            {
                _employmentSelectionDisplays[i] = _characterSelectionDisplays[i] as EmploymentSelectionDisplay;
            }
        }

        public override void AssignSelectCandidates( ref CharacterCandidate[] selectCandidates )
        {
            for( int i = 0; i < SHOWABLE_SELECTION_CHARACTERS_NUM; ++i )
            {
                if( selectCandidates[i] == null )
                {
                    _employmentSelectionDisplays[i].gameObject.SetActive( false );
                    continue;
                }

                _employmentSelectionDisplays[i].gameObject.SetActive( true );
                _employmentSelectionDisplays[i].AssignSelectCandidate( ref selectCandidates[i] );

                // 先頭と末尾以外はコスト表示を有効化
                _employmentSelectionDisplays[i].SetActiveCostObject( !( i == 0 || i == SHOWABLE_SELECTION_CHARACTERS_NUM - 1 ) );
            }
        }
    }
}