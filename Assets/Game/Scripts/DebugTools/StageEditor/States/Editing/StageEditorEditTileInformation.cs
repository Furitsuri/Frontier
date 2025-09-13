using Frontier.Stage;
using System;
using UnityEngine;
using static Constants;
using static Frontier.DebugTools.StageEditor.StageEditorController;
using static InputCode;

namespace Frontier.DebugTools
{
    public class StageEditorEditTileInformation : StageEditorEditBase
    {
        override public void Init( Action<int, int> placeTileCb, Action<int, int> resizeTileGridCb )
        {
            base.Init( placeTileCb, resizeTileGridCb );
        }

        override public void Update()
        {
            base.Update();
        }

        override public bool CanAcceptConfirm() { return CanAcceptInputAlways(); }
        override public bool CanAcceptCancel() { return false; }
        override public bool CanAcceptSub1() { return false; }
        override public bool CanAcceptSub2() { return false; }
        override public bool CanAcceptSub3() { return 0f < _refParams.SelectedHeight; }
        override public bool CanAcceptSub4() { return _refParams.SelectedHeight < TILE_MAX_HEIGHT; }

        override public bool AcceptConfirm( bool isInput )
        {
            if ( isInput )
            {
                PlaceTileCallback( _gridCursorCtrl.X(), _gridCursorCtrl.Y() );

                return true;
            }

            return false;
        }

        override public bool AcceptCancel( bool isCancel ) { return false; }

        /// <summary>
        /// タイルタイプの値をデクリメントします。
        /// 値が負になった場合は最大値-1とすることでループさせます。
        /// </summary>
        /// <param name="isInput">入力の有無</param>
        /// <returns>入力受付の有無</returns>
        override public bool AcceptSub1( bool isInput )
        {
            if ( !isInput ) return false;

            if ( --_refParams.SelectedType < 0 ) { _refParams.SelectedType = ( int )TileType.NUM - 1; }

            return true;
        }

        /// <summary>
        /// タイルタイプの値をインクリメントします。
        /// 値が最大値を超えた場合は0とすることでループさせます。
        /// </summary>
        /// <param name="isInput">入力の有無</param>
        /// <returns>入力受付の有無</returns>
        override public bool AcceptSub2( bool isInput )
        {
            if ( !isInput ) return false;

            if ( ( int )TileType.NUM <= ++_refParams.SelectedType ) { _refParams.SelectedType = 0; }

            return true;
        }

        override public bool AcceptSub3( bool isInput )
        {
            if ( !isInput ) return false;

            _refParams.SelectedHeight = Mathf.Clamp( ( float )_refParams.SelectedHeight - 0.5f, 0.0f, TILE_MAX_HEIGHT );

            return true;
        }

        override public bool AcceptSub4( bool isInput )
        {
            if ( !isInput ) return false;

            _refParams.SelectedHeight = Mathf.Clamp( ( float )_refParams.SelectedHeight + 0.5f, 0.0f, TILE_MAX_HEIGHT );

            return true;
        }
    }
}