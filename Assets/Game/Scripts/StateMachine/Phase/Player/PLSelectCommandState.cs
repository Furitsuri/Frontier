using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using static Character;

public class PLSelectCommandState : PhaseStateBase
{
    public int SelectCommandIndex { get; set; } = 0;
    private Player _selectPlayer;
    private CommandList _commandList = new CommandList();

    override public void Init()
    {
        base.Init();

        // �I�𒆂̃v���C���[���擾
        _selectPlayer = (Player)BattleManager.Instance.GetSelectCharacter();
        if(_selectPlayer == null)
        {
            Debug.Assert( false );

            return;
        }

        var endCommand = _selectPlayer.tmpParam.isEndCommand;
        if (endCommand[(int)BaseCommand.COMMAND_MOVE] && endCommand[(int)BaseCommand.COMMAND_ATTACK])
        {
            return;
        }

        // ���̓x�[�X���̐ݒ�
        List<int> commandIndexs = new List<int>((int)Character.BaseCommand.COMMAND_MAX_NUM);
        for (int i = 0; i < (int)Character.BaseCommand.COMMAND_MAX_NUM; ++i)
        {
            if ( !_selectPlayer.tmpParam.isEndCommand[i] )
            {
                commandIndexs.Add(i);
            }
        }
        _commandList.Init(ref commandIndexs, CommandList.CommandDirection.VERTICAL);

        // UI���ւ��̃X�N���v�g��o�^���AUI��\��
        var instance = BattleUISystem.Instance;
        instance.PLCommandWindow.registPLCommandScript(this);
        instance.PLCommandWindow.RegistUnenableCommandIndexs(ref _selectPlayer.tmpParam.isEndCommand);
        instance.TogglePLCommand(true);
    }

    override public bool Update()
    {
        var endCommand = _selectPlayer.tmpParam.isEndCommand;

        // �ړ��ƍU�����I����Ă���ꍇ�͎����I�ɏI����
        if ( endCommand[(int)BaseCommand.COMMAND_MOVE] && endCommand[(int)BaseCommand.COMMAND_ATTACK] )
        {
            Back();
            // �ҋ@���I��点��
            endCommand[(int)BaseCommand.COMMAND_WAIT] = true;

            return true;
        }

        if ( base.Update() )
        {
            // �ړ��̂ݏI�����Ă���ꍇ�͈ړ��O�ɖ߂��悤��          
            if ( endCommand[(int)BaseCommand.COMMAND_MOVE] && !endCommand[(int)BaseCommand.COMMAND_ATTACK] )
            {
                StageGrid.Instance.FollowFootprint( _selectPlayer );
                endCommand[(int)BaseCommand.COMMAND_MOVE] = false;
            }

            return true;
        }

        _commandList.Update();
        SelectCommandIndex = _commandList.GetCurrentIndex();

        if (Input.GetKeyUp(KeyCode.Space))
        {
            TransitIndex = SelectCommandIndex;

            return true;
        }

        return false;
    }

    override public void Exit()
    {
        // UI���\��
        BattleUISystem.Instance.TogglePLCommand(false);

        base.Exit();
    }
}
