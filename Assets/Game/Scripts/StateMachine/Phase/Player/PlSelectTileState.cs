using Frontier.Entities;
using static Constants;

namespace Frontier
{
    public class PlSelectTileState : PlPhaseStateBase
    {
        /// <summary>
        /// 遷移先を示すタグ
        /// </summary>
        enum TransitTag
        {
            CharacterCommand = 0,
            TurnEnd,
        }

        override public void Init()
        {
            base.Init();

            // グリッド選択を有効化
            _stageCtrl.SetGridCursorControllerActive(true);
        }

        override public bool Update()
        {
            // グリッド選択より遷移が戻ることはないため基底の更新は行わない
            // if( base.Update() ) { return true; }

            // 全てのキャラクターが待機済みになっていれば終了
            if (_btlRtnCtrl.BtlCharaCdr.IsEndAllArmyWaitCommand(CHARACTER_TAG.PLAYER))
            {
                Back();

                return true;
            }

            Stage.TileInformation info;
            _stageCtrl.TileInfoDataHdlr().FetchCurrentTileInfo(out info);

            // 現在の選択グリッド上に未行動のプレイヤーが存在する場合は行動選択へ
            int selectCharaIndex = info.charaIndex;

            return (0 <= TransitIndex);
        }

        /// <summary>
        /// 入力コードを登録します
        /// </summary>
        override public void RegisterInputCodes()
        {
            int hashCode = GetInputCodeHash();

            _inputFcd.RegisterInputCodes(
               (GuideIcon.ALL_CURSOR,   "MOVE",     CanAcceptDefault, new AcceptDirectionInput(AcceptDirection), GRID_DIRECTION_INPUT_INTERVAL, hashCode),
               (GuideIcon.CONFIRM,      "COMMAND",  CanAcceptConfirm, new AcceptBooleanInput(AcceptConfirm), 0.0f, hashCode),
               (GuideIcon.OPT2,         "TURN END", CanAcceptDefault, new AcceptBooleanInput(AcceptOptional), 0.0f, hashCode)
            );
        }

        /// <summary>
        /// キャラクターコマンドへ遷移可能かを判定します
        /// </summary>
        /// <returns>コマンド選択が可能か</returns>
        override protected bool CanAcceptConfirm()
        {
            if (0 <= TransitIndex)
            {
                return false;
            }

            Character character = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();
            if (character != null &&
                 character.Params.CharacterParam.characterTag == CHARACTER_TAG.PLAYER &&
                 !character.Params.TmpParam.IsEndAction())
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 方向入力を受け取った際の処理を行います
        /// </summary>
        /// <param name="dir">方向入力</param>
        /// <returns>入力実行の有無</returns>
        override protected bool AcceptDirection(Direction dir)
        {
            return _stageCtrl.OperateGridCursorController(dir);
        }

        /// <summary>
        /// 決定入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isConfirm">決定入力</param>
        /// <returns>入力実行の有無</returns>
        override protected bool AcceptConfirm(bool isInput)
        {
            if (!isInput) return false;

            TransitIndex = (int)TransitTag.CharacterCommand;
            // コマンドを開くことをチュートリアルへ通知
            TutorialFacade.Notify( TutorialFacade.TriggerType.OpenBattleCommand);

            return true;
        }

        /// <summary>
        /// オプション入力を受けた際の処理を行います
        /// </summary>
        /// <param name="isOptional">オプション入力</param>
        /// <returns>入力実行の有無</returns>
        override protected bool AcceptOptional(bool isOptional)
        {
            if (!isOptional) return false;

            TransitIndex = (int)TransitTag.TurnEnd;

            return true;
        }
    }
}