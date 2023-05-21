using System;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class StageGrid : MonoBehaviour
{
    public enum Face
    {
        xy,
        zx,
        yz,
    };

    Mesh mesh;
    [SerializeField]
    [HideInInspector]
    private int sizeX;
    [SerializeField]
    [HideInInspector]
    private int sizeZ;
    public GameObject m_StageObject;
    public float gridSize = 1f;
    public UnityEngine.Color color = UnityEngine.Color.white;
    public Face face = Face.zx;
    public bool back = true;
    public bool isAdjustStageScale = false;

    //更新検出用
    float preGridSize = 0;
    int preSize = 0;
    UnityEngine.Color preColor = UnityEngine.Color.red;
    Face preFace = Face.zx;
    bool preBack = true;

    private void Awake()
    {
        // ステージ情報から各サイズを参照する.
        if (isAdjustStageScale)
        {
            sizeX = (int)Math.Floor(m_StageObject.transform.localScale.x);
            sizeZ = (int)Math.Floor(m_StageObject.transform.localScale.z);
        }

        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh = ReGrid(mesh);
    }

    // Update is called once per frame
    void Update()
    {
        //関係値の更新を検出したらメッシュも更新
        if (gridSize != preGridSize || sizeX != preSize || preColor != color || preFace != face || preBack != back)
        {
            if (gridSize < 0) { gridSize = 0.000001f; }
            if (sizeX < 0) { sizeX = 1; }
            ReGrid(mesh);
        }
    }

    Mesh ReGrid(Mesh mesh)
    {
        if (back)
        {
            GetComponent<MeshRenderer>().material = new Material(Shader.Find("Sprites/Default"));
        }
        else
        {
            GetComponent<MeshRenderer>().material = new Material(Shader.Find("GUI/Text Shader"));
        }

        mesh.Clear();

        int resolution;
        int count = 0;
        float widthX, widthZ;
        Vector3[] vertices;
        Vector2[] uvs;
        int[] lines;
        UnityEngine.Color[] colors;

        widthX = gridSize * sizeX / 2.0f;
        widthZ = gridSize * sizeZ / 2.0f;
        Vector2 startPosition   = new Vector2(-widthX, -widthZ);
        Vector2 endPosition     = -startPosition;
        resolution = 2 * (sizeX + sizeZ + 2);
        vertices = new Vector3[resolution];
        uvs = new Vector2[resolution];
        lines = new int[resolution];
        colors = new UnityEngine.Color[resolution];

        // X方向の頂点
        for (int i = 0; count < 2 * (sizeX + 1); ++i, count = 2 * i)
        {
            vertices[count] = new Vector3(startPosition.x + ((float)i * gridSize), startPosition.y, 0);
            vertices[count + 1] = new Vector3(startPosition.x + ((float)i * gridSize), endPosition.y, 0);
        }
        // Y(Z)方向の頂点
        for (int i = 0; count < resolution; ++i, count = 2 * i + 2 * (sizeX + 1))
        {
            vertices[count] = new Vector3(startPosition.x, endPosition.y - ((float)i * gridSize), 0);
            vertices[count + 1] = new Vector3(endPosition.x, endPosition.y - ((float)i * gridSize), 0);
        }

        for (int i = 0; i < resolution; i++)
        {
            uvs[i] = Vector2.zero;
            lines[i] = i;
            colors[i] = color;
        }

        Vector3 rotDirection;
        switch (face)
        {
            case Face.xy:
                rotDirection = Vector3.forward;
                break;
            case Face.zx:
                rotDirection = Vector3.up;
                break;
            case Face.yz:
                rotDirection = Vector3.right;
                break;
            default:
                rotDirection = Vector3.forward;
                break;
        }

        mesh.vertices = RotationVertices(vertices, rotDirection);
        mesh.uv = uvs;
        mesh.colors = colors;
        mesh.SetIndices(lines, MeshTopology.Lines, 0);

        preGridSize = gridSize;
        preSize = sizeX;
        preColor = color;
        preFace = face;
        preBack = back;

        return mesh;
    }

    //頂点配列データーをすべて指定の方向へ回転移動させる
    Vector3[] RotationVertices(Vector3[] vertices, Vector3 rotDirection)
    {
        Vector3[] ret = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            ret[i] = Quaternion.LookRotation(rotDirection) * vertices[i];
        }
        return ret;
    }
#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(StageGrid))]
    public class StageGridEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            StageGrid script = target as StageGrid;

            // ステージ情報からサイズを決める際はサイズ編集を不可にする.
            EditorGUI.BeginDisabledGroup(script.isAdjustStageScale);
            script.sizeX = EditorGUILayout.IntField("X方向グリッドサイズ", script.sizeX);
            script.sizeZ = EditorGUILayout.IntField("Z方向グリッドサイズ", script.sizeZ);
            EditorGUI.EndDisabledGroup();

            base.OnInspectorGUI();
        }
    }
#endif // UNITY_EDITOR
}
