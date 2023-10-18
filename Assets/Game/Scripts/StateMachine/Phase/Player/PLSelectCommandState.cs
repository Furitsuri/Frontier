using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using static Frontier.Character;

namespace Frontier
{
    public class PLSelectCommandState : PhaseStateBase
    {
        public int SelectCommandIndex { get; private set; } = 0;
        public int SelectCommandValue { get; private set; } = 0;
        private Player _selectPlayer;
        private CommandList _commandList = new CommandList();

        override public void Init()
        {
            base.Init();

            // �I�𒆂̃v���C���[���擾
            _selectPlayer = (Player)_btlMgr.GetSelectCharacter();
            if (_selectPlayer == null)
            {
                Debug.Assert(false);

                return;
            }

            var endCommand = _selectPlayer.tmpParam.isEndCommand;
            if (endCommand[(int)Command.COMMAND_TAG.MOVE] && endCommand[(int)Command.COMMAND_TAG.ATTACK])
            {
                return;
            }

            // UI���ւ��̃X�N���v�g��o�^���AUI��\��
            var instance = BattleUISystem.Instance;
            List<Character.Command.COMMAND_TAG> executableCommands;
            _selectPlayer.FetchExecutableCommand(out executableCommands, _stageCtrl);

            // ���̓x�[�X���̐ݒ�
            List<int> commandIndexs = new List<int>();
            foreach ( var executableCmd in executableCommands )
            {
                commandIndexs.Add((int)executableCmd);
            }
            _commandList.Init(ref commandIndexs, CommandList.CommandDirection.VERTICAL);
            SelectCommandIndex = _commandList.GetCurrentIndex();
            SelectCommandValue = _commandList.GetCurrentValue();

            instance.PLCommandWindow.RegistPLCommandScript(this);
            instance.PLCommandWindow.SetExecutableCommandList(executableCommands);
            instance.TogglePLCommand(true);
        }

        override public bool Update()
        {
            var endCommand = _selectPlayer.tmpParam.isEndCommand;

            // �ړ��ƍU�����I����Ă���ꍇ�͎����I�ɏI����
            if (endCommand[(int)Command.COMMAND_TAG.MOVE] && endCommand[(int)Command.COMMAND_TAG.ATTACK])
            {
                Back();
                // �ҋ@���I��点��
                endCommand[(int)Command.COMMAND_TAG.WAIT] = true;

                return true;
            }

            if (base.Update())
            {
                // �ړ��̂ݏI�����Ă���ꍇ�͈ړ��O�ɖ߂��悤��          
                if (endCommand[(int)Command.COMMAND_TAG.MOVE] && !endCommand[(int)Command.COMMAND_TAG.ATTACK])
                {
                    _stageCtrl.FollowFootprint(_selectPlayer);
                    _stageCtrl.UpdateGridInfo();
                    endCommand[(int)Command.COMMAND_TAG.MOVE] = false;
                }

                return true;
            }

            if (_commandList.Update())
            {
                SelectCommandIndex = _commandList.GetCurrentIndex();
                SelectCommandValue = _commandList.GetCurrentValue();
            }

            if (Input.GetKeyUp(KeyCode.Space))
            {
                TransitIndex = SelectCommandValue;

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
}
