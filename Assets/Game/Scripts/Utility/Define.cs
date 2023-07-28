static public class Constants
{
    public enum Direction
    {
        NONE = -1,

        FORWARD,    // Z��������
        RIGHT,      // X��������
        BACK,       // Z��������
        LEFT,       // X��������

        NUM_MAX
    }

    // �v���C���[�A�G���ꂼ��̃L�����N�^�[�ő吔
    public const int CHARACTER_MAX_NUM = 16;
    // �L�����N�^�[�̃A�N�V�����Q�[�W�̍ő吔
    public const int ACTION_GAUGE_MAX = 10;
    // 1�O���b�h�ɗאڂ���O���b�h�̍ő吔
    public const int NEIGHBORING_GRID_MAX_NUM = 4;
    // �o�H�T���ɂ����郋�[�g�C���f�b�N�X�ő�ێ���
    public const int DIJKSTRA_ROUTE_INDEXS_MAX_NUM = 256;
    // �O���b�h��Y���W�ɉ��Z����␳�l
    public const float ADD_GRID_POS_Y = 0.02f;
    // �L�����N�^�[�̈ړ����x
    public const float CHARACTER_MOVE_SPEED = 5.0f;
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
}
