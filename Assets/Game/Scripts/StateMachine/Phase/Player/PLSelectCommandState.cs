using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;

public class PLSelectCommandState : PhaseStateBase
{
    public int SelectCommandIndex { get; set; } = 0;
    private Player selectPlayer;
    private CommandList commandList = new CommandList();

    override public void Init()
    {
        base.Init();

        // �I�𒆂̃v���C���[���擾
        selectPlayer = BattleManager.instance.SearchPlayerFromCharaIndex(BattleManager.instance.SelectCharacterIndex);
        if(selectPlayer == null)
        {
            Debug.Assert(false);

            return;
        }

        // ���̓x�[�X���̐ݒ�
        List<int> commandIndexs = new List<int>((int)Character.BaseCommand.COMMAND_MAX_NUM);
        for (int i = 0; i < (int)Character.BaseCommand.COMMAND_MAX_NUM; ++i)
        {
            if ( !selectPlayer.tmpParam.isEndCommand[i] )
            {
                commandIndexs.Add(i);
            }
        }
        commandList.Init(ref commandIndexs);

        // UI���ւ��̃X�N���v�g��o�^���AUI��\��
        var instance = BattleUISystem.Instance;
        instance.PLCommandWindow.registPLCommandScript(this);
        instance.PLCommandWindow.RegistUnenableCommandIndexs(ref selectPlayer.tmpParam.isEndCommand);
        instance.TogglePLCommand(true);
    }

    override public void Update()
    {
        base.Update();

        commandList.Update();
        SelectCommandIndex = commandList.GetCurrentIndex();

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
