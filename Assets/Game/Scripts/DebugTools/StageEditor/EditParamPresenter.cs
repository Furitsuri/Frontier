using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EditParamPresenter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _tileTypeStr;
    [SerializeField] private TextMeshProUGUI _heightStr;

    public void Link( int type, int height )
    {
        _tileTypeStr.text = type.ToString();
    }

    /// <summary>
    /// テキストを更新します。
    /// </summary>
    /// <param name="type">タイプ</param>
    /// <param name="height">高さ</param>
    public void UpdateText( int type, int height )
    {
        _tileTypeStr.text   = ((TileType)type).ToString();
        _heightStr.text     = height.ToString();
    }
}
