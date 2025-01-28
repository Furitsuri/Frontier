using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Xml.Xsl;
using UnityEditor;
using UnityEngine;
using Zenject.SpaceFighter;

namespace Frontier.Stage
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class StageController : Controller
    {
        /// <summary>
        /// �O���b�h�ɑ΂���t���O���
        /// </summary>
        public enum BitFlag
        {
            NONE                  = 0,
            CANNOT_MOVE           = 1 << 0,   // �ړ��s�O���b�h
            ATTACKABLE            = 1 << 1,   // �U���\�ȃO���b�h
            ATTACKABLE_TARGET     = 1 << 2,   // �U���Ώۂ��U���\�ȃO���b�h(ATTACKABLE�̓��e�������܂�ł���)
            PLAYER_EXIST          = 1 << 3,   // �v���C���[�L�����N�^�[������
            ENEMY_EXIST           = 1 << 4,   // �G�L�����N�^�[������
            OTHER_EXIST           = 1 << 5,   // ��O���͂�����
        }

        /// <summary>
        /// �L�����N�^�[�̈ʒu�����ɖ߂��ۂɎg�p���܂�
        /// </summary>
        public struct Footprint
        {
            public int gridIndex;
            public Quaternion rotation;
        }

        [SerializeField]
        private GameObject _stageObject;

        [SerializeField]
        private GameObject _gridMeshObject;

        [SerializeField]
        private GameObject _gridCursorObject;

        [SerializeField]
        private StageModel _stageModel;

        [SerializeField]
        private bool isAdjustStageScale = false;

        [SerializeField]
        public float BattlePosLengthFromCentral { get; private set; } = 2.0f;

        public bool back = true;
        private BattleManager _btlMgr;
        private Mesh _mesh;
        private GridCursor _gridCursor;
        private GridInfo[] _gridInfo;
        private GridInfo[] _gridInfoBase;
        private Footprint _footprint;
        private List<GridMesh> _gridMeshs;
        private List<int> _attackableGridIndexs;
        private float _operateKeyInterval = 0.13f;
        private float _operateKeyLastTime = 0;
        
        public int GridTotalNum { get; private set; } = 0;

        void Awake()
        {
            _gridMeshs = new List<GridMesh>();
            _attackableGridIndexs = new List<int>();

            // ���������O���b�h���b�V����o�^
            foreach (GridMesh grid in _gridMeshs)
            {
                AddGridMeshToList(grid);
            }
            GameObject gridCursorObject = Instantiate(_gridCursorObject);
            if (gridCursorObject != null)
            {
                _gridCursor = gridCursorObject.GetComponent<GridCursor>();
            }

            // �X�e�[�W��񂩂�e�T�C�Y���Q�Ƃ���
            if (isAdjustStageScale)
            {
                _stageModel.SetGridRowNum( (int)(Math.Floor(_stageObject.GetComponent<Renderer>().bounds.size.x) / GetGridSize()) );
                _stageModel.SetGridColumnNum( (int)(Math.Floor(_stageObject.GetComponent<Renderer>().bounds.size.z) / GetGridSize()) );
            }

            // ���b�V����`��
            GetComponent<MeshFilter>().mesh = _mesh = new Mesh();
            _mesh = ReGrid(_mesh);

            // �O���b�h���̏�����
            InitGridInfo();
            _gridCursor.Init(0, _stageModel, this);
        }

        /// <summary>
        /// �X�e�[�W��̃O���b�h����`�悵�܂�
        /// </summary>
        /// <param name="_mesh">�`��ɗp���郁�b�V��</param>
        /// <returns>�O���b�h�`����������b�V��</returns>
        Mesh ReGrid(Mesh _mesh)
        {
            if (back)
            {
                GetComponent<MeshRenderer>().material = new Material(Shader.Find("Sprites/Default"));
            }
            else
            {
                GetComponent<MeshRenderer>().material = new Material(Shader.Find("GUI/Text Shader"));
            }

            _mesh.Clear();

            int resolution;
            int count = 0;
            Vector3[] vertices;
            Vector2[] uvs;
            int[] lines;
            UnityEngine.Color[] colors;

            _stageModel.WidthX = GetGridSize() * _stageModel.GetGridRowNum() / 2.0f;
            _stageModel.WidthZ = GetGridSize() * _stageModel.GetGridColumnNum() / 2.0f;
            Vector2 startPosition = new Vector2(-_stageModel.WidthX, -_stageModel.WidthZ);
            Vector2 endPosition = -startPosition;
            resolution = 2 * (_stageModel.GetGridRowNum() + _stageModel.GetGridColumnNum() + 2);
            vertices = new Vector3[resolution];
            uvs = new Vector2[resolution];
            lines = new int[resolution];
            colors = new UnityEngine.Color[resolution];

            // X�����̒��_
            for (int i = 0; count < 2 * (_stageModel.GetGridRowNum() + 1); ++i, count = 2 * i)
            {
                vertices[count] = new Vector3(startPosition.x + ((float)i * GetGridSize()), startPosition.y, 0);
                vertices[count + 1] = new Vector3(startPosition.x + ((float)i * GetGridSize()), endPosition.y, 0);
            }
            // Y(Z)�����̒��_
            for (int i = 0; count < resolution; ++i, count = 2 * i + 2 * (_stageModel.GetGridRowNum() + 1))
            {
                vertices[count] = new Vector3(startPosition.x, endPosition.y - ((float)i * GetGridSize()), 0);
                vertices[count + 1] = new Vector3(endPosition.x, endPosition.y - ((float)i * GetGridSize()), 0);
            }

            for (int i = 0; i < resolution; i++)
            {
                uvs[i] = Vector2.zero;
                lines[i] = i;
                colors[i] = UnityEngine.Color.black;
            }

            // Y������������Ƃ��ĉ�]������
            Vector3 rotDirection = Vector3.up;
            _mesh.vertices = RotationVertices(vertices, rotDirection);
            _mesh.uv = uvs;
            _mesh.colors = colors;
            _mesh.SetIndices(lines, MeshTopology.Lines, 0);

            return _mesh;
        }

        /// <summary>
        /// �O���b�h�������������܂�
        /// </summary>
        void InitGridInfo()
        {
            GridTotalNum    = _stageModel.GetGridRowNum() * _stageModel.GetGridColumnNum();
            _gridInfo       = new GridInfo[GridTotalNum];
            _gridInfoBase   = new GridInfo[GridTotalNum]; ;

            for (int i = 0; i < GridTotalNum; ++i)
            {
                // ������
                _gridInfo[i] = new GridInfo();
                _gridInfoBase[i] = new GridInfo();
                _gridInfo[i].Init();
                _gridInfoBase[i].Init();
                // �O���b�h�ʒu����L�����̗����ʒu�ւ̕␳�l
                float charaPosCorrext = 0.5f * GetGridSize();
                // 1�����z��Ńf�[�^����������, ��(X��)�����͏�]�ōl������
                float posX = -_stageModel.WidthX + i % _stageModel.GetGridRowNum() * GetGridSize() + charaPosCorrext;
                // 1�����z��Ńf�[�^����������, �c(Z��)�����͏��ōl������
                float posZ = -_stageModel.WidthZ + i / _stageModel.GetGridRowNum() * GetGridSize() + charaPosCorrext;
                // ��L�l����e�O���b�h�̃L�����̗����ʒu������
                _gridInfoBase[i].charaStandPos = _gridInfo[i].charaStandPos = new Vector3(posX, 0, posZ);
                // TODO : �t�@�C���ǂݍ��݂���ʍs�s�\�ȉӏ��Ȃǂ�BitFlag����ݒ�o����悤�ɂ���
            }
        }

        /// <summary>
        /// _gridInfo�̏�Ԃ���̏�Ԃɖ߂��܂�
        /// </summary>
        void ResetGridInfo()
        {
            for (int i = 0; i < GridTotalNum; ++i)
            {
                _gridInfo[i] = _gridInfoBase[i].Copy();
            }
        }

        /// <summary>
        /// �ړ��\�ȃO���b�h��o�^���܂�
        /// </summary>
        /// <param name="gridIndex">�o�^�Ώۂ̃O���b�h�C���f�b�N�X</param>
        /// <param name="moveRange">�ړ��\�͈͒l</param>
        /// <param name="attackRange">�U���\�͈͒l</param>
        /// <param name="selfTag">�Ăяo�����L�����N�^�[�̃L�����N�^�[�^�O</param>
        /// <param name="isAttackable">�Ăяo�����̃L�����N�^�[���U���\���ۂ�</param>
        /// <param name="isDeparture">�o���O���b�h����Ăяo���ꂽ���ۂ�</param>
        void RegistMoveableEachGrid(int gridIndex, int moveRange, int attackRange, int selfCharaIndex,  Character.CHARACTER_TAG selfTag, bool isAttackable, bool isDeparture = false)
        {
            // �͈͊O�̃O���b�h�͍l�����Ȃ�
            if (gridIndex < 0 || GridTotalNum <= gridIndex) return;
            // �ړ��s�̃O���b�h�ɒH�蒅�����ꍇ�͏I��
            if (Methods.CheckBitFlag(_gridInfo[gridIndex].flag, BitFlag.CANNOT_MOVE)) return;
            // ���Ɍv�Z�ς݂̃O���b�h�ł���ΏI��
            if (moveRange <= _gridInfo[gridIndex].estimatedMoveRange) return;
            // ���g�ɑ΂���G�ΐ��̓L�����N�^�[�����݂���ΏI��
            StageController.BitFlag[] opponentTag = new StageController.BitFlag[(int)Character.CHARACTER_TAG.NUM]
            {
                BitFlag.ENEMY_EXIST  | BitFlag.OTHER_EXIST,     // PLAYER�ɂ�����G�ΐ���
                BitFlag.PLAYER_EXIST | BitFlag.OTHER_EXIST,     // ENEMY�ɂ�����G�ΐ���
                BitFlag.PLAYER_EXIST | BitFlag.ENEMY_EXIST      // OTHER�ɂ�����G�ΐ���
            };
            if (Methods.CheckBitFlag(_gridInfo[gridIndex].flag, opponentTag[(int)selfTag])) return;

            // ���݃O���b�h�̈ړ���R�l���X�V( �o���O���b�h�ł�moveRange�̒l�����̂܂ܓK������ )
            int currentMoveRange = (isDeparture) ? moveRange : _gridInfo[gridIndex].moveResist + moveRange;
            _gridInfo[gridIndex].estimatedMoveRange = currentMoveRange;

            // ���̒l�ł���ΏI��
            if (currentMoveRange < 0) return;
            // �U���͈͂ɂ��Ă��o�^����
            if (isAttackable && ( _gridInfo[gridIndex].characterTag == Character.CHARACTER_TAG.NONE || _gridInfo[gridIndex].charaIndex == selfCharaIndex) )
                RegistAttackableEachGrid(gridIndex, attackRange, selfTag, gridIndex);
            // ���[�����O
            if (gridIndex % _stageModel.GetGridRowNum() != 0)
                RegistMoveableEachGrid(gridIndex - 1, currentMoveRange, attackRange, selfCharaIndex, selfTag, isAttackable);      // gridIndex����X��������-1
            // �E�[�����O
            if ((gridIndex + 1) % _stageModel.GetGridRowNum() != 0)
                RegistMoveableEachGrid(gridIndex + 1, currentMoveRange, attackRange, selfCharaIndex, selfTag, isAttackable);      // gridIndex����X��������+1
            // Z�������ւ̉��Z�ƌ��Z�͂��̂܂�
            RegistMoveableEachGrid(gridIndex - _stageModel.GetGridRowNum(), currentMoveRange, attackRange, selfCharaIndex, selfTag, isAttackable);  // gridIndex����Z��������-1
            RegistMoveableEachGrid(gridIndex + _stageModel.GetGridRowNum(), currentMoveRange, attackRange, selfCharaIndex, selfTag, isAttackable);  // gridIndex����Z��������+1
        }

        /// <summary>
        /// ���_�z��f�[�^�����ׂĎw��̕����։�]�ړ������܂�
        /// </summary>
        /// <param name="vertices">��]�����钸�_�z��f�[�^</param>
        /// <param name="rotDirection">��]����</param>
        /// <returns>��]���������_�z��f�[�^</returns>
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
        /// ���������s���܂�
        /// </summary>
        /// <param name="btlMgr">�o�g���}�l�[�W��</param>
        public void Init(BattleManager btlMgr)
        {
            _btlMgr = btlMgr;
        }

        /// <summary>
        /// �O���b�h�����X�V���܂�
        /// </summary>
        public void UpdateGridInfo()
        {
            // ��x�S�ẴO���b�h�������ɖ߂�
            ResetGridInfo();
            // �L�����N�^�[�����݂���O���b�h�̏����X�V
            BitFlag[] flags =
            {
                BitFlag.PLAYER_EXIST,
                BitFlag.ENEMY_EXIST,
                BitFlag.OTHER_EXIST
            };

            for( int i = 0; i < (int)Character.CHARACTER_TAG.NUM; ++i )
            {
                foreach( var chara in _btlMgr.BtlCharaCdr.GetCharacterEnumerable((Character.CHARACTER_TAG)i))
                {
                    var gridIndex       = chara.tmpParam.gridIndex;
                    ref var info        = ref _gridInfo[gridIndex];
                    info.characterTag   = chara.param.characterTag;
                    info.charaIndex     = chara.param.characterIndex;
                    Methods.SetBitFlag(ref info.flag, flags[i]);
                }
            }
        }

        /// <summary>
        /// ��M����������񂩂�A���݂̃O���b�h�𑀍삵�܂�
        /// </summary>
        /// <param name="direction">�w�肳�ꂽ�i�s����</param>
        public void OperateGridCursor( Constants.Direction direction )
        {
            switch( direction )
            {
                case Constants.Direction.FORWARD:
                    _gridCursor.Up();
                    break;
                case Constants.Direction.BACK:
                    _gridCursor.Down();
                    break;
                case Constants.Direction.LEFT:
                    _gridCursor.Left();
                    break;
                case Constants.Direction.RIGHT:
                    _gridCursor.Right();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// ���݂̃O���b�h���L�[���͂ő��삵�܂�
        /// </summary>
        public void OperateGridCursor()
        {
            // �U���t�F�[�Y��Ԃł͍U���\�ȃL�����N�^�[�����E�őI������
            if (_gridCursor.GridState == GridCursor.State.ATTACK)
            {
                if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.LeftArrow))       { _gridCursor.TransitPrevTarget(); }
                if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.RightArrow))    { _gridCursor.TransitNextTarget(); }
            }
            else
            {
                if (Input.GetKey(KeyCode.UpArrow) && OperateKeyControl())          { _gridCursor.Up(); }
                if (Input.GetKey(KeyCode.DownArrow) && OperateKeyControl())      { _gridCursor.Down(); }
                if (Input.GetKey(KeyCode.LeftArrow) && OperateKeyControl())      { _gridCursor.Left(); }
                if (Input.GetKey(KeyCode.RightArrow) && OperateKeyControl())    { _gridCursor.Right(); }
            }
        }

        /// <summary>
        /// �L�[����������ۂɁA
        /// �Z�����Ԃŉ��x�������L�[���������ꂽ�Ɣ��肳��Ȃ��悤�ɃC���^�[�o����݂��܂�
        /// </summary>
        /// <returns>�L�[���삪�L����������</returns>
        private bool OperateKeyControl()
        {
            if( _operateKeyInterval <= Time.time - _operateKeyLastTime)
            {
                _operateKeyLastTime = Time.time;

                return true;
            }

            return false;
        }

        /// <summary>
        /// �I���O���b�h���w��̃L�����N�^�[�̃O���b�h�ɍ��킹�܂�
        /// </summary>
        /// <param name="character">�w��L�����N�^�[</param>
        public void ApplyCurrentGrid2CharacterGrid(Character character)
        {
            _gridCursor.Index = character.tmpParam.gridIndex;
        }

        /// <summary>
        /// 2�̎w��̃C���f�b�N�X���ׂ荇�����W�ɑ��݂��Ă��邩�𔻒肵�܂�
        /// </summary>
        /// <param name="fstIndex">�w��C���f�b�N�X����1</param>
        /// <param name="scdIndex">�w��C���f�b�N�X����2</param>
        /// <returns>�ׂ荇�����ۂ�</returns>
        public bool IsGridNextToEacheOther(int fstIndex, int scdIndex)
        {
            bool updown = (Math.Abs(fstIndex - scdIndex) == _stageModel.GetGridRowNum());

            int fstQuotient = fstIndex / _stageModel.GetGridColumnNum();
            int scdQuotient = scdIndex / _stageModel.GetGridColumnNum();
            var fstRemainder = fstIndex % _stageModel.GetGridColumnNum();
            var scdRemainder = scdIndex % _stageModel.GetGridColumnNum();
            bool leftright = (fstQuotient == scdQuotient) && (Math.Abs(fstRemainder - scdRemainder) == 1);

            return updown || leftright;
        }

        /// <summary>
        /// �O���b�h�Ɉړ��\����o�^���܂�
        /// </summary>
        /// <param name="departIndex">�ړ��L�����N�^�[�����݂���O���b�h�̃C���f�b�N�X�l</param>
        /// <param name="moveRange">�ړ��\�͈͒l</param>
        /// <param name="attackRange">�U���\�͈͒l</param>
        /// <param name="selfTag">�L�����N�^�[�^�O</param>
        /// <param name="isAttackable">�U���\���ۂ�</param>
        public void RegistMoveableInfo(int departIndex, int moveRange, int attackRange, int selfCharaIndex, Character.CHARACTER_TAG selfTag, bool isAttackable)
        {
            Debug.Assert(0 <= departIndex && departIndex < GridTotalNum, "StageController : Irregular Index.");

            // �ړ��ۏ����e�O���b�h�ɓo�^
            RegistMoveableEachGrid(departIndex, moveRange, attackRange, selfCharaIndex, selfTag, isAttackable, true);
        }

        /// <summary>
        /// �O���b�h�ɍU���\����o�^���܂�
        /// </summary>
        /// <param name="departIndex">�U���L�����N�^�[�����݂���O���b�h�̃C���f�b�N�X�l</param>
        /// <param name="attackRange">�U���\�͈͒l</param>
        /// <param name="selfTag">�U�����s���L�����N�^�[���g�̃L�����N�^�[�^�O</param>
        public bool RegistAttackAbleInfo(int departIndex, int attackRange, Character.CHARACTER_TAG selfTag)
        {
            Debug.Assert(0 <= departIndex && departIndex < GridTotalNum, "StageController : Irregular Index.");

            _attackableGridIndexs.Clear();
            Character attackCandidate = null;

            // �S�ẴO���b�h�̍U���ۏ���������
            for (int i = 0; i < GridTotalNum; ++i)
            {
                Methods.UnsetBitFlag(ref _gridInfo[i].flag, BitFlag.ATTACKABLE);
                Methods.UnsetBitFlag(ref _gridInfo[i].flag, BitFlag.ATTACKABLE_TARGET);
            }

            // �U���ۏ����e�O���b�h�ɓo�^
            RegistAttackableEachGrid(departIndex, attackRange, selfTag, departIndex);

            // �U���\�A���U���ΏۂƂȂ�L�����N�^�[�����݂���O���b�h�����X�g�ɓo�^
            for (int i = 0; i < _gridInfo.Length; ++i)
            {
                var info = _gridInfo[i];
                if (Methods.CheckBitFlag(info.flag, BitFlag.ATTACKABLE))
                {
                    attackCandidate = _btlMgr.BtlCharaCdr.GetCharacterFromHashtable(info.characterTag, info.charaIndex);

                    if (attackCandidate != null && attackCandidate.param.characterTag != selfTag)
                    {
                        _attackableGridIndexs.Add(i);
                    }
                }
            }

            return 0 < _attackableGridIndexs.Count;
        }

        /// <summary>
        /// �U���\�ȃO���b�h��o�^���܂�
        /// </summary>
        /// <param name="gridIndex">�Ώۂ̃O���b�h�C���f�b�N�X</param>
        /// <param name="attackRange">�U���\�͈͒l</param>
        /// <param name="selfTag">���g�̃L�����N�^�[�^�O</param>
        /// <param name="departIndex">�o���O���b�h�C���f�b�N�X</param>
        void RegistAttackableEachGrid(int gridIndex, int attackRange, Character.CHARACTER_TAG selfTag, int departIndex)
        {
            // �͈͊O�̃O���b�h�͍l�����Ȃ�
            if (gridIndex < 0 || GridTotalNum <= gridIndex) return;
            // �ړ��s�̃O���b�h�ɂ͍U���ł��Ȃ�
            if (Methods.CheckBitFlag(_gridInfo[gridIndex].flag, BitFlag.CANNOT_MOVE)) return;
            // �o���n�_�łȂ���Γo�^
            if (gridIndex != departIndex)
            {
                Methods.SetBitFlag(ref _gridInfo[gridIndex].flag, BitFlag.ATTACKABLE);

                switch (selfTag)
                {
                    case Character.CHARACTER_TAG.PLAYER:
                        if (_gridInfo[gridIndex].characterTag == Character.CHARACTER_TAG.ENEMY ||
                            _gridInfo[gridIndex].characterTag == Character.CHARACTER_TAG.OTHER)
                        {
                            Methods.SetBitFlag(ref _gridInfo[departIndex].flag, BitFlag.ATTACKABLE_TARGET);
                        }
                        break;
                    case Character.CHARACTER_TAG.ENEMY:
                        if (_gridInfo[gridIndex].characterTag == Character.CHARACTER_TAG.PLAYER ||
                            _gridInfo[gridIndex].characterTag == Character.CHARACTER_TAG.OTHER)
                        {
                            Methods.SetBitFlag(ref _gridInfo[departIndex].flag, BitFlag.ATTACKABLE_TARGET);
                        }
                        break;
                    case Character.CHARACTER_TAG.OTHER:
                        if (_gridInfo[gridIndex].characterTag == Character.CHARACTER_TAG.PLAYER ||
                            _gridInfo[gridIndex].characterTag == Character.CHARACTER_TAG.ENEMY)
                        {
                            Methods.SetBitFlag(ref _gridInfo[departIndex].flag, BitFlag.ATTACKABLE_TARGET);
                        }
                        break;
                    default:
                        break;
                }
            }

            // ���̒l�ł���ΏI��
            if (--attackRange < 0) return;

            // ���[�����O
            if (gridIndex % _stageModel.GetGridRowNum() != 0)
                RegistAttackableEachGrid(gridIndex - 1, attackRange, selfTag, departIndex);       // gridIndex����X��������-1
                                                                                                  // �E�[�����O
            if ((gridIndex + 1) % _stageModel.GetGridRowNum() != 0)
                RegistAttackableEachGrid(gridIndex + 1, attackRange, selfTag, departIndex);       // gridIndex����X��������+1
                                                                                                  // Z�������ւ̉��Z�ƌ��Z�͂��̂܂�
            RegistAttackableEachGrid(gridIndex - _stageModel.GetGridRowNum(), attackRange, selfTag, departIndex);   // gridIndex����Z��������-1
            RegistAttackableEachGrid(gridIndex + _stageModel.GetGridRowNum(), attackRange, selfTag, departIndex);   // gridindex����Z��������+1
        }

        /// <summary>
        /// �U���\�ȃL�����N�^�[�����݂���O���b�h�ɃO���b�h�J�[�\���̈ʒu��ݒ肵�܂�
        /// </summary>
        /// <param name="target">�\�ߍU���Ώۂ����܂��Ă���ۂɎw��</param>
        public void SetupGridCursorToAttackCandidate(Character target = null)
        {
            // �I���O���b�h�������I�ɍU���\�L�����N�^�[�̑��݂���O���b�h�C���f�b�N�X�ɐݒ�
            if (0 < _attackableGridIndexs.Count)
            {
                _gridCursor.SetAtkTargetNum(_attackableGridIndexs.Count);

                // �U���Ώۂ����Ɍ��܂��Ă���ꍇ�͑Ώۂ�T��
                if (target != null && 1 < _attackableGridIndexs.Count)
                {
                    for (int i = 0; i < _attackableGridIndexs.Count; ++i)
                    {
                        var info = GetGridInfo(_attackableGridIndexs[i]);

                        Character chara = _btlMgr.BtlCharaCdr.GetCharacterFromHashtable(info.characterTag, info.charaIndex);

                        if (target == chara)
                        {
                            _gridCursor.SetAtkTargetIndex(i);
                            break;
                        }
                    }
                }
                else
                {
                    _gridCursor.SetAtkTargetIndex(0);
                }
            }
        }

        /// <summary>
        /// �U���\�O���b�h�̂����A�U���\�L�����N�^�[�����݂���O���b�h�����X�g�ɓo�^���܂�
        /// </summary>
        /// <param name="targetTag">�U���Ώۂ̃^�O</param>
        /// <param name="target">�\�ߍU���Ώۂ����܂��Ă���ۂɎw��</param>
        /// <returns>�U���\�L�����N�^�[�����݂��Ă���</returns>
        public bool RegistAttackTargetGridIndexs(Character.CHARACTER_TAG targetTag, Character target = null)
        {
            Character character = null;

            _gridCursor.ClearAtkTargetInfo();
            _attackableGridIndexs.Clear();

            // �U���\�A���U���ΏۂƂȂ�L�����N�^�[�����݂���O���b�h�����X�g�ɓo�^
            for (int i = 0; i < _gridInfo.Length; ++i)
            {
                var info = _gridInfo[i];
                if (Methods.CheckBitFlag(info.flag, BitFlag.ATTACKABLE))
                {
                    character = _btlMgr.BtlCharaCdr.GetCharacterFromHashtable(info.characterTag, info.charaIndex);

                    if (character != null && character.param.characterTag == targetTag)
                    {
                        _attackableGridIndexs.Add(i);
                    }
                }
            }

            // �I���O���b�h�������I�ɍU���\�L�����N�^�[�̑��݂���O���b�h�C���f�b�N�X�ɐݒ�
            if (0 < _attackableGridIndexs.Count)
            {
                _gridCursor.SetAtkTargetNum(_attackableGridIndexs.Count);

                // �U���Ώۂ����Ɍ��܂��Ă���ꍇ�͑Ώۂ�T��
                if (target != null && 1 < _attackableGridIndexs.Count)
                {
                    for (int i = 0; i < _attackableGridIndexs.Count; ++i)
                    {
                        var info = GetGridInfo(_attackableGridIndexs[i]);

                        Character chara = _btlMgr.BtlCharaCdr.GetCharacterFromHashtable(info.characterTag, info.charaIndex);

                        if (target == chara)
                        {
                            _gridCursor.SetAtkTargetIndex(i);
                            break;
                        }
                    }
                }
                else
                {
                    _gridCursor.SetAtkTargetIndex(0);
                }
            }

            return 0 < _attackableGridIndexs.Count;
        }

        /// <summary>
        /// �ړ��\�O���b�h��`�悵�܂�
        /// </summary>
        /// <param name="departIndex">�ړ��L�����N�^�[�����݂���O���b�h�̃C���f�b�N�X�l</param>
        /// <param name="moveableRange">�ړ��\�͈͒l</param>
        /// <param name="attackableRange">�U���\�͈͒l</param>
        public void DrawMoveableGrids(int departIndex, int moveableRange, int attackableRange)
        {
            Debug.Assert(0 <= departIndex && departIndex < GridTotalNum, "StageController : Irregular Index.");

            int count = 0;
            // �O���b�h�̏�Ԃ����b�V���ŕ`��
            for (int i = 0; i < GridTotalNum; ++i)
            {
                if (Methods.CheckBitFlag(_gridInfo[i].flag, BitFlag.ATTACKABLE_TARGET))
                {
                    Instantiate(_gridMeshObject);  // TODO : ��
                    _gridMeshs[count++].DrawGridMesh(_gridInfo[i].charaStandPos, GetGridSize(), GridMesh.MeshType.ATTACKABLE_TARGET);

                    continue;
                }

                if (0 <= _gridInfo[i].estimatedMoveRange)
                {
                    Instantiate(_gridMeshObject);  // TODO : ��
                    _gridMeshs[count++].DrawGridMesh(_gridInfo[i].charaStandPos, GetGridSize(), GridMesh.MeshType.MOVE);

                    Debug.Log("Moveable Grid Index : " + i);
                    continue;
                }

                if (Methods.CheckBitFlag(_gridInfo[i].flag, BitFlag.ATTACKABLE))
                {
                    Instantiate(_gridMeshObject);  // TODO : ��
                    _gridMeshs[count++].DrawGridMesh(_gridInfo[i].charaStandPos, GetGridSize(), GridMesh.MeshType.ATTACK);

                    Debug.Log("Attackable Grid Index : " + i);
                    continue;
                }
            }
        }

        /// <summary>
        /// �U���\�O���b�h��`�悵�܂�
        /// </summary>
        /// <param name="departIndex">�U���L�����N�^�[�����݂���O���b�h�̃C���f�b�N�X�l</param>
        public void DrawAttackableGrids(int departIndex)
        {
            Debug.Assert(0 <= departIndex && departIndex < GridTotalNum, "StageController : Irregular Index.");

            int count = 0;
            // �O���b�h�̏�Ԃ����b�V���ŕ`��
            for (int i = 0; i < GridTotalNum; ++i)
            {
                if (Methods.CheckBitFlag(_gridInfo[i].flag, BitFlag.ATTACKABLE))
                {
                    Instantiate(_gridMeshObject);  // TODO : ��
                    _gridMeshs[count++].DrawGridMesh(_gridInfo[i].charaStandPos, GetGridSize(), GridMesh.MeshType.ATTACK);

                    Debug.Log("Attackable Grid Index : " + i);
                }
            }
        }

        /// <summary>
        /// �S�ẴO���b�h�ɂ�����w��̃r�b�g�t���O�̐ݒ���������܂�
        /// </summary>
        public void UnsetGridsBitFlag(BitFlag value)
        {
            // �S�ẴO���b�h�̈ړ��E�U���ۏ���������
            for (int i = 0; i < GridTotalNum; ++i)
            {
                Methods.UnsetBitFlag(ref _gridInfo[i].flag, value);
            }
        }

        /// <summary>
        /// �S�ẴO���b�h���b�V���̕`����������܂�
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
        /// �U���\�����������܂�
        /// </summary>
        public void ClearAttackableInfo()
        {
            UnsetGridsBitFlag( BitFlag.ATTACKABLE );
            _attackableGridIndexs.Clear();
        }

        /// <summary>
        /// �O���b�h���b�V���ɂ��̃N���X��o�^���܂�
        /// �O���b�h���b�V���N���X���������ꂽ�^�C�~���O�ŃO���b�h���b�V��������Ăяo����܂�
        /// </summary>
        /// <param name="script">�O���b�h���b�V���N���X�̃X�N���v�g</param>
        public void AddGridMeshToList(GridMesh script)
        {
            _gridMeshs.Add(script);
        }

        /// <summary>
        /// �c���Ɖ����̃O���b�h�����擾���܂�
        /// </summary>
        /// <returns>�c���Ɖ����̃O���b�h��</returns>
        public (int, int) GetGridNumsXZ()
        {
            return (_stageModel.GetGridRowNum(), _stageModel.GetGridColumnNum());
        }

        /// <summary>
        /// �O���b�h��1�ӂ̑傫��(����)���擾���܂�
        /// </summary>
        /// <returns>�O���b�h��1�ӂ̑傫��(����)</returns>
        public float GetGridSize()
        {
            return _stageModel.GetGridSize();
        }

        /// <summary>
        /// �w��O���b�h�ɂ�����L�����N�^�[�̃��[���h���W���擾���܂�
        /// </summary>
        /// <param name="index">�w��O���b�h</param>
        /// <returns>�O���b�h�ɂ����钆�S���[���h���W</returns>
        public Vector3 GetGridCharaStandPos(int index)
        {
            return _gridInfo[index].charaStandPos;
        }

        /// <summary>
        /// �O���b�h�J�[�\���̃C���f�b�N�X�l���擾���܂�
        /// </summary>
        /// <returns>���݂̑I���O���b�h�̃C���f�b�N�X�l</returns>
        public int GetCurrentGridIndex()
        {
            return _gridCursor.Index;
        }

        /// <summary>
        /// �O���b�h�J�[�\���̏�Ԃ��擾���܂�
        /// </summary>
        /// <returns>���݂̑I���O���b�h�̏��</returns>
        public GridCursor.State GetGridCursorState()
        {
            return _gridCursor.GridState;
        }

        /// <summary>
        /// �O���b�h�J�[�\�����o�C���h���Ă���L�����N�^�[���擾���܂�
        /// </summary>
        /// <returns>�o�C���h���Ă���L�����N�^�[(���݂��Ȃ��ꍇ��null)</returns>
        public Character GetGridCursorBindCharacter()
        {
            return _gridCursor.BindCharacter;
        }

        /// <summary>
        /// �O���b�h�J�[�\���ɃL�����N�^�[���o�C���h���܂�
        /// </summary>
        /// <param name="state">�o�C���h�^�C�v</param>
        /// <param name="bindCharacter">�o�C���h�Ώۂ̃L�����N�^�[</param>
        public void BindGridCursorState( GridCursor.State state, Character bindCharacter )
        {
            _gridCursor.GridState       = state;
            _gridCursor.BindCharacter   = bindCharacter;
        }

        /// <summary>
        /// �I���O���b�h�̃A�N�e�B�u��Ԃ�ݒ肵�܂�
        /// </summary>
        /// <param name="isActive">�ݒ肷��A�N�e�B�u���</param>
        public void SetGridCursorActive( bool isActive )
        {
            _gridCursor.SetActive( isActive );
        }

        /// <summary>
        /// �O���b�h�J�[�\���̃L�����N�^�[�o�C���h���������܂�
        /// </summary>
        public void ClearGridCursroBind()
        {
            if (_gridCursor.BindCharacter != null)
            {
                _gridCursor.BindCharacter.gameObject.SetLayerRecursively(LayerMask.NameToLayer(Constants.LAYER_NAME_CHARACTER));
            }

            _gridCursor.GridState       = GridCursor.State.NONE;
            _gridCursor.BindCharacter   = null;
        }

        /// <summary>
        /// ���ݑI�����Ă���O���b�h�̏����擾���܂�
        /// �U���ΏۑI����Ԃł͑I�����Ă���U���Ώۂ����݂���O���b�h�����擾���܂�
        /// </summary>
        /// <param name="gridInfo">�Y������O���b�h�̏��</param>
        public void FetchCurrentGridInfo(out GridInfo gridInfo)
        {
            int index = 0;

            if (_gridCursor.GridState == GridCursor.State.ATTACK)
            {
                index = _attackableGridIndexs[_gridCursor.GetAtkTargetIndex()];
            }
            else
            {
                index = _gridCursor.Index;
            }

            gridInfo = _gridInfo[index];
        }

        /// <summary>
        /// �w��C���f�b�N�X�̃O���b�h�����擾���܂�
        /// </summary>
        /// <param name="index">�w�肷��C���f�b�N�X�l</param>
        /// <returns>�w��C���f�b�N�X�̃O���b�h���</returns>
        public ref GridInfo GetGridInfo(int index)
        {
            return ref _gridInfo[index];
        }

        /// <summary>
        /// �O���b�h�̃��b�V���̕`��̐ؑւ��s���܂�
        /// </summary>
        /// <param name="isDisplay">�`�悷�邩�ۂ�</param>
        public void ToggleMeshDisplay(bool isDisplay)
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = isDisplay;
            }
        }

        /// <summary>
        /// �o���n�_�ƖړI�n����ړ��o�H�ƂȂ�O���b�h�̃C���f�b�N�X���X�g���擾���܂�
        /// </summary>
        /// <param name="departGridIndex">�o���n�O���b�h�̃C���f�b�N�X</param>
        /// <param name="destGridIndex">�ړI�n�O���b�h�̃C���f�b�N�X</param>
        public List<(int routeIndexs, int routeCost)> ExtractShortestRouteIndexs(int departGridIndex, int destGridIndex, in List<int> candidateRouteIndexs)
        {
            Dijkstra dijkstra = new Dijkstra(candidateRouteIndexs.Count);

            // �o���O���b�h����̃C���f�b�N�X�̍����擾
            for (int i = 0; i + 1 < candidateRouteIndexs.Count; ++i)
            {
                for (int j = i + 1; j < candidateRouteIndexs.Count; ++j)
                {
                    int diff = candidateRouteIndexs[j] - candidateRouteIndexs[i];
                    if ((diff == -1 && (candidateRouteIndexs[i] % _stageModel.GetGridRowNum() != 0)) ||                                 // ���ɑ���(���[������)
                        (diff == 1 && (candidateRouteIndexs[i] % _stageModel.GetGridRowNum() != _stageModel.GetGridRowNum() - 1)) ||    // �E�ɑ���(�E�[������)
                         Math.Abs(diff) == _stageModel.GetGridRowNum())                                                                 // ��܂��͉��ɑ���
                    {
                        // �ړ��\�ȗאڃO���b�h�����_�C�N�X�g���ɓ����
                        dijkstra.Add(i, j);
                        dijkstra.Add(j, i);
                    }
                }
            }

            // �_�C�N�X�g������o���O���b�h����ړI�O���b�h�܂ł̍ŒZ�o�H�𓾂�
            return dijkstra.GetMinRoute(candidateRouteIndexs.IndexOf(departGridIndex), candidateRouteIndexs.IndexOf(destGridIndex), candidateRouteIndexs);
        }

        /// <summary>
        /// �L�����N�^�[�̈ʒu�y�ь�����ێ����܂�
        /// </summary>
        /// <param name="footprint">�ێ�����l</param>
        public void LeaveFootprint(Footprint footprint)
        {
            _footprint = footprint;
        }

        /// <summary>
        /// �ێ����Ă����ʒu�y�ь������w��̃L�����N�^�[�ɐݒ肵�܂�
        /// </summary>
        /// <param name="character">�w�肷��L�����N�^�[</param>
        public void FollowFootprint(Character character)
        {
            _gridCursor.Index = _footprint.gridIndex;
            character.tmpParam.gridIndex = _footprint.gridIndex;
            GridInfo info;
            FetchCurrentGridInfo(out info);
            character.transform.position = info.charaStandPos;
            character.transform.rotation = _footprint.rotation;
        }

        /// <summary>
        /// �w�肳�ꂽ�C���f�b�N�X�Ԃ̃O���b�h����Ԃ��܂�
        /// </summary>
        /// <param name="fromIndex">�n�_�C���f�b�N�X</param>
        /// <param name="toIndex">�I�_�C���f�b�N�X</param>
        /// <returns>�O���b�h��</returns>
        public float CalcurateGridLength(int fromIndex, int toIndex)
        {
            var from = _gridInfo[fromIndex].charaStandPos;
            var to = _gridInfo[toIndex].charaStandPos;
            var gridLength = (from - to).magnitude / GetGridSize();

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

                // �X�e�[�W��񂩂�T�C�Y�����߂�ۂ̓T�C�Y�ҏW��s�ɂ���
                EditorGUI.BeginDisabledGroup(script.isAdjustStageScale);
                script._stageModel.SetGridRowNum( EditorGUILayout.IntField("X�����O���b�h��", script._stageModel.GetGridRowNum()) );
                script._stageModel.SetGridColumnNum( EditorGUILayout.IntField("Z�����O���b�h��", script._stageModel.GetGridColumnNum()) );
                EditorGUI.EndDisabledGroup();

                base.OnInspectorGUI();
            }
        }
#endif // UNITY_EDITOR
        */
    }
}