using UnityEngine;

namespace Frontier.Entities
{
    public class GhostObject : MonoBehaviour
    {
        /// <summary>
        /// ゴーストの見た目の種類です
        /// </summary>
        public enum Style
        {
            Future = 0,     // 移動先など「これから」の位置を示す(元の色のまま半透明)
            Afterimage,     // 移動前など「過去」の位置を示す残像(色を抜いたモノクロで、より薄い半透明)
        }

        private const float FUTURE_ALPHA            = 0.5f;
        private const float AFTERIMAGE_ALPHA        = 0.35f;
        private const float AFTERIMAGE_BRIGHTNESS   = 0.85f;

        public int TileIndex { get; set; } = -1;

        public static GhostObject Create( Transform source, string name, Style style = Style.Future )
        {
            var ghostGO = new GameObject( style == Style.Afterimage ? $"{name}_Afterimage" : $"{name}_Ghost" );
            var ghost   = ghostGO.AddComponent<GhostObject>();

            // SkinnedMeshRenderer: 現在の骨格ポーズをベイクして静的メッシュとして複製
            foreach( var smr in source.GetComponentsInChildren<SkinnedMeshRenderer>() )
            {
                var child = new GameObject( smr.gameObject.name );
                child.transform.SetParent( ghostGO.transform, false );
                child.transform.localPosition = source.InverseTransformPoint( smr.transform.position );
                child.transform.localRotation = Quaternion.Inverse( source.rotation ) * smr.transform.rotation;
                child.transform.localScale    = new Vector3(
                    smr.transform.lossyScale.x / Mathf.Max( source.lossyScale.x, float.Epsilon ),
                    smr.transform.lossyScale.y / Mathf.Max( source.lossyScale.y, float.Epsilon ),
                    smr.transform.lossyScale.z / Mathf.Max( source.lossyScale.z, float.Epsilon )
                );
                var baked = new Mesh();
                smr.BakeMesh( baked );
                child.AddComponent<MeshFilter>().sharedMesh  = baked;
                child.AddComponent<MeshRenderer>().materials = CreateMaterials( smr.sharedMaterials, style );
            }

            // MeshRenderer: アセットメッシュをコピーしてランタイムオブジェクトとして複製
            foreach( var mr in source.GetComponentsInChildren<MeshRenderer>() )
            {
                var mf = mr.GetComponent<MeshFilter>();
                if( mf == null || mf.sharedMesh == null ) { continue; }

                var child = new GameObject( mr.gameObject.name );
                child.transform.SetParent( ghostGO.transform, false );
                child.transform.localPosition = source.InverseTransformPoint( mr.transform.position );
                child.transform.localRotation = Quaternion.Inverse( source.rotation ) * mr.transform.rotation;
                child.transform.localScale    = new Vector3(
                    mr.transform.lossyScale.x / Mathf.Max( source.lossyScale.x, float.Epsilon ),
                    mr.transform.lossyScale.y / Mathf.Max( source.lossyScale.y, float.Epsilon ),
                    mr.transform.lossyScale.z / Mathf.Max( source.lossyScale.z, float.Epsilon )
                );
                child.AddComponent<MeshFilter>().sharedMesh  = Object.Instantiate( mf.sharedMesh );
                child.AddComponent<MeshRenderer>().materials = CreateMaterials( mr.sharedMaterials, style );
            }

            return ghost;
        }

        /// <summary>
        /// ベイクした Mesh と生成したマテリアルを明示的に解放し、ゴーストオブジェクトを破棄します。
        /// </summary>
        public void Cleanup()
        {
            foreach( var mf in GetComponentsInChildren<MeshFilter>() )
            {
                if( mf.sharedMesh != null ) { Object.Destroy( mf.sharedMesh ); }
            }
            foreach( var mr in GetComponentsInChildren<MeshRenderer>() )
            {
                foreach( var mat in mr.sharedMaterials )
                {
                    if( mat != null ) { Object.Destroy( mat ); }
                }
            }
            Object.Destroy( gameObject );
        }

        private static Material[] CreateMaterials( Material[] originals, Style style )
        {
            var mats = new Material[originals.Length];
            for( int i = 0; i < originals.Length; i++ )
            {
                var mat = new Material( originals[i] );
                mat.SetFloat( "_Surface", 1 );
                mat.SetOverrideTag( "RenderType", "Transparent" );
                mat.SetInt( "_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha );
                mat.SetInt( "_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha );
                mat.SetInt( "_ZWrite", 0 );
                mat.DisableKeyword( "_ALPHATEST_ON" );
                mat.EnableKeyword( "_SURFACE_TYPE_TRANSPARENT" );
                mat.EnableKeyword( "_ALPHABLEND_ON" );
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                if( style == Style.Afterimage )
                {
                    // テクスチャ(色情報)を外して単色にすることで、陰影のみが残るモノクロの残像にする
                    mat.mainTexture = null;
                    mat.DisableKeyword( "_EMISSION" );
                    mat.color = new Color( AFTERIMAGE_BRIGHTNESS, AFTERIMAGE_BRIGHTNESS, AFTERIMAGE_BRIGHTNESS, AFTERIMAGE_ALPHA );
                }
                else
                {
                    Color c = mat.color;
                    c.a = FUTURE_ALPHA;
                    mat.color = c;
                }
                mats[i] = mat;
            }
            return mats;
        }
    }
}
