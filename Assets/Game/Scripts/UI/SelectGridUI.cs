using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectGridUI : MonoBehaviour
{
    private float m_GridSize = 0;
    private LineRenderer m_LineRenderer;

    // Start is called before the first frame update
    void Start()
    {
        m_GridSize      = StageGrid.instance.gridSize;
        m_LineRenderer  = gameObject.GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateSelectGridCursor();
    }

    void UpdateSelectGridCursor()
    {
        Vector3 centralPos = StageGrid.instance.getCurrentGridInfo().charaStandPos;

        setSquareLine(m_GridSize, ref centralPos);

    }

    // 四角形ラインを作成
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
        m_LineRenderer.SetVertexCount(linePoints.Length);
        m_LineRenderer.SetPositions(linePoints);
    }
}