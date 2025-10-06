using Frontier.Stage;
using UnityEngine;

namespace Froniter.Registries
{
    public class PrefabRegistry : MonoBehaviour
    {
        [SerializeField] private GameObject tileMeshObject;     // TileMeshプレハブ

        public GameObject TileMeshPrefab => tileMeshObject;
    }
}