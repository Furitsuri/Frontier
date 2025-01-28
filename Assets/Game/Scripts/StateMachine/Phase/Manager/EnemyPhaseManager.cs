using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Frontier
{
    public class EnemyPhaseManager : PhaseManagerBase
    {
        /// <summary>
        /// ���������s���܂�
        /// </summary>
        override public void Init()
        {
            // �ڕW���W��U���Ώۂ����Z�b�g
            foreach (Enemy enemy in _btlMgr.BtlCharaCdr.GetCharacterEnumerable(Character.CHARACTER_TAG.ENEMY))
            {
                enemy.GetAi().ResetDestinationAndTarget();
            }
            // MEMO : ��L���Z�b�g��ɏ���������K�v�����邽�߂ɂ��̈ʒu�ł��邱�Ƃɒ���
            base.Init();
            // �I���O���b�h��(1�Ԗڂ�)�G�̃O���b�h�ʒu�ɍ��킹��
            if (0 < _btlMgr.BtlCharaCdr.GetCharacterCount(Character.CHARACTER_TAG.ENEMY) && _btlMgr.BtlCharaCdr.GetCharacterEnumerable(Character.CHARACTER_TAG.ENEMY) != null)
            {
                Character enemy = _btlMgr.BtlCharaCdr.GetCharacterEnumerable(Character.CHARACTER_TAG.ENEMY).First();
                _stgCtrl.ApplyCurrentGrid2CharacterGrid(enemy);
            }
            // �A�N�V�����Q�[�W�̉�
            _btlMgr.BtlCharaCdr.RecoveryActionGaugeForGroup(Character.CHARACTER_TAG.ENEMY);
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

        override protected void CreateTree()
        {
            // �J�ږ؂̍쐬
            RootNode = _hierarchyBld.InstantiateWithDiContainer<EMSelectState>();
            RootNode.AddChild(_hierarchyBld.InstantiateWithDiContainer<EMMoveState>());
            RootNode.AddChild(_hierarchyBld.InstantiateWithDiContainer<EMAttackState>());
            RootNode.AddChild(_hierarchyBld.InstantiateWithDiContainer<EMWaitState>());

            CurrentNode = RootNode;
        }

        /// <summary>
        /// �t�F�[�Y�A�j���[�V�������Đ����܂�
        /// </summary>
        override protected void StartPhaseAnim()
        {
            _btlUi.TogglePhaseUI(true, BattleManager.TurnType.ENEMY_TURN);
            _btlUi.StartAnimPhaseUI();
        }
    }
}
