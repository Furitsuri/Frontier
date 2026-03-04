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

            return !character.BattleParams.TmpParam.IsEndCommand( COMMAND_TAG.MOVE );
        }

        static public bool IsExecutableAttackCommand( Character character, StageController stageCtrl )
        {
            var tmpParam = character.BattleParams.TmpParam;
            if( !IsExecutableCommandBase( character ) ||        // 行動終了済みである場合は攻撃不可
                tmpParam.IsEndCommand( COMMAND_TAG.ATTACK ) ||  // 攻撃済みである場合は攻撃不可
                tmpParam.IsEndCommand( COMMAND_TAG.SKILL ) )    // スキル使用済みである場合は攻撃不可
            {
                return false;
            }

            // 現在グリッドから攻撃可能な対象の居るグリッドが存在すれば、実行可能
            int dprtTileIndex = character.BattleParams.TmpParam.CurrentTileIndex;
            character.BattleLogic.ActionRangeCtrl.SetupAttackableRangeData( dprtTileIndex );
            bool isExecutable = false;
            foreach( var data in character.BattleLogic.ActionRangeCtrl.ActionableTileMap.AttackableTileMap )
            {
                if( Methods.CheckBitFlag( data.Value.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST ) )
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
            if( !IsExecutableCommandBase( character ) ||        // 行動終了済みである場合は攻撃不可
                tmpParam.IsEndCommand( COMMAND_TAG.ATTACK ) ||  // 攻撃済みである場合は攻撃不可
                tmpParam.IsEndCommand( COMMAND_TAG.SKILL ) )    // スキル使用済みである場合は攻撃不可
            {
                return false;
            }

            bool isExecutable = false;
            for( int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                if( character.CanUseEquipSkill( i, Skill.SituationType.ATTACK ) )
                {
                    isExecutable = true;
                    break;
                }
            }

            return isExecutable;
        }

        static public bool IsExecutableWaitCommand( Character character, StageController stageCtrl )
        {
            return IsExecutableCommandBase( character );
        }
    }
}