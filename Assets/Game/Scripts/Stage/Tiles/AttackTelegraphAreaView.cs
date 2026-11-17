using System.Collections.Generic;
using UnityEngine;
using static Constants;

namespace Frontier.Stage
{
    /// <summary>
    /// NPCが次のフェーズで使用を予約した攻撃の範囲(予兆)を、1件の予約ごとに「薄い塗り + 濃い外周の縁取り」で表示します。
    /// 予約ごとに1つのインスタンスを用いるため、複数の予兆が重なっても各範囲の形(どの敵がどこを狙っているか)が外周で判別できます。
    /// 見た目(塗り・縁取りの色や描画順はマテリアル、縁取りの太さやタイル表面からの高さはこのコンポーネントの値)はすべて
    /// プレハブ(AttackTelegraphArea.prefab)側で設定し、このクラスは指定されたタイル群の形状からメッシュを組み立てることのみを担います。
    /// 生成はPrefabRegistry.AttackTelegraphAreaPrefabを基に、HierarchyBuilderBase経由で行ってください。
    /// </summary>
    public class AttackTelegraphAreaView : MonoBehaviour
    {
        [Header( "描画先" )]
        [SerializeField] private MeshFilter _fillMeshFilter     = null;    // 範囲内の塗り(色・描画順はこのMeshFilterと同じオブジェクトのMeshRendererのマテリアルで設定)
        [SerializeField] private MeshFilter _outlineMeshFilter  = null;    // 範囲の外周の縁取り(色・描画順はこのMeshFilterと同じオブジェクトのMeshRendererのマテリアルで設定)

        [Header( "形状" )]
        [Tooltip( "縁取りの太さ(タイル1枚の幅を1とした値)。縁取りは範囲の内側に向かって描画されます" )]
        [SerializeField, Range( 0.01f, 0.5f )] private float _outlineWidth          = 0.08f;
        [Tooltip( "タイル表面から塗りまでの高さ。他の範囲表示(危険範囲等)より上に表示する場合は、それらより大きくしてください" )]
        [SerializeField] private float _fillHeightOffset                            = 0.04f;
        [Tooltip( "タイル表面から縁取りまでの高さ。塗りより大きくしてください" )]
        [SerializeField] private float _outlineHeightOffset                         = 0.045f;

        private Mesh _fillMesh      = null;
        private Mesh _outlineMesh   = null;

        // タイル上で見た上下左右の隣接方向(x, y)。順番は北(+Z)・東(+X)・南(-Z)・西(-X)
        private static readonly Vector2Int[] SideOffsets = { new Vector2Int( 0, 1 ), new Vector2Int( 1, 0 ), new Vector2Int( 0, -1 ), new Vector2Int( -1, 0 ) };
        private const int NORTH = 0, EAST = 1, SOUTH = 2, WEST = 3;

        /// <summary>
        /// 指定したタイル群を予兆の範囲として表示します
        /// </summary>
        /// <param name="stageData">表示するステージ</param>
        /// <param name="tileIndices">予兆の範囲に含まれるタイルのインデックス</param>
        public void Show( StageData stageData, IReadOnlyCollection<int> tileIndices )
        {
            // メッシュはワールド座標で組み立てるため、配置先の親に関わらず原点に置く
            transform.SetPositionAndRotation( Vector3.zero, Quaternion.identity );

            var region = new HashSet<int>( tileIndices );

            var fillVertices        = new List<Vector3>();
            var fillTriangles       = new List<int>();
            var outlineVertices     = new List<Vector3>();
            var outlineTriangles    = new List<int>();

            int colNum = stageData.TileColNum;
            int rowNum = stageData.TileRowNum;
            float half = 0.5f * TILE_SIZE;
            float w    = Mathf.Min( _outlineWidth, half ) * TILE_SIZE;

            foreach( int index in region )
            {
                if( index < 0 || stageData.GetTileTotalNum() <= index ) { continue; }

                int x = index % colNum;
                int y = index / colNum;
                Vector3 center = stageData.GetTile( index ).StaticData().CursorStandPos;
                float x0 = center.x - half, x1 = center.x + half;
                float z0 = center.z - half, z1 = center.z + half;

                AddQuad( fillVertices, fillTriangles, x0, x1, z0, z1, center.y + _fillHeightOffset );

                // 隣接タイルが範囲外の辺を外周として縁取る
                bool[] isEdge = new bool[SideOffsets.Length];
                for( int side = 0; side < SideOffsets.Length; ++side )
                {
                    isEdge[side] = !Contains( region, x + SideOffsets[side].x, y + SideOffsets[side].y, colNum, rowNum );
                }

                float h = center.y + _outlineHeightOffset;
                if( isEdge[NORTH] ) { AddQuad( outlineVertices, outlineTriangles, x0, x1, z1 - w, z1, h ); }
                if( isEdge[SOUTH] ) { AddQuad( outlineVertices, outlineTriangles, x0, x1, z0, z0 + w, h ); }
                // 東西の辺は、南北の辺と角で重ならないよう(半透明の場合に角だけ濃くならないよう)端を詰める
                float zFrom = isEdge[SOUTH] ? z0 + w : z0;
                float zTo   = isEdge[NORTH] ? z1 - w : z1;
                if( isEdge[EAST] ) { AddQuad( outlineVertices, outlineTriangles, x1 - w, x1, zFrom, zTo, h ); }
                if( isEdge[WEST] ) { AddQuad( outlineVertices, outlineTriangles, x0, x0 + w, zFrom, zTo, h ); }

                // 範囲が内側に折れ曲がる角(隣接する2辺は範囲内だが、斜めのタイルが範囲外)は、縁取りが欠けないよう角を埋める
                if( !isEdge[NORTH] && !isEdge[EAST] && !Contains( region, x + 1, y + 1, colNum, rowNum ) ) { AddQuad( outlineVertices, outlineTriangles, x1 - w, x1, z1 - w, z1, h ); }
                if( !isEdge[SOUTH] && !isEdge[EAST] && !Contains( region, x + 1, y - 1, colNum, rowNum ) ) { AddQuad( outlineVertices, outlineTriangles, x1 - w, x1, z0, z0 + w, h ); }
                if( !isEdge[SOUTH] && !isEdge[WEST] && !Contains( region, x - 1, y - 1, colNum, rowNum ) ) { AddQuad( outlineVertices, outlineTriangles, x0, x0 + w, z0, z0 + w, h ); }
                if( !isEdge[NORTH] && !isEdge[WEST] && !Contains( region, x - 1, y + 1, colNum, rowNum ) ) { AddQuad( outlineVertices, outlineTriangles, x0, x0 + w, z1 - w, z1, h ); }
            }

            ApplyMesh( ref _fillMesh, _fillMeshFilter, fillVertices, fillTriangles );
            ApplyMesh( ref _outlineMesh, _outlineMeshFilter, outlineVertices, outlineTriangles );

            gameObject.SetActive( true );
        }

        /// <summary>
        /// 表示を破棄します
        /// </summary>
        public void Remove()
        {
            Destroy( gameObject );
        }

        private void OnDestroy()
        {
            if( _fillMesh != null ) { Destroy( _fillMesh ); }
            if( _outlineMesh != null ) { Destroy( _outlineMesh ); }
        }

        private static bool Contains( HashSet<int> region, int x, int y, int colNum, int rowNum )
        {
            if( x < 0 || colNum <= x || y < 0 || rowNum <= y ) { return false; }

            return region.Contains( x + y * colNum );
        }

        private static void AddQuad( List<Vector3> vertices, List<int> triangles, float x0, float x1, float z0, float z1, float y )
        {
            if( x1 <= x0 || z1 <= z0 ) { return; }

            int start = vertices.Count;
            vertices.Add( new Vector3( x0, y, z0 ) );
            vertices.Add( new Vector3( x0, y, z1 ) );
            vertices.Add( new Vector3( x1, y, z1 ) );
            vertices.Add( new Vector3( x1, y, z0 ) );

            triangles.Add( start );     triangles.Add( start + 1 ); triangles.Add( start + 2 );
            triangles.Add( start );     triangles.Add( start + 2 ); triangles.Add( start + 3 );
        }

        private static void ApplyMesh( ref Mesh mesh, MeshFilter meshFilter, List<Vector3> vertices, List<int> triangles )
        {
            if( meshFilter == null ) { return; }

            if( mesh == null ) { mesh = new Mesh { name = meshFilter.gameObject.name }; }
            mesh.Clear();
            mesh.SetVertices( vertices );

            // 頂点カラーを白で埋め、色はマテリアル側の設定がそのまま反映されるようにする
            var colors = new Color[vertices.Count];
            for( int i = 0; i < colors.Length; ++i ) { colors[i] = Color.white; }
            mesh.SetColors( colors );

            mesh.SetTriangles( triangles, 0 );
            mesh.RecalculateBounds();

            meshFilter.sharedMesh = mesh;
        }
    }
}
