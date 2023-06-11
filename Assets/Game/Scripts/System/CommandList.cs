using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CommandList
{
    private LinkedList<int> list;
    private LinkedListNode<int> currentNode;

    // 使用するコマンドのインデックス配列を渡して初期化
    public void Init(ref List<int> setCommandIndexs)
    {
        if (setCommandIndexs.Count <= 0)
        {
            // TODO : ERROR処理
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
            // 同じ値が存在する場合は仕様違反
            if( current.Value == index )
            {
                // TODO : ERROR処理
                return;
            }
            // indexの値より大きい値の直前に挿入
            if( index < current.Value )
            {
                list.AddBefore(current, index);
                return;
            }

            current = current.Next;
        }

        // 存在しなかった場合は末尾に追加
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
            // TODO : ERROR処理
            return 0;
        }

        return currentNode.Value;
    }
}
