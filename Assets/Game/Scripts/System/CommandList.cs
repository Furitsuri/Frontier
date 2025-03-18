using System;
using System.Collections;
using System.Collections.Generic;
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

    /// <summary>
    /// CommandListを使用するクラスに、このクラスをインスタンスとして持たせて下記Initを用いて参照値を渡すことで、
    /// CommandList内のUpdateでリストのIndex,Valueを更新させます
    /// </summary>
    public class CommandIndexedValue
    {
        public int index;
        public int value;

        public CommandIndexedValue( int i, int v ) { index = i; value = v; }
    }

    private LinkedList<int> _list;
    private LinkedListNode<int> _currentNode;
    private Constants.Direction _transitPrevInput;
    private Constants.Direction _transitNextInput;
    // このクラスを使用するクラス内で、コマンドのIndex値、及びValue値を適応させたい変数を下記2つに設定します
    private CommandIndexedValue _cmdIdxVal;

    /// <summary>
    /// 初期化します
    /// </summary>
    /// <param name="setCommandIndexs">操作させたいListの参照</param>
    /// <param name="direction">リストの方向</param>
    /// <param name="lastNodeStart">ノードの初期値を末尾にする(falseの場合は先頭)</param>
    /// <param name="cmdIdxVal">使用側がこのクラスに操作させたいIndexとValue</param>
    public void Init(ref List<int> setCommandIndexs, CommandDirection direction, bool lastNodeStart, CommandIndexedValue cmdIdxVal )
    {
        if (setCommandIndexs.Count <= 0)
        {
            Debug.Assert(false);
            setCommandIndexs.Add(0);
        }

        _list = new LinkedList<int>(setCommandIndexs);

        _currentNode = ( lastNodeStart ) ? _list.Last : _list.First;

        if( direction == CommandDirection.VERTICAL )
        {
            _transitPrevInput = Constants.Direction.FORWARD;
            _transitNextInput = Constants.Direction.BACK;
        }
        else if( direction == CommandDirection.HORIZONTAL )
        {
            _transitPrevInput = Constants.Direction.LEFT;
            _transitNextInput = Constants.Direction.RIGHT;
        }

        _cmdIdxVal = cmdIdxVal;
        _cmdIdxVal.index = GetCurrentIndex();
        _cmdIdxVal.value = GetCurrentValue();
    }

    /// <summary>
    /// 新たにコマンドを挿入します
    /// </summary>
    /// <param name="index">挿入目標とするListのIndex</param>
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

    /// <summary>
    /// 既存のコマンドを削除します
    /// </summary>
    /// <param name="index">削除目標のListのIndex</param>
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

    /// <summary>
    /// 入力を受け取ってコマンドリストの選択位置を変更します
    /// </summary>
    /// <param name="dir">方向入力</param>
    /// <returns>受け取った入力によって位置を更新したか</returns>
    public bool OperateListCursor( Constants.Direction dir )
    {
        bool isAccept = false;

        if ( _transitPrevInput == dir )
        {
            isAccept = true;

            if (_currentNode == _list.First)
            {
                _currentNode = _list.Last;
            }
            else
            {
                _currentNode = _currentNode.Previous;
            }
        }
        if ( _transitNextInput == dir )
        {
            isAccept = true;

            if (_currentNode == _list.Last)
            {
                _currentNode = _list.First;
            }
            else
            {
                _currentNode = _currentNode.Next;
            }
        }

        if (isAccept)
        {
            _cmdIdxVal.index = GetCurrentIndex();
            _cmdIdxVal.value = GetCurrentValue();

            return true;
        }

        return false;
    }

    /// <summary>
    /// 現在のノードのIndex値を取得します
    /// </summary>
    /// <returns>現在のノードのIndex値</returns>
    public int GetCurrentIndex()
    {
        if( _currentNode == null )
        {
            DebugUtils.NULL_ASSERT(_currentNode);
            return 0;
        }

        // LinkedListはFindからindex値を取れないため、順に検索
        int index = 0;
        for( var node = _list.First; node != null; node = node.Next, ++index )
        {
            if( node == _currentNode ) return index;
        }

        return -1;
    }

    /// <summary>
    /// 現在のノードのValue値を取得します
    /// </summary>
    /// <returns>現在ノードのValue値</returns>
    public int GetCurrentValue()
    {
        if (_currentNode == null)
        {
            DebugUtils.NULL_ASSERT(_currentNode);
            return 0;
        }

        return _currentNode.Value;
    }
}
