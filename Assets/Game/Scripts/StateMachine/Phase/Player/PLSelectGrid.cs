using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class PLSelectGrid : PhaseStateBase
    {
        override public void Init()
        {
            base.Init();

            // グリッド選択を有効化
            Stage.StageController.Instance.SetGridCursorActive(true);
        }

        override public bool Update()
        {
            // グリッド選択より遷移が戻ることはないため基底の更新は行わない
            // if( base.Update() ) { return true; }

            // 全てのキャラクターが待機済みになっていれば終了
            if (_btlMgr.IsEndAllCharacterWaitCommand())
            {
                Back();

                return true;
            }

            // ターン終了確認へ遷移
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                TransitIndex = 1;
                return true;
            }

            // グリッドの操作
            Stage.StageController.Instance.OperateGridCursor();
            Stage.GridInfo info;
            Stage.StageController.Instance.FetchCurrentGridInfo(out info);

            // 現在の選択グリッド上に未行動のプレイヤーが存在する場合は行動選択へ
            int selectCharaIndex = info.charaIndex;

            Character character = _btlMgr.GetSelectCharacter();
            if (character != null &&
                 character.param.characterTag == Character.CHARACTER_TAG.PLAYER &&
                 !character.tmpParam.isEndCommand[(int)Character.Command.COMMAND_TAG.WAIT])
            {
                if (Input.GetKeyUp(KeyCode.Space))
                {
                    TransitIndex = 0;   // 遷移

                    return true;
                }
            }

            return false;
        }

        override public void Exit()
        {
            // グリッド選択を無効化 → 無効化しないほうが見た目がよかったため、コメントアウト
            // Stage.StageController.Instance.SetGridCursorActive( false );

            base.Exit();
        }
    }
}