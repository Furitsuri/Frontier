using Frontier.Stage;
using System;
using UnityEngine;
using static Constants;
using static InputCode;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    public class StageEditorEditTileInformation : StageEditorEditBase
    {
        public override void Init( Action<int, int> callback )
        {
            base.Init( callback );
        }

        public override void Update()
        {
            base.Update();
        }

        public override bool CanAcceptConfirm() { return CanAcceptInputAlways(); }
        public override bool CanAcceptCancel() { return false; }
        public override bool CanAcceptSub1() { return false; }
        public override bool CanAcceptSub2() { return false; }
        public override bool CanAcceptSub3() { return 0f < _refParams.SelectedHeight; }
        public override bool CanAcceptSub4() { return _refParams.SelectedHeight < TILE_MAX_HEIGHT; }

        public override bool AcceptConfirm( bool isInput )
        {
            if ( isInput )
            {
                OwnCallback( _gridCursorCtrl.X(), _gridCursorCtrl.Y() );

                return true;
            }

            return false;
        }

        public override bool AcceptCancel( bool isCancel ) { return false; }

        /// <summary>
        /// タイルタイプの値をデクリメントします。
        /// 値が負になった場合は最大値-1とすることでループさせます。
        /// </summary>
        /// <param name="isInput">入力の有無</param>
        /// <returns>入力受付の有無</returns>
        public override bool AcceptSub1( bool isInput )
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
        public override bool AcceptSub2( bool isInput )
        {
            if ( !isInput ) return false;

            if ( ( int )TileType.NUM <= ++_refParams.SelectedType ) { _refParams.SelectedType = 0; }

            return true;
        }

        public override bool AcceptSub3( bool isInput )
        {
            if ( !isInput ) return false;

            _refParams.SelectedHeight = Mathf.Clamp( ( float )_refParams.SelectedHeight - 0.5f, 0.0f, TILE_MAX_HEIGHT );

            return true;
        }

        public override bool AcceptSub4( bool isInput )
        {
            if ( !isInput ) return false;

            _refParams.SelectedHeight = Mathf.Clamp( ( float )_refParams.SelectedHeight + 0.5f, 0.0f, TILE_MAX_HEIGHT );

            return true;
        }
    }
}
#endif //UNITY_EDITOR