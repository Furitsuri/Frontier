using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PLSelectCommandState : PhaseStateBase
{
    enum BaseCommand
    {
        COMMAND_MOVE = 0,
        COMMAND_ATTACK,
        COMMAND_WAIT,

        COMMAND_MAX,
    }

    public int SelectCommandIndex { get; set; } = 0;

    override public void Init()
    {
        base.Init();

        SelectCommandIndex = (int)BaseCommand.COMMAND_MOVE;

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
        if ((int)BaseCommand.COMMAND_MAX <= SelectCommandIndex)
        {
            SelectCommandIndex = 0;
        }
        else if (SelectCommandIndex < 0)
        {
            SelectCommandIndex = SelectCommandIndex = (int)BaseCommand.COMMAND_MAX - 1;
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
