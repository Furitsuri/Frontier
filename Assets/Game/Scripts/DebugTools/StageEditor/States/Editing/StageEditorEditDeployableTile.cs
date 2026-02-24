using System;
using static Constants;

namespace Frontier.DebugTools.StageEditor
{
    public class StageEditorEditDeployableTile : StageEditorEditBase
    {
        public override void Init( Action<EditActionContext> callback )
        {
            base.Init( callback );

            _context.ExtraIntValues.Clear();
            _context.ExtraIntValues.Add( _refParams.MaxDeployableUnits );
        }

        public override bool CanAcceptSub1()
        {
            return DEPLOYABLE_UNIT_MIN_NUM < _refParams.MaxDeployableUnits;
        }

        public override bool CanAcceptSub2()
        {
            return _refParams.MaxDeployableUnits < DEPLOYABLE_UNIT_MAX_NUM;
        }

        /// <summary>
        /// タイルの配置可否情報を切り替えます
        /// </summary>
        /// <param name="isInput"></param>
        /// <returns></returns>
        public override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            _context.X = _gridCursorCtrl.X();
            _context.Y = _gridCursorCtrl.Y();

            OwnCallback( _context );

            return true;
        }

        /// <summary>
        /// ステージに配置可能なユニット数を設定します
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public override bool AcceptSub1( InputContext context )
        {
            if( !base.AcceptSub1( context ) ) { return false; }

            _refParams.SetDeployableUnitsNum( _refParams.MaxDeployableUnits - 1 );
            _context.ExtraIntValues[0] = _refParams.MaxDeployableUnits;
            _context.X = _context.Y = -1;   // タイルの配置可否情報の切り替えではないため、座標は-1にしておく

            OwnCallback( _context );

            return true;
        }

        public override bool AcceptSub2( InputContext context )
        {
            if( !base.AcceptSub2( context ) ) { return false; }

            _refParams.SetDeployableUnitsNum( _refParams.MaxDeployableUnits + 1 );
            _context.ExtraIntValues[0] = _refParams.MaxDeployableUnits;
            _context.X = _context.Y = -1;   // タイルの配置可否情報の切り替えではないため、座標は-1にしておく

            OwnCallback( _context );

            return true;
        }
    }
}