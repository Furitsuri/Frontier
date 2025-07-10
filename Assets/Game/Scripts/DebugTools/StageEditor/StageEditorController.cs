using Frontier.Stage;
using System;
using System.ComponentModel;
using Unity.Mathematics;
using UnityEngine;
using Zenject;
using static Constants;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    /// <summary>
    /// ステージエディターのコントローラー
    /// </summary>
    public class StageEditorController : MonoBehaviour
    {
        [Header("Editor Settings")]
        public int row    = 10;
        public int column = 10;

        [Header("Prefabs")]
        [SerializeField]
        public GameObject[] tilePrefabs;
        [SerializeField]
        public GameObject cursorPrefab;
        [SerializeField]
        private EditParamPresenter EditParamView;

        private InputFacade _inputFcd;
        private HierarchyBuilderBase _hierarchyBld;
        private TileBehaviour[,] tileObjects;
        private StageData _stageData;
        private StageMesh _stageMesh;
        private GridCursorController _gridCursorCtrl;
        private int selectedType = 0;
        private int selectedHeight = 0;

        private Vector3 offset = new Vector3(0, 5, -5); // ターゲットからの相対位置

        private Camera _mainCamera;

        [Inject]
        public void Construct(InputFacade inputFacade, HierarchyBuilderBase hierarchyBld)
        {
            _inputFcd       = inputFacade;
            _hierarchyBld   = hierarchyBld;
        }

        private void Start()
        {
            _inputFcd.Init();           // 入力ファサードの初期化
            TileMaterialLibrary.Init(); // タイルマテリアルの初期化

            CreateStage();
            CreateCursor();
            RegistInputCodes();         // 入力コードの登録

            _mainCamera = Camera.main;
        }

        private void CreateStage()
        {
            if (_stageData == null)
            {
                _stageData = _hierarchyBld.InstantiateWithDiContainer<StageData>(true);
            }
            _stageData.Init(row, column);

            for (int y = 0; y < column; y++)
            {
                for (int x = 0; x < row; x++)
                {
                    _stageData.SetTile(x, y, _hierarchyBld.InstantiateWithDiContainer<StageTileData>(false));
                    _stageData.GetTile(x, y).InstantiateTileInfo(x + y * _stageData.GridRowNum, _stageData.GridRowNum);
                    _stageData.GetTile(x, y).InstantiateTileBhv(x, y, tilePrefabs);
                }
            }

            if( _stageMesh == null )
            {
                _stageMesh = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<StageMesh>(true, false, "StageMesh");
            }
            _stageMesh.Init(true);
            _stageMesh.DrawMesh();
        }

        private void CreateCursor()
        {
            var cursorObj = Instantiate(cursorPrefab, Vector3.zero, Quaternion.identity);
            _gridCursorCtrl = cursorObj.GetComponent<GridCursorController>();
            _gridCursorCtrl.Init(0, _stageData);
        }

        private void Update()
        {
            UpdateCamera(_gridCursorCtrl.X(), _gridCursorCtrl.Y());
            UpdateTileVisual(_gridCursorCtrl.X(), _gridCursorCtrl.Y());
            EditParamView.UpdateText(selectedType, selectedHeight);
        }

        private void RegistInputCodes()
        {
            _inputFcd.RegisterInputCodes(
                (GuideIcon.ALL_CURSOR,  "SELECT",       CanAcceptDirection,     new AcceptDirectionInput(AcceptDirection),  0.1f),
                (GuideIcon.CONFIRM,     "CONFIRM",      CanAcceptConfirm,       new AcceptBooleanInput(AcceptConfirm),      0.0f),
                (GuideIcon.SUB1,        "SUB TILE NUM", CanAcceptSub,           new AcceptBooleanInput(AcceptSub1),         0.0f),
                (GuideIcon.SUB2,        "ADD TILE NUM", CanAcceptSub,           new AcceptBooleanInput(AcceptSub2),         0.0f),
                (GuideIcon.SUB3,        "SUB HEIGHT",   CanAcceptSub,           new AcceptBooleanInput(AcceptSub3),         0.0f),
                (GuideIcon.SUB4,        "ADD HEIGHT",   CanAcceptSub,           new AcceptBooleanInput(AcceptSub4),         0.0f)
            );
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.S)) StageDataSerializer.Save(_stageData, "test_stage");
            if (Input.GetKeyDown(KeyCode.L)) LoadStage("test_stage");
        }

        private void PlaceTile(int x, int y)
        {
            Destroy(tileObjects[x, y]);
            
            _stageData.SetTile(x, y, _hierarchyBld.InstantiateWithDiContainer<StageTileData>(false));
            _stageData.GetTile(x, y).SetTileTypeAndHeight((TileType)selectedType, selectedHeight);
            _stageData.GetTile(x, y).InstantiateTileBhv(x, y, tilePrefabs);

            var tileBhv = tileObjects[x, y].GetComponent<TileBehaviour>();
            if (tileBhv != null)
            {
                tileBhv.ApplyTileType((TileType)selectedType);
            }
        }

        private void UpdateCamera(int x, int y)
        {
            if (_mainCamera == null) return;
            // カメラの位置を更新
            Vector3 targetPosition = _gridCursorCtrl.GetPosition() + offset;
            _mainCamera.transform.position = targetPosition;
            _mainCamera.transform.LookAt(_gridCursorCtrl.GetPosition());
        }

        private void UpdateTileVisual(int x, int y)
        {
            // 選択中タイルの見た目の強調表示など
        }

        private void LoadStage(string fileName)
        {
            var data = StageDataSerializer.Load(fileName);
            if (data == null) return;

            // 簡易的に再ロード
            foreach (Transform child in transform) Destroy(child.gameObject);
            _stageData  = data;
            row         = _stageData.GridRowNum;
            column      = _stageData.GridColumnNum;
            CreateStage();

            /*
            for (int y = 0; y < column; y++)
            {
                for (int x = 0; x < row; x++)
                {
                    var tile = data.GetTile(x, y);
                    Destroy(tileObjects[x, y]);
                    SpawnTile(x, y);
                }
            }
            */
        }

        private bool CanAcceptDirection()
        {
            return true;
        }

        private bool CanAcceptConfirm()
        {
            return true;
        }

        /// <summary>
        /// サブ1の入力の受付可否を判定します
        /// </summary>
        /// <returns>サブ1の入力の受付可否</returns>
        private bool CanAcceptSub()
        {
            return true;
        }

        private bool AcceptDirection(Direction dir)
        {

            if (dir == Direction.NONE) return false;

            if (dir == Direction.RIGHT)         _gridCursorCtrl.Right();
            else if (dir == Direction.LEFT)     _gridCursorCtrl.Left();
            else if (dir == Direction.FORWARD)  _gridCursorCtrl.Up();
            else if (dir == Direction.BACK)     _gridCursorCtrl.Down();

            return true;
        }

        /// <summary>
        /// 決定入力を受け取った際の処理を行います
        /// </summary>
        /// <param name="isInput">決定入力</param>
        /// <returns>入力実行の有無</returns>
        private bool AcceptConfirm(bool isInput)
        {
            if (isInput)
            {
                PlaceTile(_gridCursorCtrl.X(), _gridCursorCtrl.Y());

                return true;
            }

            return false;
        }

        private bool AcceptSub1(bool isInput)
        {
            if (isInput)
            {
                selectedType = Math.Clamp(selectedType - 1, 0, (int)TileType.NUM);

                return true;
            }
            
            return false;
        }

        private bool AcceptSub2(bool isInput)
        {
            if (isInput)
            {
                selectedType = Math.Clamp( selectedType + 1, 0, (int)TileType.NUM );

                return true;
            }

            return false;
        }

        private bool AcceptSub3(bool isInput)
        {
            if (isInput)
            {
                selectedHeight = Mathf.Clamp(selectedHeight - 1, 0, 5);

                return true;
            }

            return false;
        }

        private bool AcceptSub4(bool isInput)
        {
            if (isInput)
            {
                selectedHeight = Mathf.Clamp(selectedHeight + 1, 0, 5);

                return true;
            }

            return false;
        }
    }
} // namespace Frontier.DebugTools.StageEditor

#endif // UNITY_EDITOR