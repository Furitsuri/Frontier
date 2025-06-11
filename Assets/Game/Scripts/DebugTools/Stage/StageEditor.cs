using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using Frontier.Stage;

public class StageEditor : MonoBehaviour
{
    [SerializeField]
    private TileSelector _tileSelector; // 選択中のタイルタイプ

    [SerializeField]
    private GameObject _tilePrefab;

    private StageData _currentStage = new();

    public void SetTile(int x, int y, TileType type)
    {
        // ビジュアルを変更
        ReplaceTileVisual(x, y, type);

        // データを更新
        var existing = _currentStage.tiles.Find(t => t.x == x && t.y == y);
        if (existing != null)
            existing.tileType = type;
        else
            _currentStage.tiles.Add(new StageTileData { x = x, y = y, tileType = type });
    }

    public void ReplaceTileVisual(int x, int y, TileType type)
    {
        // タイルのビジュアルを置き換える処理
        // ここではプレハブをインスタンス化して配置する例を示す
        Vector3 position = new Vector3(x * _currentStage.GetGridSize(), 0, y * _currentStage.GetGridSize());
        GameObject tileObject = Instantiate(_tilePrefab, position, Quaternion.identity);
        // tileObject.GetComponent<TileSelector>().SetTileType(type); // タイルのタイプを設定するメソッドがあると仮定
    }
}