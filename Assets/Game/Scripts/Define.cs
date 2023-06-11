static public class Constants
{
    public enum Direction
    {
        FORWARD,    // Z��������
        RIGHT,      // X��������
        BACK,       // Z��������
        LEFT,       // X��������

        NUM_MAX
    }

    // �v���C���[�A�G���ꂼ��̃L�����N�^�[�ő吔
    public const int CHARACTER_MAX_NUM = 16;
    // �O���b�h��Y���W�ɉ��Z����␳�l
    public const float ADD_GRID_POS_Y = 0.02f;
    // �L�����N�^�[�̈ړ����x
    public const float CHARACTER_MOVE_SPEED = 3.0f;
}
