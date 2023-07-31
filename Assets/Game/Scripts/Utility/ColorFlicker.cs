using UnityEngine;
using UnityEngine.UI;

public class ColorFlicker<T> where T : IColorEditable 
{
    [SerializeField]
    T colorFlickObject;
    [Header("1���[�v�̒���(�b�P��)")]
    [SerializeField]
    [Range(0.1f, 10.0f)]
    float duration = 1.0f;
    [Header("���[�v�J�n���̐F")]
    [SerializeField]
    Color32 startColor = new Color32(255, 255, 255, 255);
    //���[�v�I��(�܂�Ԃ�)���̐F��0�`255�܂ł̐����Ŏw��B
    [Header("���[�v�I�����̐F")]
    [SerializeField]
    Color32 endColor = new Color32(255, 255, 255, 64);

    private bool _enabled = false;
    private float _elapsedTime = 0f;

    public ColorFlicker(T target)
    {
        colorFlickObject = target;
    }

    public void Init()
    {
        _enabled = false;
    }

    public void UpdateFlick()
    {
        if (!_enabled) return;

        _elapsedTime += Time.deltaTime;
        colorFlickObject.Color = Color.Lerp(startColor, endColor, Mathf.PingPong(_elapsedTime / duration, 1.0f));
    }

    /// <summary>
    /// �L���E��L���ݒ���s���܂�
    /// </summary>
    /// <param name="enabled">�L���E��L��</param>
    public void setEnabled(bool enabled)
    {
        _enabled = enabled;
        _elapsedTime = 0f;
    }
}