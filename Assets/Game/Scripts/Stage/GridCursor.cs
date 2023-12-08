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

        private LineRenderer _lineRenderer;
        private StageModel _stageModel = null;
        private StageController _stageCtrl = null;
        private int _atkTargetIndex = 0;
        private int _atkTargetNum = 0;

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
        public void Init(int initIndex, StageModel stageModel, StageController stgCtrl)
        {
            Index           = initIndex;
            _stageModel     = stageModel;
            _stageCtrl      = stgCtrl;
            _atkTargetIndex = 0;
            _atkTargetNum   = 0;
            GridState       = State.NONE;
            BindCharacter   = null;
        }

        private void Update()
        {
            UpdateUI();
        }

        /// <summary>
        /// 選択しているカーソル位置を更新します
        /// </summary>
        void UpdateUI()
        {
            GridInfo info;
            _stageCtrl.FetchCurrentGridInfo(out info);

            Vector3 centralPos = info.charaStandPos;

            DrawSquareLine(_stageModel.GetGridSize(), ref centralPos);
        }

        /// <summary>
        /// 指定した位置(centralPos)に四角形ラインを描画します
        /// </summary>
        /// <param name="gridSize">1グリッドのサイズ</param>
        /// <param name="centralPos">指定グリッドの中心位置</param>
        void DrawSquareLine(float gridSize, ref Vector3 centralPos)
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
        /// インデックス値を現在グリッドの上に該当する値に設定します
        /// </summary>
        public void Up()
        {
            Index += _stageModel.GetGridRowNum();
            if (_stageModel.GetGridRowNum() * _stageModel.GetGridRowNum() <= Index)
            {
                Index = Index % (_stageModel.GetGridRowNum() * _stageModel.GetGridRowNum());
            }
        }

        /// <summary>
        /// インデックス値を現在グリッドの下に該当する値に設定します
        /// </summary>
        public void Down()
        {
            Index -= _stageModel.GetGridRowNum();
            if (Index < 0)
            {
                Index += _stageModel.GetGridRowNum() * _stageModel.GetGridRowNum();
            }
        }

        /// <summary>
        /// インデックス値を現在グリッドの右に該当する値に設定します
        /// </summary>
        public void Right()
        {
            Index++;
            if (Index % _stageModel.GetGridRowNum() == 0)
            {
                Index -= _stageModel.GetGridRowNum();
            }
        }

        /// <summary>
        /// インデックス値を現在グリッドの左に該当する値に設定します
        /// </summary>
        public void Left()
        {
            Index--;
            if ((Index + 1) % _stageModel.GetGridRowNum() == 0)
            {
                Index += _stageModel.GetGridRowNum();
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
        /// 
        /// </summary>
        /// <param name="isActive"></param>
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