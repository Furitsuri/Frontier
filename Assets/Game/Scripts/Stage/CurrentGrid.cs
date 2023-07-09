using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class CurrentGrid
{
    private static CurrentGrid _currentGrid = new CurrentGrid();
    private int _index = 0;
    private int _rowNum = 0;
    private int _columnNum = 0;
    private int _atkTargetIndex = 0;
    private int _atkTargetNum = 0;

    public static CurrentGrid GetInstance() { return _currentGrid; }

    /// <summary>
    /// 初期化します
    /// </summary>
    /// <param name="initIndex">初期インデックス値</param>
    /// <param name="rowNum">盤面における行に該当するグリッド数</param>
    /// <param name="columnNum">盤面における列に該当するグリッド数</param>
    public void Init( int initIndex, int rowNum, int columnNum )
    {
        _index          = initIndex;
        _rowNum         = rowNum;
        _columnNum      = columnNum;
        _atkTargetIndex = 0;
        _atkTargetNum   = 0;
    }

    /// <summary>
    /// インデックス値を取得します
    /// </summary>
    /// <returns>インデックス値</returns>
    public int GetIndex()
    {
        return _index;
    }

    /// <summary>
    /// インデックス値を設定します
    /// </summary>
    /// <param name="index">指定するインデックス値</param>
    public void SetIndex( int index )
    {
        _index = index;
    }

    /// <summary>
    /// インデックス値を現在グリッドの上に該当する値に設定します
    /// </summary>
    public void Up()
    {
        _index += _rowNum;
        if( _rowNum * _columnNum <= _index )
        {
            _index = _index % (_rowNum * _columnNum);
        }
    }

    /// <summary>
    /// インデックス値を現在グリッドの下に該当する値に設定します
    /// </summary>
    public void Down()
    {
        _index -= _rowNum;
        if (_index < 0)
        {
            _index += _rowNum * _columnNum;
        }
    }

    /// <summary>
    /// インデックス値を現在グリッドの右に該当する値に設定します
    /// </summary>
    public void Right()
    {
        _index++;
        if( _index % _rowNum == 0 )
        {
            _index -= _rowNum;
        }
    }

    /// <summary>
    /// インデックス値を現在グリッドの左に該当する値に設定します
    /// </summary>
    public void Left()
    {
        _index--;
        if( ( _index  + 1 ) % _rowNum == 0 )
        {
            _index += _rowNum;
        }
    }

    /// <summary>
    /// 攻撃対象インデックス値を取得します
    /// </summary>
    /// <returns>攻撃対象インデックス値</returns>
    public int GetAtkTargetIndex()
    {
        return _atkTargetIndex;
    }

    /// <summary>
    /// 攻撃対象インデックス値を設定します
    /// </summary>
    /// <param name="index">攻撃対象インデックス値</param>
    public void SetAtkTargetIndex(int index)
    {
        _atkTargetIndex = index;
    }

    /// <summary>
    /// 攻撃対象インデックスの総数を設定します
    /// </summary>
    /// <param name="num">攻撃対象インデックスの総数</param>
    public void SetAtkTargetNum(int num)
    {
        _atkTargetNum = num;
    }

    /// <summary>
    /// 次のターゲットインデックス値に遷移します
    /// </summary>
    public void TransitNextTarget()
    {
        _atkTargetIndex = (_atkTargetIndex + 1) % _atkTargetNum;
    }

    /// <summary>
    /// 前のターゲットインデックス値に遷移します
    /// </summary>
    public void TransitPrevTarget()
    {
        _atkTargetIndex = ( _atkTargetIndex - 1 ) < 0 ? _atkTargetNum - 1 : _atkTargetIndex - 1;
    }

    /// <summary>
    /// 攻撃対象情報をクリアします
    /// </summary>
    public void ClearAtkTargetInfo()
    {
        _atkTargetIndex = 0;
        _atkTargetNum = 0;
    }
}
