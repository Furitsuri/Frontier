using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Entities
{
    // 第三勢力は形式上のものとして、中身は敵と同一にする
    // 仕様変更があれば処理を追加する
    public class Other : Enemy
    {
        override public void ToggleDisplayDangerRange()
        {
            _actionRangeCtrl.ToggleDisplayDangerRange( in TileColors.Colors[( int ) MeshType.OTHERS_ATTACKABLE] );
        }

        override public void SetDisplayDangerRange( bool isShow )
        {
            _actionRangeCtrl.SetDisplayDangerRange( isShow, in TileColors.Colors[( int ) MeshType.OTHERS_ATTACKABLE] );
        }
    }
}
