using UnityEngine;

namespace Frontier.Registries
{
    public class PrefabRegistry : MonoBehaviour
    {
        [Header( "味方キャラクター" )]
        [SerializeField] private GameObject[] _playerGameObjects;

        [Header( "敵キャラクター" )]
        [SerializeField] private GameObject[] _enemyGameObjects;

        [Header( "第三勢力キャラクター" )]
        [SerializeField] private GameObject[] _otherGameObjects;

        [Header( "Battle関連" )]
        [SerializeField] private GameObject _battleFileLoaderObject;    // BattleFileLoaderプレハブ

        [Header( "Stage関連" )]
        [SerializeField] private GameObject _stageFileLoaderPrefab;     // StageFileLoaderプレハブ
        [SerializeField] private GameObject _gridMeshObject;            // GridMeshプレハブ
        [SerializeField] private GameObject _gridCursorCtrlObject;      // GridCursorCtrlプレハブ
        [SerializeField] private GameObject tileMeshObject;             // TileMeshプレハブ
        [SerializeField] private GameObject[] _tilePrefabs;             // タイルプレハブ配列

        public GameObject[] PlayerPrefabs => _playerGameObjects;
        public GameObject[] EnemyPrefabs => _enemyGameObjects;
        public GameObject[] OtherPrefabs => _otherGameObjects;
        public GameObject BattleFileLoaderPrefab => _battleFileLoaderObject;
        public GameObject StageFileLoaderPrefab => _stageFileLoaderPrefab;
        public GameObject GridMeshPrefab => _gridMeshObject;
        public GameObject GridCursorCtrlPrefab => _gridCursorCtrlObject;
        public GameObject TileMeshPrefab => tileMeshObject;
        public GameObject[] TilePrefabs => _tilePrefabs;
    }
}