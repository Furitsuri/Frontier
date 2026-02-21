using System;
using static Constants;
using static InputCode;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    public class StageEditorEditRowAndColumn : StageEditorEditBase
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
        public override bool CanAcceptSub1() { return TILE_ROW_MIN_NUM < _refParams.Row; }
        public override bool CanAcceptSub2() { return _refParams.Row < TILE_ROW_MAX_NUM; }
        public override bool CanAcceptSub3() { return TILE_COLUMN_MIN_NUM < _refParams.Col; }
        public override bool CanAcceptSub4() { return _refParams.Col < TILE_COLUMN_MAX_NUM; }

        public override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            OwnCallback( _refParams.Col, _refParams.Row );

            return true;
        }

        public override bool AcceptCancel( InputContext context ) { return false; }

        public override bool AcceptSub1( InputContext context )
        {
            if( !base.AcceptSub1( context ) ) { return false; }

            _refParams.Col = Math.Clamp( _refParams.Col - 1, TILE_COLUMN_MIN_NUM, TILE_COLUMN_MAX_NUM );

            return true;
        }

        public override bool AcceptSub2( InputContext context )
        {
            if( !base.AcceptSub2( context ) ) { return false; }

            _refParams.Col = Math.Clamp( _refParams.Col + 1, TILE_COLUMN_MIN_NUM, TILE_COLUMN_MAX_NUM );

            return true;
        }

        public override bool AcceptSub3( InputContext context )
        {
            if( !base.AcceptSub3( context ) ) { return false; }

            _refParams.Row = Math.Clamp( _refParams.Row - 1, TILE_ROW_MIN_NUM, TILE_ROW_MAX_NUM );

            return true;
        }

        public override bool AcceptSub4( InputContext context )
        {
            if( !base.AcceptSub4( context ) ) { return false; }

            _refParams.Row = Math.Clamp( _refParams.Row + 1, TILE_ROW_MIN_NUM, TILE_ROW_MAX_NUM );

            return true;
        }
    }
}
#endif // UNITY_EDITOR