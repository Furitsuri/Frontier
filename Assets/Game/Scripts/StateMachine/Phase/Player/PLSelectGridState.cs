using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class PLSelectGridState : PhaseStateBase
    {
        override public void Init(BattleManager btlMgr, StageController stgCtrl)
        {
            base.Init(btlMgr, stgCtrl);

            // グリッド選択を有効化
            _stageCtrl.SetGridCursorActive(true);

            // キーガイドを登録
            TransitKeyGuides(
                (Constants.KeyIcon.ALL_CURSOR,  "Move"),
                (Constants.KeyIcon.ESCAPE,      "TURN END"));
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

            /*
            // キーガイドを登録
            TransitKeyGuides(
                (Constants.KeyIcon.ALL_CURSOR, "Move"),
                (Constants.KeyIcon.ESCAPE, "TURN END"));
            */

            // グリッドの操作
            _stageCtrl.OperateGridCursor();
            Stage.GridInfo info;
            _stageCtrl.FetchCurrentGridInfo(out info);

            // 現在の選択グリッド上に未行動のプレイヤーが存在する場合は行動選択へ
            int selectCharaIndex = info.charaIndex;

            Character character = _btlMgr.GetSelectCharacter();
            if (character != null &&
                 character.param.characterTag == Character.CHARACTER_TAG.PLAYER &&
                 !character.tmpParam.isEndCommand[(int)Character.Command.COMMAND_TAG.WAIT])
            {
                /*
                // キーガイドを登録
                TransitKeyGuides(
                    (Constants.KeyIcon.ALL_CURSOR,  "Move"),
                    (Constants.KeyIcon.DECISION,    "DECISION"),
                    (Constants.KeyIcon.ESCAPE,      "TURN END"));
                */

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
            // _stageCtrl.SetGridCursorActive( false );

            base.Exit();
        }
    }
}