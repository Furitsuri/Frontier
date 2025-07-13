using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Constants;

public class TileMesh : MonoBehaviour
{
    private Vector3 _tilePos = Vector3.zero;
    private Mesh _mesh;
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;

    private void Awake()
    {
        gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>();

        GetComponent<MeshFilter>().mesh = _mesh = new Mesh();
    }

    public void Init(bool back)
    {
        if (back)
        {
            GetComponent<MeshRenderer>().material = new Material(Shader.Find("Sprites/Default"));
        }
        else
        {
            GetComponent<MeshRenderer>().material = new Material(Shader.Find("GUI/Text Shader"));
        }
    }

    public void Dispose()
    {         
        if (_mesh != null)
        {
            Destroy(_mesh);
            _mesh = null;
        }
        if (_meshFilter != null)
        {
            Destroy(_meshFilter);
            _meshFilter = null;
        }
        if (_meshRenderer != null)
        {
            Destroy(_meshRenderer);
            _meshRenderer = null;
        }
    }

    public void DrawMesh()
    {
        _mesh.Clear();

        int resolution;
        int count = 0;
        Vector3[] vertices;
        Vector2[] uvs;
        int[] lines;
        Color[] colors;

        Vector2 tileHalfSize    = new Vector2(0.5f * TILE_SIZE, 0.5f * TILE_SIZE);
        Vector2 startPosition   = new Vector2(_tilePos.x, _tilePos.z) - tileHalfSize;
        Vector2 endPosition     = new Vector2(_tilePos.x, _tilePos.z) + tileHalfSize;
        resolution              = 8;
        vertices                = new Vector3[resolution];
        uvs                     = new Vector2[resolution];
        lines                   = new int[resolution];
        colors                  = new Color[resolution];

        // XZ平面上の線分の頂点
        for (int i = 0; count < 4; ++i, count = 2 * i)
        {
            vertices[count]     = new Vector3(startPosition.x + ((float)i * TILE_SIZE), 0.0f, startPosition.y);
            vertices[count + 1] = new Vector3(startPosition.x + ((float)i * TILE_SIZE), 0.0f, endPosition.y);
        }

        /*
        // Z方向の線分の頂点
        for (int i = 0; count < 2 * (_stageData.GridRowNum + 1); ++i, count = 2 * i)
        {
            vertices[count] = new Vector3(startPosition.x + ((float)i * TILE_SIZE), 0.0f, startPosition.y);
            vertices[count + 1] = new Vector3(startPosition.x + ((float)i * TILE_SIZE), 0.0f, endPosition.y);
        }
        // X方向の線分の頂点
        for (int i = 0; count < resolution; ++i, count = 2 * i + 2 * (_stageData.GridRowNum + 1))
        {
            vertices[count] = new Vector3(startPosition.x, 0.0f, endPosition.y - ((float)i * TILE_SIZE));
            vertices[count + 1] = new Vector3(endPosition.x, 0.0f, endPosition.y - ((float)i * TILE_SIZE));
        }
        */

        for (int i = 0; i < resolution; i++)
        {
            uvs[i] = Vector2.zero;
            lines[i] = i;
            colors[i] = UnityEngine.Color.black;
        }

        _mesh.vertices = vertices;
        _mesh.uv = uvs;
        _mesh.colors = colors;
        _mesh.SetIndices(lines, MeshTopology.Lines, 0);
    }
}
