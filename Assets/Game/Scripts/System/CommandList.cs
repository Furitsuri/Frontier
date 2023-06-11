using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CommandList
{
    private LinkedList<int> list;
    private LinkedListNode<int> currentNode;

    // �g�p����R�}���h�̃C���f�b�N�X�z���n���ď�����
    public void Init(ref List<int> setCommandIndexs)
    {
        if (setCommandIndexs.Count <= 0)
        {
            // TODO : ERROR����
        }

        list = new LinkedList<int>(setCommandIndexs);

        currentNode = list.First;
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if( currentNode == list.First )
            {
                currentNode = list.Last;
            }
            else
            {
                currentNode = currentNode.Previous;
            }
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (currentNode == list.Last)
            {
                currentNode = list.First;
            }
            else
            {
                currentNode = currentNode.Next;
            }
        }
    }

    public void InsertCommand(int index)
    {
        LinkedListNode<int> current = list.First;

        while ( current != null )
        {
            // �����l�����݂���ꍇ�͎d�l�ᔽ
            if( current.Value == index )
            {
                // TODO : ERROR����
                return;
            }
            // index�̒l���傫���l�̒��O�ɑ}��
            if( index < current.Value )
            {
                list.AddBefore(current, index);
                return;
            }

            current = current.Next;
        }

        // ���݂��Ȃ������ꍇ�͖����ɒǉ�
        list.AddLast(index);
    }
    public void DeleteCommand(int index)
    {
        foreach (var item in list)
        {
            if (item == index)
            {
                list.Remove(item);
            }
        }

        currentNode = list.First;
    }

    public int GetCurrentIndex()
    {
        if( currentNode == null )
        {
            // TODO : ERROR����
            return 0;
        }

        return currentNode.Value;
    }
}
