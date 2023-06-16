using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectGridUI : MonoBehaviour
{
    private float _GridSize = 0;
    private LineRenderer _LineRenderer;

    // Start is called before the first frame update
    void Start()
    {
        _GridSize      = StageGrid.Instance.gridSize;
        _LineRenderer  = gameObject.GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateSelectGridCursor();
    }

    void UpdateSelectGridCursor()
    {
        Vector3 centralPos = StageGrid.Instance.GetCurrentGridInfo().charaStandPos;

        setSquareLine(_GridSize, ref centralPos);
    }

    /// <summary>
    /// 指定した位置(centralPos)に四角形ラインを作成します
    /// </summary>
    /// <param name="gridSize">1グリッドのサイズ</param>
    /// /// <param name="centralPos">指定グリッドの中心位置</param>
    void setSquareLine(float gridSize, ref Vector3 centralPos)
    {
        float halfSize = 0.5f * gridSize;

        Vector3[] linePoints = new Vector3[]
        {
            new Vector3(-halfSize, 0.05f, -halfSize) + centralPos,
            new Vector3(-halfSize, 0.05f,  halfSize) + centralPos,
            new Vector3( halfSize, 0.05f,  halfSize) + centralPos,
            new Vector3( halfSize, 0.05f, -halfSize) + centralPos,
             
        };

        // SetVertexCountは廃止されているはずだが、使用しないと正常に描画されなかったため使用(2023/5/26)
        _LineRenderer.SetVertexCount(linePoints.Length);
        _LineRenderer.SetPositions(linePoints);
    }
}