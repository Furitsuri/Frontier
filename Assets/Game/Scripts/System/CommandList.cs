using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CommandList
{
    private LinkedList<int> _list;
    private LinkedListNode<int> _currentNode;

    // �g�p����R�}���h�̃C���f�b�N�X�z���n���ď�����
    public void Init(ref List<int> setCommandIndexs)
    {
        if (setCommandIndexs.Count <= 0)
        {
            Debug.Assert(false);
            setCommandIndexs.Add(0);
        }

        _list = new LinkedList<int>(setCommandIndexs);

        _currentNode = _list.First;
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if( _currentNode == _list.First )
            {
                _currentNode = _list.Last;
            }
            else
            {
                _currentNode = _currentNode.Previous;
            }
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (_currentNode == _list.Last)
            {
                _currentNode = _list.First;
            }
            else
            {
                _currentNode = _currentNode.Next;
            }
        }
    }

    public void InsertCommand(int index)
    {
        LinkedListNode<int> current = _list.First;

        while ( current != null )
        {
            // �����l�����݂���ꍇ�͎d�l�ᔽ
            if( current.Value == index )
            {
                Debug.Assert(false);
                return;
            }
            // index�̒l���傫���l�̒��O�ɑ}��
            if( index < current.Value )
            {
                _list.AddBefore(current, index);
                return;
            }

            current = current.Next;
        }

        // ���݂��Ȃ������ꍇ�͖����ɒǉ�
        _list.AddLast(index);
    }
    public void DeleteCommand(int index)
    {
        foreach (var item in _list)
        {
            if (item == index)
            {
                _list.Remove(item);
            }
        }

        _currentNode = _list.First;
    }

    public int GetCurrentIndex()
    {
        if( _currentNode == null )
        {
            Debug.Assert(false);
            return 0;
        }

        return _currentNode.Value;
    }
}
