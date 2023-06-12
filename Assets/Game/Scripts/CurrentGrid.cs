using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class CurrentGrid
{
    private static CurrentGrid _currentGrid = new CurrentGrid();
    private int _index = 0;
    private int _rowNum = 0;
    private int _columnNum = 0;

    public static CurrentGrid GetInstance() { return _currentGrid; }

    /// <summary>
    /// ���������܂�
    /// </summary>
    /// <param name="initIndex">�����C���f�b�N�X�l</param>
    /// <param name="rowNum">�Ֆʂɂ�����s�ɊY������O���b�h��</param>
    /// <param name="columnNum">�Ֆʂɂ������ɊY������O���b�h��</param>
    public void Init( int initIndex, int rowNum, int columnNum )
    {
        _index = initIndex;
        _rowNum = rowNum;
        _columnNum = columnNum;
    }

    /// <summary>
    /// �C���f�b�N�X�l���擾���܂�
    /// </summary>
    /// <returns>�C���f�b�N�X�l</returns>
    public int GetIndex()
    {
        return _index;
    }

    /// <summary>
    /// �C���f�b�N�X�l��ݒ肵�܂�
    /// </summary>
    /// <param name="index">�w�肷��C���f�b�N�X�l</param>
    public void SetIndex( int index )
    {
        _index = index;
    }

    /// <summary>
    /// �C���f�b�N�X�l�����݃O���b�h�̏�ɊY������l�ɐݒ肵�܂�
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
    /// �C���f�b�N�X�l�����݃O���b�h�̉��ɊY������l�ɐݒ肵�܂�
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
    /// �C���f�b�N�X�l�����݃O���b�h�̉E�ɊY������l�ɐݒ肵�܂�
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
    /// �C���f�b�N�X�l�����݃O���b�h�̍��ɊY������l�ɐݒ肵�܂�
    /// </summary>
    public void Left()
    {
        _index--;
        if( ( _index  + 1 ) % _rowNum == 0 )
        {
            _index += _rowNum;
        }
    }
}
