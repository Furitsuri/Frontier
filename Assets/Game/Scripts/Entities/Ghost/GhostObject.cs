using UnityEngine;

namespace Frontier.Entities
{
    public class GhostObject : MonoBehaviour
    {
        public int TileIndex { get; set; } = -1;

        public static GhostObject Create( Transform source, string name )
        {
            var ghostGO = new GameObject( $"{name}_Ghost" );
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
                child.AddComponent<MeshRenderer>().materials = CreateMaterials( smr.sharedMaterials );
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
                child.AddComponent<MeshRenderer>().materials = CreateMaterials( mr.sharedMaterials );
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

        private static Material[] CreateMaterials( Material[] originals )
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
                Color c = mat.color;
                c.a = 0.5f;
                mat.color = c;
                mats[i] = mat;
            }
            return mats;
        }
    }
}
