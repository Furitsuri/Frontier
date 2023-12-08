using UnityEngine;

#pragma warning disable 0618

namespace Frontier.Stage
{
    public class GridCursor : MonoBehaviour
    {
        /// <summary>
        /// �O���b�h�J�[�\���̏��
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
        /// ���������܂�
        /// </summary>
        /// <param name="initIndex">�����C���f�b�N�X�l</param>
        /// <param name="rowNum">�Ֆʂɂ�����s�ɊY������O���b�h��</param>
        /// <param name="columnNum">�Ֆʂɂ������ɊY������O���b�h��</param>
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
        /// �I�����Ă���J�[�\���ʒu���X�V���܂�
        /// </summary>
        void UpdateUI()
        {
            GridInfo info;
            _stageCtrl.FetchCurrentGridInfo(out info);

            Vector3 centralPos = info.charaStandPos;

            DrawSquareLine(_stageModel.GetGridSize(), ref centralPos);
        }

        /// <summary>
        /// �w�肵���ʒu(centralPos)�Ɏl�p�`���C����`�悵�܂�
        /// </summary>
        /// <param name="gridSize">1�O���b�h�̃T�C�Y</param>
        /// <param name="centralPos">�w��O���b�h�̒��S�ʒu</param>
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

            // SetVertexCount�͔p�~����Ă���͂������A�g�p���Ȃ��Ɛ���ɕ`�悳��Ȃ��������ߎg�p(2023/5/26)
            _lineRenderer.SetVertexCount(linePoints.Length);
            _lineRenderer.SetPositions(linePoints);
        }

        /// <summary>
        /// �C���f�b�N�X�l�����݃O���b�h�̏�ɊY������l�ɐݒ肵�܂�
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
        /// �C���f�b�N�X�l�����݃O���b�h�̉��ɊY������l�ɐݒ肵�܂�
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
        /// �C���f�b�N�X�l�����݃O���b�h�̉E�ɊY������l�ɐݒ肵�܂�
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
        /// �C���f�b�N�X�l�����݃O���b�h�̍��ɊY������l�ɐݒ肵�܂�
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
        /// �U���ΏۃC���f�b�N�X�l���擾���܂�
        /// </summary>
        /// <returns>�U���ΏۃC���f�b�N�X�l</returns>
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
        /// �U���ΏۃC���f�b�N�X�l��ݒ肵�܂�
        /// </summary>
        /// <param name="index">�U���ΏۃC���f�b�N�X�l</param>
        public void SetAtkTargetIndex(int index)
        {
            _atkTargetIndex = index;
        }

        /// <summary>
        /// �U���ΏۃC���f�b�N�X�̑�����ݒ肵�܂�
        /// </summary>
        /// <param name="num">�U���ΏۃC���f�b�N�X�̑���</param>
        public void SetAtkTargetNum(int num)
        {
            _atkTargetNum = num;
        }

        /// <summary>
        /// ���̃^�[�Q�b�g�C���f�b�N�X�l�ɑJ�ڂ��܂�
        /// </summary>
        public void TransitNextTarget()
        {
            _atkTargetIndex = (_atkTargetIndex + 1) % _atkTargetNum;
        }

        /// <summary>
        /// �O�̃^�[�Q�b�g�C���f�b�N�X�l�ɑJ�ڂ��܂�
        /// </summary>
        public void TransitPrevTarget()
        {
            _atkTargetIndex = (_atkTargetIndex - 1) < 0 ? _atkTargetNum - 1 : _atkTargetIndex - 1;
        }

        /// <summary>
        /// �U���Ώۏ����N���A���܂�
        /// </summary>
        public void ClearAtkTargetInfo()
        {
            _atkTargetIndex = 0;
            _atkTargetNum = 0;
        }
    }
}