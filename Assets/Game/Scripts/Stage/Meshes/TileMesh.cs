using UnityEngine;
using static Constants;

namespace Frontier.Stage
{
    public class TileMesh : MonoBehaviour
    {
        // TileMeshはTileBehaviorの子オブジェクトとして配置されるため、
        // TileBehaviorの座標を基準にしている。TileBehaviorの座標はグリッドの中心位置。
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

        public void Init(bool back, in Vector3 pos, float tileHalfHeight)
        {
            if (back)
            {
                GetComponent<MeshRenderer>().material = new Material(Shader.Find("Sprites/Default"));
            }
            else
            {
                GetComponent<MeshRenderer>().material = new Material(Shader.Find("GUI/Text Shader"));
            }

            transform.position  = pos + new Vector3( 0f, tileHalfHeight, 0f);
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

            Destroy(gameObject);
            Destroy( this );
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
            resolution  = 16;
            vertices    = new Vector3[resolution];
            uvs         = new Vector2[resolution];
            lines       = new int[resolution];
            colors      = new Color[resolution];

            // XZ平面上の線分の頂点
            for (int i = 0; count < (int)(0.5f * resolution); ++i, count = 4 * i)
            {
                vertices[count]     = new Vector3(startPosition.x + ((float)i * TILE_SIZE), 0.0f, startPosition.y);
                vertices[count + 1] = new Vector3(startPosition.x + ((float)i * TILE_SIZE), 0.0f, endPosition.y);
                vertices[count + 2] = new Vector3(startPosition.x,  0.0f, startPosition.y + ((float)i * TILE_SIZE));
                vertices[count + 3] = new Vector3(endPosition.x,    0.0f, startPosition.y + ((float)i * TILE_SIZE));
            }
            
            // XY平面上の線分の頂点
            startPosition   = new Vector2(_tilePos.x - 0.5f * TILE_SIZE, _tilePos.y - TILE_SIZE);    // タイル位置はTileBehaviourの位置を基準にしており、スケーリングによって高さは自動的に変化するためこの値
            endPosition     = new Vector2(_tilePos.x + 0.5f * TILE_SIZE, _tilePos.y);
            for (int i = 0; count < resolution; ++i, count = 4 * i + (int)(0.5f * resolution))
            {
                vertices[count]     = new Vector3(startPosition.x + ((float)i * TILE_SIZE), startPosition.y,    -0.5f * TILE_SIZE);
                vertices[count + 1] = new Vector3(startPosition.x + ((float)i * TILE_SIZE), endPosition.y,      -0.5f * TILE_SIZE);
                vertices[count + 2] = new Vector3(startPosition.x + ((float)i * TILE_SIZE), startPosition.y,    0.5f * TILE_SIZE);
                vertices[count + 3] = new Vector3(startPosition.x + ((float)i * TILE_SIZE), endPosition.y,      0.5f * TILE_SIZE);
            }

            for (int i = 0; i < resolution; i++)
            {
                uvs[i]      = Vector2.zero;
                lines[i]    = i;
                colors[i]   = Color.black;
            }

            _mesh.vertices  = vertices;
            _mesh.uv        = uvs;
            _mesh.colors    = colors;
            _mesh.SetIndices(lines, MeshTopology.Lines, 0);
        }
    }
}