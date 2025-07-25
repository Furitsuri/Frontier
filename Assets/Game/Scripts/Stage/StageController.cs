﻿using Frontier.Battle;
using Frontier.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Xsl;
using UnityEditor;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Stage
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class StageController : Controller
    {
        /// <summary>
        /// グリッドに対するフラグ情報
        /// </summary>
        public enum BitFlag
        {
            NONE                  = 0,
            CANNOT_MOVE           = 1 << 0,   // 移動不可グリッド
            ATTACKABLE            = 1 << 1,   // 攻撃可能なグリッド
            ATTACKABLE_TARGET     = 1 << 2,   // 攻撃対象を攻撃可能なグリッド(ATTACKABLEの内容を実質含んでいる)
            PLAYER_EXIST          = 1 << 3,   // プレイヤーキャラクターが存在
            ENEMY_EXIST           = 1 << 4,   // 敵キャラクターが存在
            OTHER_EXIST           = 1 << 5,   // 第三勢力が存在
        }

        /// <summary>
        /// キャラクターの位置を元に戻す際に使用します
        /// </summary>
        public struct Footprint
        {
            public int gridIndex;
            public Quaternion rotation;
        }

        [SerializeField]
        private GameObject _gridMeshObject;

        [SerializeField]
        private GameObject _gridCursorCtrlObject;

        [SerializeField]
        public GameObject[] _tilePrefabs;

        [SerializeField]
        public float BattlePosLengthFromCentral { get; private set; } = 2.0f;

        public bool back = true;
        private HierarchyBuilderBase _hierarchyBld = null;
        private BattleRoutineController _btlRtnCtrl;
		private StageData _stageData;
        private GridCursorController _gridCursorCtrl;
        private Footprint _footprint;
        private List<GridMesh> _gridMeshs;
        private List<int> _attackableGridIndexs;

        [Inject]
        public void Construct( HierarchyBuilderBase hierarchyBld, StageData stageData )
        {
            _hierarchyBld   = hierarchyBld;
            _stageData      = stageData;
        }

        void Awake()
        {
            TileMaterialLibrary.Init(); // タイルマテリアルの初期化

            _gridMeshs = new List<GridMesh>();
            _attackableGridIndexs = new List<int>();

            _stageData.Init(10, 10);

            // グリッド情報の初期化
            for( int x = 0; x < _stageData.GridRowNum; ++x )
            {
                for( int y = 0; y < _stageData.GridColumnNum; ++y )
                {
                    _stageData.SetTile(x, y, _hierarchyBld.InstantiateWithDiContainer<StageTileData>(false));
                    _stageData.GetTile(x, y).InstantiateTileInfo(x + y * _stageData.GridRowNum, _stageData.GridRowNum, _hierarchyBld);
                    _stageData.GetTile(x, y).InstantiateTileBhv(x, y, _tilePrefabs, _hierarchyBld);
                    _stageData.GetTile(x, y).InstantiateTileMesh(_hierarchyBld);
                }
            }

            GameObject gridCursorObject = Instantiate(_gridCursorCtrlObject);
            if (gridCursorObject != null)
            {
                _gridCursorCtrl = gridCursorObject.GetComponent<GridCursorController>();
                _gridCursorCtrl.Init(0, _stageData);
            }
        }

        /// <summary>
        /// _gridInfoの状態を基の状態に戻します
        /// </summary>
        void ResetGridInfo()
        {
            for (int i = 0; i < _stageData.GetGridToralNum(); ++i)
            {
                // _gridInfo[i] = _gridInfoBase[i].Copy();
                _stageData.TileDatas[i].CopyTileInfoBaseToOriginal();
            }
        }

        /// <summary>
        /// 移動可能なグリッドを登録します
        /// </summary>
        /// <param name="gridIndex">登録対象のグリッドインデックス</param>
        /// <param name="moveRange">移動可能範囲値</param>
        /// <param name="attackRange">攻撃可能範囲値</param>
        /// <param name="selfTag">呼び出し元キャラクターのキャラクタータグ</param>
        /// <param name="isAttackable">呼び出し元のキャラクターが攻撃可能か否か</param>
        /// <param name="isDeparture">出発グリッドから呼び出されたか否か</param>
        void RegistMoveableEachGrid(int gridIndex, int moveRange, int attackRange, int selfCharaIndex,  Character.CHARACTER_TAG selfTag, bool isAttackable, bool isDeparture = false)
        {
            // 範囲外のグリッドは考慮しない
            if (gridIndex < 0 || _stageData.GetGridToralNum() <= gridIndex) return;
            // 指定のタイル情報を取得
            var tileInfo = _stageData.GetTileInfo(gridIndex);
            if (tileInfo == null) return;
            // 移動不可のグリッドに辿り着いた場合は終了
            if (Methods.CheckBitFlag(tileInfo.flag, BitFlag.CANNOT_MOVE)) return;
            // 既に計算済みのグリッドであれば終了
            if (moveRange <= tileInfo.estimatedMoveRange) return;
            // 自身に対する敵対勢力キャラクターが存在すれば終了
            StageController.BitFlag[] opponentTag = new StageController.BitFlag[(int)Character.CHARACTER_TAG.NUM]
            {
                BitFlag.ENEMY_EXIST  | BitFlag.OTHER_EXIST,     // PLAYERにおける敵対勢力
                BitFlag.PLAYER_EXIST | BitFlag.OTHER_EXIST,     // ENEMYにおける敵対勢力
                BitFlag.PLAYER_EXIST | BitFlag.ENEMY_EXIST      // OTHERにおける敵対勢力
            };
            if (Methods.CheckBitFlag(tileInfo.flag, opponentTag[(int)selfTag])) return;

            // 現在グリッドの移動抵抗値を更新( 出発グリッドではmoveRangeの値をそのまま適応する )
            int currentMoveRange = (isDeparture) ? moveRange : tileInfo.moveResist + moveRange;
            tileInfo.estimatedMoveRange = currentMoveRange;

            // 負の値であれば終了
            if (currentMoveRange < 0) return;
            // 攻撃範囲についても登録する
            if (isAttackable && ( tileInfo.charaTag == Character.CHARACTER_TAG.NONE || tileInfo.charaIndex == selfCharaIndex) )
                RegistAttackableEachGrid(gridIndex, attackRange, selfTag, gridIndex);
            // 左端を除外
            if (gridIndex % _stageData.GridRowNum != 0)
                RegistMoveableEachGrid(gridIndex - 1, currentMoveRange, attackRange, selfCharaIndex, selfTag, isAttackable);      // gridIndexからX軸方向へ-1
            // 右端を除外
            if ((gridIndex + 1) % _stageData.GridRowNum != 0)
                RegistMoveableEachGrid(gridIndex + 1, currentMoveRange, attackRange, selfCharaIndex, selfTag, isAttackable);      // gridIndexからX軸方向へ+1
            // Z軸方向への加算と減算はそのまま
            RegistMoveableEachGrid(gridIndex - _stageData.GridRowNum, currentMoveRange, attackRange, selfCharaIndex, selfTag, isAttackable);  // gridIndexからZ軸方向へ-1
            RegistMoveableEachGrid(gridIndex + _stageData.GridRowNum, currentMoveRange, attackRange, selfCharaIndex, selfTag, isAttackable);  // gridIndexからZ軸方向へ+1
        }

        /// <summary>
        /// 頂点配列データをすべて指定の方向へ回転移動させます
        /// </summary>
        /// <param name="vertices">回転させる頂点配列データ</param>
        /// <param name="rotDirection">回転方向</param>
        /// <returns>回転させた頂点配列データ</returns>
        Vector3[] RotationVertices(Vector3[] vertices, Vector3 rotDirection)
        {
            Vector3[] ret = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                ret[i] = Quaternion.LookRotation(rotDirection) * vertices[i];
            }
            return ret;
        }

        /// <summary>
        /// 初期化を行います
        /// </summary>
        /// <param name="btlRtnCtrl">バトルマネージャ</param>
        public void Init(BattleRoutineController btlRtnCtrl)
        {
            _btlRtnCtrl = btlRtnCtrl;
        }

        /// <summary>
        /// グリッド情報を更新します
        /// </summary>
        public void UpdateGridInfo()
        {
            // 一度全てのグリッド情報を元に戻す
            ResetGridInfo();
            // キャラクターが存在するグリッドの情報を更新
            BitFlag[] flags =
            {
                BitFlag.PLAYER_EXIST,
                BitFlag.ENEMY_EXIST,
                BitFlag.OTHER_EXIST
            };

            for( int i = 0; i < (int)Character.CHARACTER_TAG.NUM; ++i )
            {
                foreach( var chara in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable((Character.CHARACTER_TAG)i))
                {
                    var gridIndex       = chara.GetCurrentGridIndex();
                    ref var tileInfo    = ref _stageData.GetTileInfo(gridIndex);
                    tileInfo.charaTag   = chara.param.characterTag;
                    tileInfo.charaIndex = chara.param.characterIndex;
                    Methods.SetBitFlag(ref tileInfo.flag, flags[i]);
                }
            }
        }

        /// <summary>
        /// 指定方向にグリッドを移動させます
        /// </summary>
        /// <param name="direction">グリッドの移動方向</param>
        /// /// <returns>グリッド移動の有無</returns>
        public bool OperateGridCursorController( Constants.Direction direction )
        {
            if (direction == Constants.Direction.FORWARD)   { _gridCursorCtrl.Up();     return true; }
            if (direction == Constants.Direction.BACK)      { _gridCursorCtrl.Down();   return true; }
            if (direction == Constants.Direction.LEFT)      { _gridCursorCtrl.Left();   return true; }
            if (direction == Constants.Direction.RIGHT)     { _gridCursorCtrl.Right();  return true; }

            return false;
        }

        /// <summary>
        /// 攻撃対象を設定します
        /// </summary>
        /// <param name="direction">グリッドの移動方向</param>
        /// <returns>グリッド移動の有無</returns>
        public bool OperateTargetSelect( Constants.Direction direction )
        {
            if (direction == Constants.Direction.FORWARD || direction == Constants.Direction.LEFT)  { _gridCursorCtrl.TransitPrevTarget(); return true; }
            if (direction == Constants.Direction.BACK || direction == Constants.Direction.RIGHT)    { _gridCursorCtrl.TransitNextTarget(); return true; }

            return false;
        }

        /// <summary>
        /// 選択グリッドを指定のキャラクターのグリッドに合わせます
        /// </summary>
        /// <param name="character">指定キャラクター</param>
        public void ApplyCurrentGrid2CharacterGrid(Character character)
        {
            _gridCursorCtrl.Index = character.GetCurrentGridIndex();
        }

        /// <summary>
        /// 2つの指定のインデックスが隣り合う座標に存在しているかを判定します
        /// </summary>
        /// <param name="fstIndex">指定インデックスその1</param>
        /// <param name="scdIndex">指定インデックスその2</param>
        /// <returns>隣り合うか否か</returns>
        public bool IsGridNextToEacheOther(int fstIndex, int scdIndex)
        {
            bool updown = (Math.Abs(fstIndex - scdIndex) == _stageData.GridRowNum);

            int fstQuotient = fstIndex / _stageData.GridColumnNum;
            int scdQuotient = scdIndex / _stageData.GridColumnNum;
            var fstRemainder = fstIndex % _stageData.GridColumnNum;
            var scdRemainder = scdIndex % _stageData.GridColumnNum;
            bool leftright = (fstQuotient == scdQuotient) && (Math.Abs(fstRemainder - scdRemainder) == 1);

            return updown || leftright;
        }

        /// <summary>
        /// グリッドに移動可能情報を登録します
        /// </summary>
        /// <param name="departIndex">移動キャラクターが存在するグリッドのインデックス値</param>
        /// <param name="moveRange">移動可能範囲値</param>
        /// <param name="attackRange">攻撃可能範囲値</param>
        /// <param name="selfTag">キャラクタータグ</param>
        /// <param name="isAttackable">攻撃可能か否か</param>
        public void RegistMoveableInfo(int departIndex, int moveRange, int attackRange, int selfCharaIndex, Character.CHARACTER_TAG selfTag, bool isAttackable)
        {
            Debug.Assert(0 <= departIndex && departIndex < _stageData.GetGridToralNum(), "StageController : Irregular Index.");

            // 移動可否情報を各グリッドに登録
            RegistMoveableEachGrid(departIndex, moveRange, attackRange, selfCharaIndex, selfTag, isAttackable, true);
        }

        /// <summary>
        /// グリッドに攻撃可能情報を登録します
        /// </summary>
        /// <param name="departIndex">攻撃キャラクターが存在するグリッドのインデックス値</param>
        /// <param name="attackRange">攻撃可能範囲値</param>
        /// <param name="selfTag">攻撃を行うキャラクター自身のキャラクタータグ</param>
        public bool RegistAttackAbleInfo(int departIndex, int attackRange, Character.CHARACTER_TAG selfTag)
        {
            Debug.Assert(0 <= departIndex && departIndex < _stageData.GetGridToralNum(), "StageController : Irregular Index.");

            _attackableGridIndexs.Clear();
            Character attackCandidate = null;

            // 全てのグリッドの攻撃可否情報を初期化
            for (int i = 0; i < _stageData.GetGridToralNum(); ++i)
            {
                Methods.UnsetBitFlag(ref _stageData.GetTileInfo(i).flag, BitFlag.ATTACKABLE);
                Methods.UnsetBitFlag(ref _stageData.GetTileInfo(i).flag, BitFlag.ATTACKABLE_TARGET);
            }

            // 攻撃可否情報を各グリッドに登録
            RegistAttackableEachGrid(departIndex, attackRange, selfTag, departIndex);

            // 攻撃可能、かつ攻撃対象となるキャラクターが存在するグリッドをリストに登録
            for (int i = 0; i < _stageData.GetGridToralNum(); ++i)
            {
                var info = _stageData.GetTileInfo(i);
                if (Methods.CheckBitFlag(info.flag, BitFlag.ATTACKABLE))
                {
                    attackCandidate = _btlRtnCtrl.BtlCharaCdr.GetCharacterFromHashtable(info.charaTag, info.charaIndex);

                    if (attackCandidate != null && attackCandidate.param.characterTag != selfTag)
                    {
                        _attackableGridIndexs.Add(i);
                    }
                }
            }

            return 0 < _attackableGridIndexs.Count;
        }

        /// <summary>
        /// 攻撃可能なグリッドを登録します
        /// </summary>
        /// <param name="gridIndex">対象のグリッドインデックス</param>
        /// <param name="attackRange">攻撃可能範囲値</param>
        /// <param name="selfTag">自身のキャラクタータグ</param>
        /// <param name="departIndex">出発グリッドインデックス</param>
        void RegistAttackableEachGrid(int gridIndex, int attackRange, Character.CHARACTER_TAG selfTag, int departIndex)
        {
            // 範囲外のグリッドは考慮しない
            if (gridIndex < 0 || _stageData.GetGridToralNum() <= gridIndex) return;
            // 移動不可のグリッドには攻撃できない
            if (Methods.CheckBitFlag(_stageData.GetTileInfo(gridIndex).flag, BitFlag.CANNOT_MOVE)) return;
            // 出発地点でなければ登録
            if (gridIndex != departIndex)
            {
                Methods.SetBitFlag(ref _stageData.GetTileInfo(gridIndex).flag, BitFlag.ATTACKABLE);
                var tileInfo = _stageData.GetTileInfo(gridIndex);

                switch (selfTag)
                {
                    case Character.CHARACTER_TAG.PLAYER:
                        if (tileInfo.charaTag == Character.CHARACTER_TAG.ENEMY ||
                            tileInfo.charaTag == Character.CHARACTER_TAG.OTHER)
                        {
                            Methods.SetBitFlag(ref _stageData.GetTileInfo(departIndex).flag, BitFlag.ATTACKABLE_TARGET);
                        }
                        break;
                    case Character.CHARACTER_TAG.ENEMY:
                        if (tileInfo.charaTag == Character.CHARACTER_TAG.PLAYER ||
                            tileInfo.charaTag == Character.CHARACTER_TAG.OTHER)
                        {
                            Methods.SetBitFlag(ref _stageData.GetTileInfo(departIndex).flag, BitFlag.ATTACKABLE_TARGET);
                        }
                        break;
                    case Character.CHARACTER_TAG.OTHER:
                        if (tileInfo.charaTag == Character.CHARACTER_TAG.PLAYER ||
                            tileInfo.charaTag == Character.CHARACTER_TAG.ENEMY)
                        {
                            Methods.SetBitFlag(ref _stageData.GetTileInfo(departIndex).flag, BitFlag.ATTACKABLE_TARGET);
                        }
                        break;
                    default:
                        break;
                }
            }

            // 負の値であれば終了
            if (--attackRange < 0) return;

            // 左端を除外
            if (gridIndex % _stageData.GridRowNum != 0)
                RegistAttackableEachGrid(gridIndex - 1, attackRange, selfTag, departIndex);       // gridIndexからX軸方向へ-1
                                                                                                  // 右端を除外
            if ((gridIndex + 1) % _stageData.GridRowNum != 0)
                RegistAttackableEachGrid(gridIndex + 1, attackRange, selfTag, departIndex);       // gridIndexからX軸方向へ+1
                                                                                                  // Z軸方向への加算と減算はそのまま
            RegistAttackableEachGrid(gridIndex - _stageData.GridRowNum, attackRange, selfTag, departIndex);   // gridIndexからZ軸方向へ-1
            RegistAttackableEachGrid(gridIndex + _stageData.GridRowNum, attackRange, selfTag, departIndex);   // gridindexからZ軸方向へ+1
        }

        /// <summary>
        /// 攻撃可能なキャラクターが存在するグリッドにグリッドカーソルの位置を設定します
        /// </summary>
        /// <param name="target">予め攻撃対象が決まっている際に指定</param>
        public void SetupGridCursorControllerToAttackCandidate(Character target = null)
        {
            // 選択グリッドを自動的に攻撃可能キャラクターの存在するグリッドインデックスに設定
            if (0 < _attackableGridIndexs.Count)
            {
                _gridCursorCtrl.SetAtkTargetNum(_attackableGridIndexs.Count);

                // 攻撃対象が既に決まっている場合は対象を探す
                if (target != null && 1 < _attackableGridIndexs.Count)
                {
                    for (int i = 0; i < _attackableGridIndexs.Count; ++i)
                    {
                        var info = GetGridInfo(_attackableGridIndexs[i]);

                        Character chara = _btlRtnCtrl.BtlCharaCdr.GetCharacterFromHashtable(info.charaTag, info.charaIndex);

                        if (target == chara)
                        {
                            _gridCursorCtrl.SetAtkTargetIndex(i);
                            break;
                        }
                    }
                }
                else
                {
                    _gridCursorCtrl.SetAtkTargetIndex(0);
                }
            }
        }

        /// <summary>
        /// 攻撃可能グリッドのうち、攻撃可能キャラクターが存在するグリッドをリストに登録します
        /// </summary>
        /// <param name="targetTag">攻撃対象のタグ</param>
        /// <param name="target">予め攻撃対象が決まっている際に指定</param>
        /// <returns>攻撃可能キャラクターが存在している</returns>
        public bool RegistAttackTargetGridIndexs(Character.CHARACTER_TAG targetTag, Character target = null)
        {
            Character character = null;

            _gridCursorCtrl.ClearAtkTargetInfo();
            _attackableGridIndexs.Clear();

            // 攻撃可能、かつ攻撃対象となるキャラクターが存在するグリッドをリストに登録
            for (int i = 0; i < _stageData.GetGridToralNum(); ++i)
            {
                var info = _stageData.GetTileInfo(i);
                if (Methods.CheckBitFlag(info.flag, BitFlag.ATTACKABLE))
                {
                    character = _btlRtnCtrl.BtlCharaCdr.GetCharacterFromHashtable(info.charaTag, info.charaIndex);

                    if (character != null && character.param.characterTag == targetTag)
                    {
                        _attackableGridIndexs.Add(i);
                    }
                }
            }

            // 選択グリッドを自動的に攻撃可能キャラクターの存在するグリッドインデックスに設定
            if (0 < _attackableGridIndexs.Count)
            {
                _gridCursorCtrl.SetAtkTargetNum(_attackableGridIndexs.Count);

                // 攻撃対象が既に決まっている場合は対象を探す
                if (target != null && 1 < _attackableGridIndexs.Count)
                {
                    for (int i = 0; i < _attackableGridIndexs.Count; ++i)
                    {
                        var info = GetGridInfo(_attackableGridIndexs[i]);

                        Character chara = _btlRtnCtrl.BtlCharaCdr.GetCharacterFromHashtable(info.charaTag, info.charaIndex);

                        if (target == chara)
                        {
                            _gridCursorCtrl.SetAtkTargetIndex(i);
                            break;
                        }
                    }
                }
                else
                {
                    _gridCursorCtrl.SetAtkTargetIndex(0);
                }
            }

            return 0 < _attackableGridIndexs.Count;
        }

        /// <summary>
        /// 移動可能グリッドを描画します
        /// </summary>
        /// <param name="departIndex">移動キャラクターが存在するグリッドのインデックス値</param>
        /// <param name="moveableRange">移動可能範囲値</param>
        /// <param name="attackableRange">攻撃可能範囲値</param>
        public void DrawMoveableGrids(int departIndex, int moveableRange, int attackableRange)
        {
            Debug.Assert( 0 <= departIndex && departIndex < _stageData.GetGridToralNum(), "StageController : Irregular Index." );

            int count = 0;

            // 3つの条件毎に異なるメッシュタイプやデバッグ表示があるため、
            // for文で判定するためにそれぞれを配列化
            GridMesh.MeshType[] meshTypes =
            {
                GridMesh.MeshType.ATTACKABLE_TARGET,
                GridMesh.MeshType.MOVE,
                GridMesh.MeshType.ATTACK
            };

            string[] dbgStrs =
            {
                "Attackable Target Grid Index : ",
                "Moveable Grid Index : ",
                "Attackable Grid Index : "
            };

            // グリッドの状態をメッシュで描画
            for (int i = 0; i < _stageData.GetGridToralNum(); ++i)
            {
                var info = _stageData.GetTileInfo(i);

                bool[] conditions =
                {
                    Methods.CheckBitFlag(info.flag, BitFlag.ATTACKABLE_TARGET),
                    (0 <= info.estimatedMoveRange),
                    Methods.CheckBitFlag(info.flag, BitFlag.ATTACKABLE)
                };

                for( int j = 0; j < meshTypes.Length; ++j )
                {
                    if( conditions[j] )
                    {
                        var gridMesh = _hierarchyBld.CreateComponentAndOrganize<GridMesh>(_gridMeshObject, true);
                        NullCheck.AssertNotNull(gridMesh);
                        if( gridMesh == null ) continue;
                     
                        _gridMeshs.Add(gridMesh);
                        _gridMeshs[count++].DrawGridMesh(info.charaStandPos, TILE_SIZE, meshTypes[j]);
                        Debug.Log(dbgStrs[j] + i);

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 攻撃可能グリッドを描画します
        /// </summary>
        /// <param name="departIndex">攻撃キャラクターが存在するグリッドのインデックス値</param>
        public void DrawAttackableGrids(int departIndex)
        {
            Debug.Assert(0 <= departIndex && departIndex < _stageData.GetGridToralNum(), "StageController : Irregular Index.");

            int count = 0;
            // グリッドの状態をメッシュで描画
            for (int i = 0; i < _stageData.GetGridToralNum(); ++i)
            {
                if (Methods.CheckBitFlag(_stageData.GetTileInfo(i).flag, BitFlag.ATTACKABLE))
                {
                    var gridMesh = _hierarchyBld.CreateComponentAndOrganize<GridMesh>(_gridMeshObject, true);
                    NullCheck.AssertNotNull(gridMesh);
                    if (gridMesh == null) continue;

                    _gridMeshs.Add(gridMesh);
                    _gridMeshs[count++].DrawGridMesh(_stageData.GetTileInfo(i).charaStandPos, TILE_SIZE, GridMesh.MeshType.ATTACK);

                    Debug.Log("Attackable Grid Index : " + i);
                }
            }
        }

        /// <summary>
        /// 全てのグリッドにおける指定のビットフラグの設定を解除します
        /// </summary>
        public void UnsetGridsBitFlag(BitFlag value)
        {
            // 全てのグリッドの移動・攻撃可否情報を初期化
            for (int i = 0; i < _stageData.GetGridToralNum(); ++i)
            {
                Methods.UnsetBitFlag(ref _stageData.GetTileInfo(i).flag, value);
            }
        }

        /// <summary>
        /// 全てのグリッドメッシュの描画を消去します
        /// </summary>
        public void ClearGridMeshDraw()
        {
            foreach (var grid in _gridMeshs)
            {
                grid.ClearDraw();
                grid.Remove();
            }
            _gridMeshs.Clear();
        }

        /// <summary>
        /// 攻撃可能情報を消去します
        /// </summary>
        public void ClearAttackableInfo()
        {
            UnsetGridsBitFlag( BitFlag.ATTACKABLE );
            _attackableGridIndexs.Clear();
        }

        /// <summary>
        /// グリッドメッシュにこのクラスを登録します
        /// グリッドメッシュクラスが生成されたタイミングでグリッドメッシュ側から呼び出されます
        /// </summary>
        /// <param name="script">グリッドメッシュクラスのスクリプト</param>
        public void AddGridMeshToList(GridMesh script)
        {
            _gridMeshs.Add(script);
        }

        /// <summary>
        /// 縦軸と横軸のグリッド数を取得します
        /// </summary>
        /// <returns>縦軸と横軸のグリッド数</returns>
        public (int, int) GetGridNumsXZ()
        {
            return (_stageData.GridRowNum, _stageData.GridColumnNum);
        }

        /// <summary>
        /// 指定グリッドにおけるキャラクターのワールド座標を取得します
        /// </summary>
        /// <param name="index">指定グリッド</param>
        /// <returns>グリッドにおける中心ワールド座標</returns>
        public Vector3 GetGridCharaStandPos(int index)
        {
            return _stageData.GetTileInfo(index).charaStandPos;
        }

        /// <summary>
        /// グリッドカーソルのインデックス値を取得します
        /// </summary>
        /// <returns>現在の選択グリッドのインデックス値</returns>
        public int GetCurrentGridIndex()
        {
            return _gridCursorCtrl.Index;
        }

        /// <summary>
        /// グリッドカーソルの状態を取得します
        /// </summary>
        /// <returns>現在の選択グリッドの状態</returns>
        public GridCursorController.State GetGridCursorControllerState()
        {
            return _gridCursorCtrl.GridState;
        }

        /// <summary>
        /// グリッドカーソルがバインドしているキャラクターを取得します
        /// </summary>
        /// <returns>バインドしているキャラクター(存在しない場合はnull)</returns>
        public Character GetGridCursorControllerBindCharacter()
        {
            return _gridCursorCtrl.BindCharacter;
        }

        /// <summary>
        /// グリッドカーソルにキャラクターをバインドします
        /// </summary>
        /// <param name="state">バインドタイプ</param>
        /// <param name="bindCharacter">バインド対象のキャラクター</param>
        public void BindGridCursorControllerState( GridCursorController.State state, Character bindCharacter )
        {
            _gridCursorCtrl.GridState       = state;
            _gridCursorCtrl.BindCharacter   = bindCharacter;
        }

        /// <summary>
        /// 選択グリッドのアクティブ状態を設定します
        /// </summary>
        /// <param name="isActive">設定するアクティブ状態</param>
        public void SetGridCursorControllerActive( bool isActive )
        {
            _gridCursorCtrl.SetActive( isActive );
        }

        /// <summary>
        /// グリッドカーソルのキャラクターバインドを解除します
        /// </summary>
        public void ClearGridCursroBind()
        {
            if (_gridCursorCtrl.BindCharacter != null)
            {
                _gridCursorCtrl.BindCharacter.gameObject.SetLayerRecursively(LayerMask.NameToLayer(Constants.LAYER_NAME_CHARACTER));
            }

            _gridCursorCtrl.GridState       = GridCursorController.State.NONE;
            _gridCursorCtrl.BindCharacter   = null;
        }

        /// <summary>
        /// 現在選択しているグリッドの情報を取得します
        /// 攻撃対象選択状態では選択している攻撃対象が存在するグリッド情報を取得します
        /// </summary>
        /// <param name="gridInfo">該当するグリッドの情報</param>
        public void FetchCurrentGridInfo(out GridInfo gridInfo)
        {
            int index = 0;

            if (_gridCursorCtrl.GridState == GridCursorController.State.ATTACK)
            {
                index = _attackableGridIndexs[_gridCursorCtrl.GetAtkTargetIndex()];
            }
            else
            {
                index = _gridCursorCtrl.Index;
            }

            gridInfo = _stageData.GetTileInfo(index);
        }

        /// <summary>
        /// 指定インデックスのグリッド情報を取得します
        /// </summary>
        /// <param name="index">指定するインデックス値</param>
        /// <returns>指定インデックスのグリッド情報</returns>
        public ref GridInfo GetGridInfo(int index)
        {
            return ref _stageData.GetTileInfo(index);
        }

        /// <summary>
        /// グリッドのメッシュの描画の切替を行います
        /// </summary>
        /// <param name="isDisplay">描画するか否か</param>
        public void ToggleMeshDisplay(bool isDisplay)
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = isDisplay;
            }
        }

        /// <summary>
        /// 出発地点と目的地から移動経路となるグリッドのインデックスリストを取得します
        /// </summary>
        /// <param name="departGridIndex">出発地グリッドのインデックス</param>
        /// <param name="destGridIndex">目的地グリッドのインデックス</param>
        public List<(int routeIndexs, int routeCost)> ExtractShortestRouteIndexs(int departGridIndex, int destGridIndex, in List<int> candidateRouteIndexs)
        {
            Dijkstra dijkstra = new Dijkstra(candidateRouteIndexs.Count);

            // 出発グリッドからのインデックスの差を取得
            for (int i = 0; i + 1 < candidateRouteIndexs.Count; ++i)
            {
                for (int j = i + 1; j < candidateRouteIndexs.Count; ++j)
                {
                    int diff = candidateRouteIndexs[j] - candidateRouteIndexs[i];
                    if ((diff == -1 && (candidateRouteIndexs[i] % _stageData.GridRowNum != 0)) ||                                 // 左に存在(左端を除く)
                        (diff == 1 && (candidateRouteIndexs[i] % _stageData.GridRowNum != _stageData.GridRowNum - 1)) ||    // 右に存在(右端を除く)
                         Math.Abs(diff) == _stageData.GridRowNum)                                                                 // 上または下に存在
                    {
                        // 移動可能な隣接グリッド情報をダイクストラに入れる
                        dijkstra.Add(i, j);
                        dijkstra.Add(j, i);
                    }
                }
            }

            // ダイクストラから出発グリッドから目的グリッドまでの最短経路を得る
            return dijkstra.GetMinRoute(candidateRouteIndexs.IndexOf(departGridIndex), candidateRouteIndexs.IndexOf(destGridIndex), candidateRouteIndexs);
        }

        /// <summary>
        /// キャラクターの位置及び向きを保持します
        /// </summary>
        /// <param name="footprint">保持する値</param>
        public void LeaveFootprint(Footprint footprint)
        {
            _footprint = footprint;
        }

        /// <summary>
        /// 保持していた位置及び向きを指定のキャラクターに設定します
        /// </summary>
        /// <param name="character">指定するキャラクター</param>
        public void FollowFootprint(Character character)
        {
            _gridCursorCtrl.Index = _footprint.gridIndex;
            character.SetCurrentGridIndex(_footprint.gridIndex);
            GridInfo info;
            FetchCurrentGridInfo(out info);
            character.transform.position = info.charaStandPos;
            character.transform.rotation = _footprint.rotation;
        }

        /// <summary>
        /// 指定されたインデックス間のグリッド長を返します
        /// </summary>
        /// <param name="fromIndex">始点インデックス</param>
        /// <param name="toIndex">終点インデックス</param>
        /// <returns>グリッド長</returns>
        public float CalcurateGridLength(int fromIndex, int toIndex)
        {
            var from    = _stageData.GetTileInfo(fromIndex).charaStandPos;
            var to      = _stageData.GetTileInfo(toIndex).charaStandPos;
            var gridLength = (from - to).magnitude / TILE_SIZE;

            return gridLength;
        }

        /*
#if UNITY_EDITOR
        [UnityEditor.CustomEditor(typeof(StageController))]
        public class StageControllerEditor : UnityEditor.Editor
        {
            override public void OnInspectorGUI()
            {
                StageController script = target as StageController;

                // ステージ情報からサイズを決める際はサイズ編集を不可にする
                EditorGUI.BeginDisabledGroup(script.isAdjustStageScale);
                script._stageData.SetGridRowNum( EditorGUILayout.IntField("X方向グリッド数", script._stageData.GridRowNum) );
                script._stageData.SetGridColumnNum( EditorGUILayout.IntField("Z方向グリッド数", script._stageData.GridColumnNum) );
                EditorGUI.EndDisabledGroup();

                base.OnInspectorGUI();
            }
        }
#endif // UNITY_EDITOR
        */
    }
}