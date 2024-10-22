static public class Constants
{
    /// <summary>
    /// �X�e�[�W��̐i�s����
    /// </summary>
    public enum Direction
    {
        NONE = -1,

        FORWARD,    // Z��������
        RIGHT,      // X��������
        BACK,       // Z��������
        LEFT,       // X��������

        NUM_MAX
    }

    /// <summary>
    /// �e����L�[�̒�`
    /// </summary>
    public enum KeyIcon : int
    {
        ALL_CURSOR = 0, // �S����
        UP,             // ��
        DOWN,           // ��
        LEFT,           // ��
        RIGHT,          // �E
        DECISION,       // ����
        CANCEL,         // �߂�
        ESCAPE,         // �I��

        NUM_MAX
    }

    // �v���C���[�A�G���ꂼ��̃L�����N�^�[�ő吔
    public const int CHARACTER_MAX_NUM = 16;
    // �L�����N�^�[�������o����X�L���̍ő吔
    public const int EQUIPABLE_SKILL_MAX_NUM = 4;
    // �L�����N�^�[�̃A�N�V�����Q�[�W�̍ő吔
    public const int ACTION_GAUGE_MAX = 10;
    // 1�O���b�h�ɗאڂ���O���b�h�̍ő吔
    public const int NEIGHBORING_GRID_MAX_NUM = 4;
    // �o�H�T���ɂ����郋�[�g�C���f�b�N�X�ő�ێ���
    public const int DIJKSTRA_ROUTE_INDEXS_MAX_NUM = 256;
    // �O���b�h��Y���W�ɉ��Z����␳�l
    public const float ADD_GRID_POS_Y = 0.02f;
    // �L�����N�^�[�̈ړ����x
    public const float CHARACTER_MOVE_SPEED = 7.5f;
    // �L�����N�^�[�̉�]���x
    public const float CHARACTER_ROT_SPEED = 10f;
    // �L�����N�^�[�̉�]�I��臒l
    public const float CHARACTER_ROT_THRESHOLD = 3f;
    // �v���C���[�̈ړ����쎞�A�ڕW���W�ɑ΂����͂��󂯕t������O���b�h�T�C�Y�̊���
    public const float ACCEPTABLE_INPUT_GRID_SIZE_RATIO = 0.33f;
    // �G���ړ��͈͂�\��������A���ۂɈړ�����܂ł̑҂�����
    public const float ENEMY_SHOW_MOVE_RANGE_TIME = 0.35f;
    // �U�����Ɍ������߂�ۂ̑҂�����
    public const float ATTACK_ROTATIION_TIME = 0.2f;
    // �U�����ɑ���ɋߐڂ���܂ł̎���
    public const float ATTACK_CLOSING_TIME = 0.55f;
    // �U����ɑ��肩�狗�������܂ł̎���
    public const float ATTACK_DISTANCING_TIME = 0.23f;
    // �U���V�[�P���X�ɂ�����҂�����
    public const float ATTACK_SEQUENCE_WAIT_TIME = 0.75f;
    // �U���V�[�P���X�ɂ�����U���J�n�܂ł̑҂�����
    public const float ATTACK_SEQUENCE_WAIT_ATTACK_TIME = 0.5f;
    // �U���V�[�P���X�ɂ�����I���҂�����
    public const float ATTACK_SEQUENCE_WAIT_END_TIME = 0.95f;
    // �L�[�K�C�h�ɂ�����X�v���C�g�e�L�X�g�Ԃ̕�
    public const float SPRITE_TEXT_SPACING_ON_KEY_GUIDE = 10f;

    public const string LAYER_NAME_CHARACTER            = "Character";
    public const string LAYER_NAME_LEFT_PARAM_WINDOW    = "ParamRenderLeft";
    public const string LAYER_NAME_RIGHT_PARAM_WINDOW   = "ParamRenderRight";
    public const string OBJECT_TAG_NAME_CHARA_SKIN_MESH = "CharacterSkinMesh";
    public const string GUIDE_SPRITE_FOLDER_PASS        = "Sprites/Originals/UI/KeyGuide/";
#if UNITY_EDITOR
    public const string GUIDE_SPRITE_FILE_NAME          = "Preview Keyboard & Mouse";
#elif UNITY_STANDALONE_WIN
    public const string GUIDE_SPRITE_FILE_NAME          = "Preview Steam Deck";
#else
    public const string GUIDE_SPRITE_FILE_NAME          = "Preview Steam Deck";
#endif
}
