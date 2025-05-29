using Frontier.Entities;
using UnityEngine;

#pragma warning disable 0618

namespace Frontier.Stage
{
    public class GridCursor : MonoBehaviour
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
        private StageData _stageData = null;
        private StageController _stageCtrl = null;
        private Vector3 _beginPos   = Vector3.zero;
        private Vector3 _endPos     = Vector3.zero;
        private int _atkTargetIndex = 0;
        private int _atkTargetNum = 0;
        private float _totalTime = 0;

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
        public void Init(int initIndex, StageData stageModel, StageController stgCtrl)
        {
            Index           = initIndex;
            _stageData     = stageModel;
            _stageCtrl      = stgCtrl;
            _atkTargetIndex = 0;
            _atkTargetNum   = 0;
            GridState       = State.NONE;
            BindCharacter   = null;
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
            _endPos = GetCurrentPosition();

            if ( GridState == State.NONE || GridState == State.MOVE)
            {
                UpdateLerpPosition(delta);
            }
            else
            {
                DrawSquareLine(_stageData.GetGridSize(), _endPos);
            }
        }

        /// <summary>
        /// グリッドの位置を線形補間で更新します
        /// </summary>
        /// <param name="delta">フレーム間の時間</param>
        void UpdateLerpPosition( float delta )
        {
            _totalTime += delta;
            Vector3 curPos = Vector3.Lerp( _beginPos, _endPos, _totalTime / MoveInterpolationTime);

            DrawSquareLine(_stageData.GetGridSize(), curPos);
        }

        /// <summary>
        /// 指定した位置(centralPos)に四角形ラインを描画します
        /// </summary>
        /// <param name="gridSize">1グリッドのサイズ</param>
        /// <param name="centralPos">指定グリッドの中心位置</param>
        void DrawSquareLine(float gridSize, in Vector3 centralPos)
        {
            float halfSize = 0.5f * gridSize;

            Vector3[] linePoints = new Vector3[]
            {
            new Vector3(-halfSize, 0.05f, -halfSize) + centralPos,
            new Vector3(-halfSize, 0.05f,  halfSize) + centralPos,
            new Vector3( halfSize, 0.05f,  halfSize) + centralPos,
            new Vector3( halfSize, 0.05f, -halfSize) + centralPos,

            };

            // SetVertexCountは廃止されているはずだが、使用しないと正常に描画されなかったため使用(2023/5/26)
            _lineRenderer.SetVertexCount(linePoints.Length);
            _lineRenderer.SetPositions(linePoints);
        }

        /// <summary>
        /// 線形補間移動を開始します
        /// </summary>
        void StartLerpMove()
        {
            _beginPos   = GetCurrentPosition();
            _totalTime  = 0f;
        }

        /// <summary>
        /// グリッドの現在座標を取得します
        /// </summary>
        /// <returns>グリッドの現在座標</returns>
        Vector3 GetCurrentPosition()
        {
            GridInfo info;
            _stageCtrl.FetchCurrentGridInfo(out info);

            return info.charaStandPos;
        }

        /// <summary>
        /// インデックス値を現在グリッドの上に該当する値に設定します
        /// </summary>
        public void Up()
        {
            StartLerpMove();
            Index += _stageData.GetGridRowNum();
            if (_stageData.GetGridRowNum() * _stageData.GetGridRowNum() <= Index)
            {
                Index = Index % (_stageData.GetGridRowNum() * _stageData.GetGridRowNum());
            }
        }

        /// <summary>
        /// インデックス値を現在グリッドの下に該当する値に設定します
        /// </summary>
        public void Down()
        {
            StartLerpMove();
            Index -= _stageData.GetGridRowNum();
            if (Index < 0)
            {
                Index += _stageData.GetGridRowNum() * _stageData.GetGridRowNum();
            }
        }

        /// <summary>
        /// インデックス値を現在グリッドの右に該当する値に設定します
        /// </summary>
        public void Right()
        {
            StartLerpMove();
            Index++;
            if (Index % _stageData.GetGridRowNum() == 0)
            {
                Index -= _stageData.GetGridRowNum();
            }
        }

        /// <summary>
        /// インデックス値を現在グリッドの左に該当する値に設定します
        /// </summary>
        public void Left()
        {
            StartLerpMove();
            Index--;
            if ((Index + 1) % _stageData.GetGridRowNum() == 0)
            {
                Index += _stageData.GetGridRowNum();
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
        public void SetActive( bool isActive )
        {
            gameObject.SetActive( isActive );
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
    }
}