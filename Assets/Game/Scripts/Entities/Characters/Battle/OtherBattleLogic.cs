using Frontier.Combat.Skill;
using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Entities
{
    public class OtherBattleLogic : BattleLogicBase
    {
        public override void ToggleDisplayDangerRange()
        {
            _actionRangeCtrl.ToggleDisplayDangerRange( in TileColors.Colors[( int ) MeshType.OTHERS_ATTACKABLE] );
        }

        public override void SetDisplayDangerRange( bool isShow )
        {
            _actionRangeCtrl.SetDisplayDangerRange( isShow, in TileColors.Colors[( int ) MeshType.OTHERS_ATTACKABLE] );
        }

        /// <summary>
        /// 使用スキルを選択します
        /// </summary>
        /// <param name="type">攻撃、防御、常駐などのスキルタイプ</param>
        public override void SelectUseSkills( SituationType type )
        {

        }
    }
}