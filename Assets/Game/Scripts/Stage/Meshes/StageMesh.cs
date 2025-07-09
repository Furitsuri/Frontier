using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Constants;
using Zenject;

namespace Frontier.Stage
{
    public class StageMesh : MonoBehaviour
    {
        private StageData _stageData;
        private Mesh _mesh;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        [Inject]
        public void Construct(StageData stageData)
        {
            _stageData = stageData;
        }

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

        public void DrawMesh()
        {
            _mesh.Clear();

            int resolution;
            int count = 0;
            Vector3[] vertices;
            Vector2[] uvs;
            int[] lines;
            Color[] colors;

            Vector2 startPosition = new Vector2(0.0f, 0.0f);
            Vector2 endPosition = new Vector2(_stageData.WidthX(), _stageData.WidthZ());
            resolution = 2 * (_stageData.GridRowNum + _stageData.GridColumnNum + 2);
            vertices = new Vector3[resolution];
            uvs = new Vector2[resolution];
            lines = new int[resolution];
            colors = new Color[resolution];

            // Z方向の線分の頂点
            for (int i = 0; count < 2 * (_stageData.GridRowNum + 1); ++i, count = 2 * i)
            {
                vertices[count]     = new Vector3(startPosition.x + ((float)i * TILE_SIZE), 0.1f, startPosition.y);
                vertices[count + 1] = new Vector3(startPosition.x + ((float)i * TILE_SIZE), 0.1f, endPosition.y);
            }
            // X方向の線分の頂点
            for (int i = 0; count < resolution; ++i, count = 2 * i + 2 * (_stageData.GridRowNum + 1))
            {
                vertices[count]     = new Vector3(startPosition.x, 0.0f, endPosition.y - ((float)i * TILE_SIZE));
                vertices[count + 1] = new Vector3(endPosition.x, 0.0f, endPosition.y - ((float)i * TILE_SIZE));
            }

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
}