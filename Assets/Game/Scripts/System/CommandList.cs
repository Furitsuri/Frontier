using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CommandList
{
    // コマンドリストの方向
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

    // 使用するコマンドのインデックス配列を渡して初期化
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
            // 同じ値が存在する場合は仕様違反
            if( current.Value == index )
            {
                Debug.Assert(false);
                return;
            }
            // indexの値より大きい値の直前に挿入
            if( index < current.Value )
            {
                _list.AddBefore(current, index);
                return;
            }

            current = current.Next;
        }

        // 存在しなかった場合は末尾に追加
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
