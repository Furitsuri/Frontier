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

        public static bool IsExecutableCommandBase(Character character)
        {
            if (character.tmpParam.IsEndAction()) return false;

            return true;
        }

        public static bool IsExecutableMoveCommand(Character character, StageController stageCtrl)
        {
            if (!IsExecutableCommandBase(character)) return false;

            return !character.tmpParam.IsEndCommand(COMMAND_TAG.MOVE);
        }

        public static bool IsExecutableAttackCommand(Character character, StageController stageCtrl)
        {
            if (!IsExecutableCommandBase(character)) return false;

            if (character.tmpParam.IsEndCommand(COMMAND_TAG.ATTACK)) return false;

            // 現在グリッドから攻撃可能な対象の居るグリッドが存在すれば、実行可能
            bool isExecutable = stageCtrl.RegistAttackAbleInfo(character.tmpParam.GetCurrentGridIndex(), character.characterParam.attackRange, character.characterParam.characterTag);

            // 実行不可である場合は登録した攻撃情報を全てクリア
            if (!isExecutable)
            {
                stageCtrl.ClearAttackableInfo();
            }

            return isExecutable;
        }

        public static bool IsExecutableWaitCommand(Character character, StageController stageCtrl)
        {
            return IsExecutableCommandBase(character);
        }
    }
}