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
    /// �w�肵���ʒu(centralPos)�Ɏl�p�`���C�����쐬���܂�
    /// </summary>
    /// <param name="gridSize">1�O���b�h�̃T�C�Y</param>
    /// /// <param name="centralPos">�w��O���b�h�̒��S�ʒu</param>
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
        _LineRenderer.SetVertexCount(linePoints.Length);
        _LineRenderer.SetPositions(linePoints);
    }
}