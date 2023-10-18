using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

            if (0 < _btlMgr.GetCharacterCount(Character.CHARACTER_TAG.PLAYER))
            {
                // �I���O���b�h��(1�Ԗڂ�)�v���C���[�̃O���b�h�ʒu�ɍ��킹��
                Player player = _btlMgr.GetPlayerEnumerable().First();
                _stageCtrl.ApplyCurrentGrid2CharacterGrid(player);
                // �A�N�V�����Q�[�W�̉�
                _btlMgr.RecoveryActionGaugeForGroup(Character.CHARACTER_TAG.PLAYER);
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
            if (BattleUISystem.Instance.IsPlayingPhaseUI())
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
            // TODO : �ʂ̃t�@�C��(XML�Ȃ�)����ǂݍ���ō쐬�o����悤�ɂ���̂��A��

            RootNode = new PLSelectGrid();
            RootNode.AddChild(new PLSelectCommandState());
            RootNode.AddChild(new PLConfirmTurnEnd());
            RootNode.Children[0].AddChild(new PLMoveState());
            RootNode.Children[0].AddChild(new PLAttackState());
            RootNode.Children[0].AddChild(new PLWaitState());

            CurrentNode = RootNode;
        }

        /// <summary>
        /// �t�F�[�Y�A�j���[�V�������Đ����܂�
        /// </summary>
        override protected void StartPhaseAnim()
        {
            BattleUISystem.Instance.TogglePhaseUI(true, BattleManager.TurnType.PLAYER_TURN);
            BattleUISystem.Instance.StartAnimPhaseUI();
        }
    }
}