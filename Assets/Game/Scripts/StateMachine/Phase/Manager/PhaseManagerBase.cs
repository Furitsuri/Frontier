using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PhaseManagerBase : Tree<PhaseStateBase>
{
    protected bool _isFirstUpdate = false;
    protected BattleManager _btlMgr;

    virtual public void Init()
    {
        _btlMgr = ManagerProvider.Instance.GetService<BattleManager>();

        // �J�ږ؂̍쐬
        CreateTree();

        CurrentNode.Init();

        _isFirstUpdate = true;
    }

    virtual public bool Update()
    {
        // ���ݎ��s���̃X�e�[�g���X�V
        if( CurrentNode.Update() )
        {
            if( CurrentNode.IsBack() && CurrentNode.Parent == null )
            {
                return true;
            }
        }

        return false;
    }

    virtual public void LateUpdate()
    {
        // �X�e�[�g�̑J�ڂ��Ď�
        int transitIndex = CurrentNode.TransitIndex;
        if ( 0 <= transitIndex )
        {
            CurrentNode.Exit();
            CurrentNode = CurrentNode.Children[transitIndex];
            CurrentNode.Init();
        }
        else if( CurrentNode.IsBack() )
        {
            CurrentNode.Exit();
            CurrentNode = CurrentNode.Parent;
            CurrentNode.Init();
        }
    }

    /// <summary>
    /// �t�F�[�Y�A�j���[�V�������Đ����܂�
    /// </summary>
    virtual protected void StartPhaseAnim()
    {
    }
}
