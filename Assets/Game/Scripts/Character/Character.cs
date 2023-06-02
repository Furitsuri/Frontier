using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    // �L�����N�^�[�̎��p�����[�^
    public struct Parameter
    {
        // �L�����N�^�[�ԍ�
        public int characterIndex;
        // �X�e�[�W�J�n���O���b�h���W(�C���f�b�N�X)
        public int initGridIndex;
        // �ړ������W
        public int moveRange;
        // �s���ς݂�
        public bool isEndAction;

        public void Init()
        {
            characterIndex  = 0;
            initGridIndex   = 0;
            moveRange       = 0;
            isEndAction     = false;
        }
    }

    public enum CHARACTER_TAG
    {
        CHARACTER_NONE = -1,
        CHARACTER_PLAYER,
        CHARACTER_ENEMY,
        CHARACTER_OTHER,
        CHARACTER_NUM,
    }

    public enum MOVE_TYPE
    {
        MOVE_TYPE_NORMAL = 0,
        MOVE_TYPE_HORSE,
        MOVE_TYPE_FLY,
        MOVE_TYPE_HEAVY,
        MOVE_TYPE_NUM,
    }

    private Animator animator;
    public Parameter param;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        param.Init();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Vector2[] moveableGrid()
    {



        return new Vector2[0];
    }
}
