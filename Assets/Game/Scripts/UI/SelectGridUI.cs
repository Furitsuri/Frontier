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

    // �l�p�`���C�����쐬
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

        // SetVertexCount�͔p�~����Ă���͂������A�g�p���Ȃ��Ɛ���ɕ`�悳��Ȃ��������ߎg�p(2023/5/26)
        m_LineRenderer.SetVertexCount(linePoints.Length);
        m_LineRenderer.SetPositions(linePoints);
    }
}