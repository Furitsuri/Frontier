using System;
using static Constants;
using static InputCode;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    public class StageEditorEditRowAndColumn : StageEditorEditBase
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
        override public bool CanAcceptSub1() { return TILE_ROW_MIN_NUM < _refParams.Row; }
        override public bool CanAcceptSub2() { return _refParams.Row < TILE_ROW_MAX_NUM; }
        override public bool CanAcceptSub3() { return TILE_COLUMN_MIN_NUM < _refParams.Col; }
        override public bool CanAcceptSub4() { return _refParams.Col < TILE_COLUMN_MAX_NUM; }

        override public bool AcceptConfirm( bool isInput )
        {
            if ( isInput )
            {
                ResizeTileGridCallback( _refParams.Col, _refParams.Row );

                return true;
            }

            return false;
        }

        override public bool AcceptCancel( bool isCancel ) { return false; }

        override public bool AcceptSub1( bool isInput )
        {
            if ( !isInput ) return false;

            _refParams.Col = Math.Clamp( _refParams.Col - 1, TILE_COLUMN_MIN_NUM, TILE_COLUMN_MAX_NUM );

            return true;
        }

        override public bool AcceptSub2( bool isInput )
        {
            if ( !isInput ) return false;

            _refParams.Col = Math.Clamp( _refParams.Col + 1, TILE_COLUMN_MIN_NUM, TILE_COLUMN_MAX_NUM );

            return true;
        }

        override public bool AcceptSub3( bool isInput )
        {
            if ( !isInput ) return false;

            _refParams.Row = Math.Clamp( _refParams.Row - 1, TILE_ROW_MIN_NUM, TILE_ROW_MAX_NUM );

            return true;
        }

        override public bool AcceptSub4( bool isInput )
        {
            if ( !isInput ) return false;

            _refParams.Row = Math.Clamp( _refParams.Row + 1, TILE_ROW_MIN_NUM, TILE_ROW_MAX_NUM );

            return true;
        }
    }
}
#endif // UNITY_EDITOR