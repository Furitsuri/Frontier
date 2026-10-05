using Frontier.Entities;
using Frontier.Stage;

namespace Frontier.Combat
{
    /// <summary>
    /// キャラクターが使用可能なコマンドの管理クラスです
    /// </summary>
    public class Command
    {
        static public bool IsExecutableCommandBase( Character character )
        {
            if( character.BattleParams.TmpParam.IsEndAction() ) { return false; }

            return true;
        }

        static public bool IsExecutableMoveCommand( Character character, StageController stageCtrl )
        {
            if( !IsExecutableCommandBase( character ) ) { return false; }

            return !character.BattleParams.TmpParam.IsEndCommand[( int ) COMMAND_TAG.MOVE];
        }

        /// <summary>
        /// コマンドメニューで移動コマンドを選択出来るかを判定します。
        /// 未移動の場合(通常の移動)に加え、暫定的に移動している場合も、移動先の変更(最初に移動を開始した地点を起点とした移動のやり直し)として選択出来ます。
        /// グループ移動への登録可否など「新たに移動を開始出来るか」の判定にはIsExecutableMoveCommandを使用してください
        /// (暫定的に移動しているキャラクターはグループ移動に登録出来ません)。
        /// </summary>
        static public bool IsSelectableMoveCommand( Character character, StageController stageCtrl )
        {
            if( IsExecutableMoveCommand( character, stageCtrl ) ) { return true; }

            return IsExecutableCommandBase( character ) && ( character is Player player ) && player.IsProvisionallyMoved();
        }

        static public bool IsExecutableAttackCommand( Character character, StageController stageCtrl )
        {
            var tmpParam = character.BattleParams.TmpParam;
            if( !IsExecutableCommandBase( character ) ||                // 行動終了済みである場合は攻撃不可
                tmpParam.IsEndCommand[ ( int ) COMMAND_TAG.ATTACK ] ||  // 攻撃済みである場合は攻撃不可
                tmpParam.IsEndCommand[( int ) COMMAND_TAG.SKILL] )      // スキル使用済みである場合は攻撃不可
            {
                return false;
            }

            // 現在グリッドから攻撃可能な対象の居るグリッドが存在すれば、実行可能
            int dprtTileIndex = character.BattleParams.TmpParam.CurrentTileIndex;
            character.BattleLogic.ActionRangeCtrl.SetupAttackableRangeData( dprtTileIndex );
            bool isExecutable = false;
            foreach( var data in character.BattleLogic.ActionRangeCtrl.ActionableTileData.AttackableTileMap )
            {
                if( Methods.HasAnyFlag( data.Value.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST ) )
                {
                    isExecutable = true;
                    break;
                }
            }

            // 実行不可である場合は登録した攻撃情報を全てクリア
            if( !isExecutable ) { stageCtrl.TileDataHdlr().ClearAttackableInformation(); }

            return isExecutable;
        }

        static public bool IsExecutableSkillCommand( Character character, StageController stageCtrl )
        {
            var tmpParam = character.BattleParams.TmpParam;
            if( !IsExecutableCommandBase( character ) ||                // 行動終了済みである場合は攻撃不可
                tmpParam.IsEndCommand[( int ) COMMAND_TAG.ATTACK] ||    // 攻撃済みである場合は攻撃不可
                tmpParam.IsEndCommand[( int ) COMMAND_TAG.SKILL] )      // スキル使用済みである場合は攻撃不可
            {
                return false;
            }

            return true;
        }

        static public bool IsExecutableWaitCommand( Character character, StageController stageCtrl )
        {
            return IsExecutableCommandBase( character );
        }
    }
}