using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CommandList
{
    // �R�}���h���X�g�̕���
    public enum CommandDirection
    {
        VERTICAL = 0,
        HORIZONTAL,

        NUM,
    }

    private LinkedList<int> _list;
    private LinkedListNode<int> _currentNode;
    private KeyCode _transitPrevKey;
    private KeyCode _transitNextKey;

    // �g�p����R�}���h�̃C���f�b�N�X�z���n���ď�����
    public void Init(ref List<int> setCommandIndexs, CommandDirection direction )
    {
        if (setCommandIndexs.Count <= 0)
        {
            Debug.Assert(false);
            setCommandIndexs.Add(0);
        }

        _list = new LinkedList<int>(setCommandIndexs);

        _currentNode = _list.First;

        if( direction == CommandDirection.VERTICAL )
        {
            _transitPrevKey = KeyCode.UpArrow;
            _transitNextKey = KeyCode.DownArrow;
        }
        else if( direction == CommandDirection.HORIZONTAL )
        {
            _transitPrevKey = KeyCode.LeftArrow;
            _transitNextKey = KeyCode.RightArrow;
        }
    }

    public void Update()
    {
        if (Input.GetKeyDown(_transitPrevKey))
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
        if (Input.GetKeyDown(_transitNextKey))
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
