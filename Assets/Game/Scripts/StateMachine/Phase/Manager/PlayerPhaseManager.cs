using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace Frontier
{
    public class PlayerPhaseManager : PhaseManagerBase
    {
        /// <summary>
        /// ���������s���܂�
        /// </summary>
        override public void Init()
        {
            base.Init();

            if (0 < _btlMgr.BtlCharaCdr.GetCharacterCount(Character.CHARACTER_TAG.PLAYER))
            {
                // �I���O���b�h��(1�Ԗڂ�)�v���C���[�̃O���b�h�ʒu�ɍ��킹��
                Character player = _btlMgr.BtlCharaCdr.GetCharacterEnumerable(Character.CHARACTER_TAG.PLAYER).First();
                _stgCtrl.ApplyCurrentGrid2CharacterGrid(player);
                // �A�N�V�����Q�[�W�̉�
                _btlMgr.BtlCharaCdr.RecoveryActionGaugeForGroup(Character.CHARACTER_TAG.PLAYER);
            }
        }

        /// <summary>
        /// �X�V���s���܂�
        /// </summary>
        override public bool Update()
        {
            if (_isFirstUpdate)
            {
                // �t�F�[�Y�A�j���[�V�����̊J�n
                StartPhaseAnim();

                _isFirstUpdate = false;

                return false;
            }
            // �t�F�[�Y�A�j���[�V�������͑��얳��
            if (_btlUi.IsPlayingPhaseUI())
            {
                return false;
            }

            return base.Update();
        }

        /// <summary>
        /// �J�ڂ̖؍\�����쐬���܂�
        /// </summary>
        override protected void CreateTree()
        {
            // �J�ږ؂̍쐬
            // MEMO : �ʂ̃t�@�C��(XML�Ȃ�)����ǂݍ���ō쐬�o����悤�ɂ���̂��A��

            RootNode = _hierarchyBld.InstantiateWithDiContainer<PLSelectGridState>();
            RootNode.AddChild(_hierarchyBld.InstantiateWithDiContainer<PLSelectCommandState>());
            RootNode.AddChild(_hierarchyBld.InstantiateWithDiContainer<PLConfirmTurnEnd>());
            RootNode.Children[0].AddChild(_hierarchyBld.InstantiateWithDiContainer<PLMoveState>());
            RootNode.Children[0].AddChild(_hierarchyBld.InstantiateWithDiContainer<PLAttackState>());
            RootNode.Children[0].AddChild(_hierarchyBld.InstantiateWithDiContainer<PLWaitState>());

            CurrentNode = RootNode;
        }

        /// <summary>
        /// �t�F�[�Y�A�j���[�V�������Đ����܂�
        /// </summary>
        override protected void StartPhaseAnim()
        {
            _btlUi.TogglePhaseUI(true, BattleManager.TurnType.PLAYER_TURN);
            _btlUi.StartAnimPhaseUI();
        }
    }
}