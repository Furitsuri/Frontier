using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PLSelectCommandState : PhaseStateBase
{
    public int SelectCommandIndex { get; set; } = 0;
    private bool[] enableCommands = new bool[(int)Character.BaseCommand.COMMAND_MAX_NUM];
    private Player selectPlayer;

    override public void Init()
    {
        base.Init();

        selectPlayer = BattleManager.instance.GetPlayerFromIndex(BattleManager.instance.SelectCharacterIndex);
        for( int i =0; i < (int)Character.BaseCommand.COMMAND_MAX_NUM; ++i )
        {
            enableCommands[i] = selectPlayer.tmpParam.isEndCommand[i];
        }

        SelectCommandIndex = (int)Character.BaseCommand.COMMAND_MOVE;

        // UI���ւ��̃X�N���v�g��o�^���AUI��\��
        var instance = BattleUISystem.Instance;
        instance.PLCommandWindow.registPLCommandScript(this);
        instance.TogglePLCommand(true);
    }

    override public void Update()
    {
        base.Update();

        // �ȈՓI�ȃL�[�������
        if (Input.GetKeyDown(KeyCode.UpArrow)) { SelectCommandIndex--; }
        if (Input.GetKeyDown(KeyCode.DownArrow)) { SelectCommandIndex++; }
        if ((int)Character.BaseCommand.COMMAND_MAX_NUM <= SelectCommandIndex)
        {
            SelectCommandIndex = 0;
        }
        else if (SelectCommandIndex < 0)
        {
            SelectCommandIndex = SelectCommandIndex = (int)Character.BaseCommand.COMMAND_MAX_NUM - 1;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            TransitIndex = SelectCommandIndex;

            return;
        }
    }

    override public void Exit()
    {
        // UI���\��
        var instance = BattleUISystem.Instance;
        instance.TogglePLCommand(false);

        base.Exit();
    }
}
