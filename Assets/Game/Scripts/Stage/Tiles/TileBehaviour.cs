using Frontier.Stage;
using UnityEngine;

namespace Frontier.Stage
{
    public class TileBehaviour : MonoBehaviour
    {
        private MeshRenderer _renderer;
        void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();

            // タイルのデフォルトスケールを設定
            transform.localScale = TileMaterialLibrary.GetDefaultTileScale();
        }

        public void ApplyTileType(TileType type)
        {
            _renderer.material = TileMaterialLibrary.GetMaterial(type);
            // transform.localScale = TileMaterialLibrary.GetScale(type); // 高さ表現もここで調整
        }
    }
}