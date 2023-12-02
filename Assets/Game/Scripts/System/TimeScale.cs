using System;

/// <summary>
/// ���̃N���X���C���X�^���X�Ɏ��I�u�W�F�N�g�̃^�C���X�P�[�����Ǘ����܂�
/// </summary>
public class TimeScale
{
    private float _timeScale = 1f;
    public float CurrentTimeScale => _timeScale;

    public Action<float> OnValueChange;

    public void SetTimeScale(float timeScale)
    {
        _timeScale = timeScale;
        OnValueChange?.Invoke(timeScale);
    }
}