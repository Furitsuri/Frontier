using Frontier.Entities;
using UnityEngine;
using static Constants;

#pragma warning disable 0618

namespace Frontier.Stage
{
    public class GridCursorController : MonoBehaviour
    {
        /// <summary>
        /// グリッドカーソルの状態
        /// </summary>
        public enum State
        {
            NONE = 0,
            MOVE,
            ATTACK
        }

        [Header("移動補間時間")]
        [SerializeField]
        private float MoveInterpolationTime = 1f;

        private LineRenderer _lineRenderer;
        private StageData _stageData    = null;
        private Vector3 _beginPos       = Vector3.zero;
        private Vector3 _endPos         = Vector3.zero;
        private Vector3 _currentPos     = Vector3.zero;
        private int _atkTargetIndex     = 0;
        private int _atkTargetNum       = 0;
        private float _totalTime        = 0;

        public int Index { get; set; } = 0;
        public State GridState { get; set; } = State.NONE;
        public Character BindCharacter { get; set; } = null;

        private void Start()
        {
            _lineRenderer = gameObject.GetComponent<LineRenderer>();
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        /// <param name="initIndex">初期インデックス値</param>
        /// <param name="rowNum">盤面における行に該当するグリッド数</param>
        /// <param name="columnNum">盤面における列に該当するグリッド数</param>
        public void Init(int initIndex, StageData stageData)
        {
            Index           = initIndex;
            _stageData      = stageData;
            _atkTargetIndex = 0;
            _atkTargetNum   = 0;
            GridState       = State.NONE;
            BindCharacter   = null;
        }

        /// <summary>
        /// インデックス値を現在グリッドの上に該当する値に設定します
        /// </summary>
        public void Up()
        {
            var rowNum = _stageData.GridRowNum;

            StartLerpMove();
            Index += rowNum;
            if (rowNum * rowNum <= Index)
            {
                Index = Index % (rowNum * rowNum);
            }
        }

        /// <summary>
        /// インデックス値を現在グリッドの下に該当する値に設定します
        /// </summary>
        public void Down()
        {
            var rowNum = _stageData.GridRowNum;

            StartLerpMove();
            Index -= rowNum;
            if (Index < 0)
            {
                Index += rowNum * rowNum;
            }
        }

        /// <summary>
        /// インデックス値を現在グリッドの右に該当する値に設定します
        /// </summary>
        public void Right()
        {
            var rowNum = _stageData.GridRowNum;

            StartLerpMove();
            Index++;
            if (Index % rowNum == 0)
            {
                Index -= rowNum;
            }
        }

        /// <summary>
        /// インデックス値を現在グリッドの左に該当する値に設定します
        /// </summary>
        public void Left()
        {
            var rowNum = _stageData.GridRowNum;

            StartLerpMove();
            Index--;
            if ((Index + 1) % rowNum == 0)
            {
                Index += rowNum;
            }
        }

        /// <summary>
        /// 攻撃対象インデックス値を取得します
        /// </summary>
        /// <returns>攻撃対象インデックス値</returns>
        public int GetAtkTargetIndex()
        {
            return _atkTargetIndex;
        }

        /// <summary>
        /// オブジェクトのアクティブ・非アクティブを設定します
        /// </summary>
        /// <param name="isActive">アクティブ設定</param>
        public void SetActive(bool isActive)
        {
            gameObject.SetActive(isActive);
        }

        /// <summary>
        /// 攻撃対象インデックス値を設定します
        /// </summary>
        /// <param name="index">攻撃対象インデックス値</param>
        public void SetAtkTargetIndex(int index)
        {
            _atkTargetIndex = index;
        }

        /// <summary>
        /// 攻撃対象インデックスの総数を設定します
        /// </summary>
        /// <param name="num">攻撃対象インデックスの総数</param>
        public void SetAtkTargetNum(int num)
        {
            _atkTargetNum = num;
        }

        /// <summary>
        /// 次のターゲットインデックス値に遷移します
        /// </summary>
        public void TransitNextTarget()
        {
            _atkTargetIndex = (_atkTargetIndex + 1) % _atkTargetNum;
        }

        /// <summary>
        /// 前のターゲットインデックス値に遷移します
        /// </summary>
        public void TransitPrevTarget()
        {
            _atkTargetIndex = (_atkTargetIndex - 1) < 0 ? _atkTargetNum - 1 : _atkTargetIndex - 1;
        }

        /// <summary>
        /// 攻撃対象情報をクリアします
        /// </summary>
        public void ClearAtkTargetInfo()
        {
            _atkTargetIndex = 0;
            _atkTargetNum = 0;
        }

        public int X()
        {
            return Index % _stageData.GridRowNum;
        }

        public int Y()
        {
            return Index / _stageData.GridRowNum;
        }

        public int GetAttackableTargetNum() { return _atkTargetNum; }

        public Vector3 GetPosition()
        {
            return _currentPos;
        }

        private void Update()
        {
            UpdateUI( Time.deltaTime );
        }

        /// <summary>
        /// 選択しているカーソル位置を更新します
        /// </summary>
        /// /// <param name="delta">フレーム間の時間</param>
        void UpdateUI( float delta )
        {
            _endPos = GetGoalPosition();

            if ( GridState == State.NONE || GridState == State.MOVE)
            {
                UpdateLerpPosition(delta);
            }
            else
            {
                DrawSquareLine(TILE_SIZE, _endPos);
            }
        }

        /// <summary>
        /// グリッドの位置を線形補間で更新します
        /// </summary>
        /// <param name="delta">フレーム間の時間</param>
        void UpdateLerpPosition( float delta )
        {
            _totalTime += delta;
            _currentPos = Vector3.Lerp( _beginPos, _endPos, _totalTime / MoveInterpolationTime);

            DrawSquareLine(TILE_SIZE, _currentPos);
        }

        /// <summary>
        /// 指定した位置(centralPos)に四角形ラインを描画します
        /// </summary>
        /// <param name="gridSize">1グリッドのサイズ</param>
        /// <param name="centralPos">指定グリッドの中心位置</param>
        void DrawSquareLine(float gridSize, in Vector3 centralPos)
        {
            float halfSize = 0.5f * gridSize;

            Vector3 tileScale = _stageData.GetTile(Index).GetTileScale();

            Vector3[] linePoints = new Vector3[]
            {
                new Vector3(-halfSize, GRID_CURSOR_OFFSET_Y, -halfSize) + centralPos,
                new Vector3(-halfSize, GRID_CURSOR_OFFSET_Y,  halfSize) + centralPos,
                new Vector3( halfSize, GRID_CURSOR_OFFSET_Y,  halfSize) + centralPos,
                new Vector3( halfSize, GRID_CURSOR_OFFSET_Y, -halfSize) + centralPos,
            };

            // SetVertexCountは廃止されているはずだが、使用しないと正常に描画されなかったため使用(2023/5/26)
            _lineRenderer.SetVertexCount(linePoints.Length);
            _lineRenderer.SetPositions(linePoints);
        }

        /// <summary>
        /// 線形補間移動を開始します
        /// </summary>
        private void StartLerpMove()
        {
            _beginPos   = GetGoalPosition();
            _totalTime  = 0f;
        }

        /// <summary>
        /// グリッドの現在座標を取得します
        /// </summary>
        /// <returns>グリッドの現在座標</returns>
        private Vector3 GetGoalPosition()
        {
            return _stageData.GetTileInfo(Index).charaStandPos;
        }
    }
}