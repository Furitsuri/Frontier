using Frontier.Stage;
namespace Frontier.Stage
{
    /// <summary>
    /// �X�e�[�W�f�[�^�񋟃C���^�[�t�F�[�X
    /// �X�e�[�W�f�[�^�𒼐ڎ擾�o����悤�ɂ���ƁA
    /// C#�̎d�l��A�X�e�[�W��ҏW�E�Ǎ�����ۂɐV�����f�[�^�ւƍ����ւ��邱�Ƃ��o���Ȃ�����
    /// �ԐړI�Ɏ擾����C���^�[�t�F�[�X���K�v
    /// </summary>
    public interface IStageDataProvider
    {
        StageData CurrentData { get; set; }
    }
}