using Frontier.Entities;
using Frontier.Stage;

namespace Frontier.Combat
{
    /// <summary>
    /// キャラクターが使用可能なコマンドの管理クラスです
    /// </summary>
    public class Command
    {
        public enum COMMAND_TAG
        {
            MOVE = 0,
            ATTACK,
            WAIT,

            NUM,
        }

        static public bool IsExecutableCommandBase(Character character)
        {
            if( character.Params.TmpParam.IsEndAction() ) { return false; }

            return true;
        }

        static public bool IsExecutableMoveCommand(Character character, StageController stageCtrl)
        {
            if( !IsExecutableCommandBase( character ) ) { return false; }

            return !character.Params.TmpParam.IsEndCommand( COMMAND_TAG.MOVE );
        }

        static public bool IsExecutableAttackCommand( Character character, StageController stageCtrl )
        {
            var charaParam  = character.Params.CharacterParam;
            var tmpParam    = character.Params.TmpParam;
            if( !IsExecutableCommandBase( character ) ||       // 行動終了済みである場合は攻撃不可
                tmpParam.IsEndCommand( COMMAND_TAG.ATTACK ) )  // 攻撃済みである場合は攻撃不可
            {
                return false;
            }

            // 現在グリッドから攻撃可能な対象の居るグリッドが存在すれば、実行可能
            stageCtrl.TileInfoDataHdlr().BeginRegisterAttackableTiles( tmpParam.GetCurrentGridIndex(), charaParam.attackRange, charaParam.characterTag, true );
            bool isExecutable = stageCtrl.TileInfoDataHdlr().CorrectAttackableTileIndexs( charaParam.characterTag );

			// 実行不可である場合は登録した攻撃情報を全てクリア
			if( !isExecutable ) { stageCtrl.TileInfoDataHdlr().ClearAttackableInformation(); }

            return isExecutable;
        }

        static public bool IsExecutableWaitCommand(Character character, StageController stageCtrl)
        {
            return IsExecutableCommandBase(character);
        }
    }
}