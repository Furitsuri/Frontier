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
    /// ���������܂�
    /// </summary>
    /// <param name="initIndex">�����C���f�b�N�X�l</param>
    /// <param name="rowNum">�Ֆʂɂ�����s�ɊY������O���b�h��</param>
    /// <param name="columnNum">�Ֆʂɂ������ɊY������O���b�h��</param>
    public void Init( int initIndex, int rowNum, int columnNum )
    {
        _index          = initIndex;
        _rowNum         = rowNum;
        _columnNum      = columnNum;
        _atkTargetIndex = 0;
        _atkTargetNum   = 0;
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

    /// <summary>
    /// �U���ΏۃC���f�b�N�X�l���擾���܂�
    /// </summary>
    /// <returns>�U���ΏۃC���f�b�N�X�l</returns>
    public int GetAtkTargetIndex()
    {
        return _atkTargetIndex;
    }

    /// <summary>
    /// �U���ΏۃC���f�b�N�X�l��ݒ肵�܂�
    /// </summary>
    /// <param name="index">�U���ΏۃC���f�b�N�X�l</param>
    public void SetAtkTargetIndex(int index)
    {
        _atkTargetIndex = index;
    }

    /// <summary>
    /// �U���ΏۃC���f�b�N�X�̑�����ݒ肵�܂�
    /// </summary>
    /// <param name="num">�U���ΏۃC���f�b�N�X�̑���</param>
    public void SetAtkTargetNum(int num)
    {
        _atkTargetNum = num;
    }

    /// <summary>
    /// ���̃^�[�Q�b�g�C���f�b�N�X�l�ɑJ�ڂ��܂�
    /// </summary>
    public void TransitNextTarget()
    {
        _atkTargetIndex = (_atkTargetIndex + 1) % _atkTargetNum;
    }

    /// <summary>
    /// �O�̃^�[�Q�b�g�C���f�b�N�X�l�ɑJ�ڂ��܂�
    /// </summary>
    public void TransitPrevTarget()
    {
        _atkTargetIndex = ( _atkTargetIndex - 1 ) < 0 ? _atkTargetNum - 1 : _atkTargetIndex - 1;
    }

    /// <summary>
    /// �U���Ώۏ����N���A���܂�
    /// </summary>
    public void ClearAtkTargetInfo()
    {
        _atkTargetIndex = 0;
        _atkTargetNum = 0;
    }
}
