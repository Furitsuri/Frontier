using UnityEngine;

namespace Frontier.Registries
{
    public class PrefabRegistry : MonoBehaviour
    {
        [Header( "Battle関連" )]
        [SerializeField] private GameObject battleFileLoaderObject; // BattleFileLoaderプレハブ

        [Header( "Stage関連" )]
        [SerializeField] private GameObject stageFileLoaderPrefab;  // StageFileLoaderプレハブ
        [SerializeField] private GameObject _gridMeshObject;        // GridMeshプレハブ
        [SerializeField] private GameObject _gridCursorCtrlObject;  // GridCursorCtrlプレハブ
        [SerializeField] private GameObject tileMeshObject;         // TileMeshプレハブ
        [SerializeField] private GameObject[] _tilePrefabs;         // タイルプレハブ配列

        public GameObject BattleFileLoaderPrefab => battleFileLoaderObject;
        public GameObject StageFileLoaderPrefab => stageFileLoaderPrefab;
        public GameObject GridMeshPrefab => _gridMeshObject;
        public GameObject GridCursorCtrlPrefab => _gridCursorCtrlObject;
        public GameObject TileMeshPrefab => tileMeshObject;
        public GameObject[] TilePrefabs => _tilePrefabs;
    }
}