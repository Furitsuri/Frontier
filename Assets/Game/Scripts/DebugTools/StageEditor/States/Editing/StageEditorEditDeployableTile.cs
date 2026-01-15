using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.DebugTools.StageEditor
{
    public class StageEditorEditDeployableTile : StageEditorEditBase
    {
        /// <summary>
        /// タイルの配置可否情報を切り替えます
        /// </summary>
        /// <param name="isInput"></param>
        /// <returns></returns>
        public override bool AcceptConfirm( bool isInput )
        {
            if( isInput )
            {
                OwnCallback( _gridCursorCtrl.X(), _gridCursorCtrl.Y() );

                return true;
            }

            return false;
        }
    }
}