using System.Drawing;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// �J�����Ƀ��U�C�N���ʂ�������N���X�ł�
/// ExecuteAlways��ݒ肵�����̂ŁABattleCameraController�Ƃ͕ʂɂ��Ă��܂�
/// </summary>
[ExecuteAlways]
public class CameraMosaicEffect : MonoBehaviour
{
    [SerializeField]
    private Material _mosaicMaterial;
    [SerializeField]
    private bool _enabled = false;

    private int _blockSizeID;
    private float _initialBlockSize;

    void Start()
    {
        _blockSizeID = Shader.PropertyToID("_BlockSize");

        if( _mosaicMaterial != null )
        {
            _initialBlockSize = _mosaicMaterial.GetFloat(_blockSizeID);
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (_mosaicMaterial != null && _enabled)
        {
            Graphics.Blit(source, destination, _mosaicMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }

    /// <summary>
    /// �L���E��L����ݒ肵�܂�
    /// </summary>
    /// <param name="enable">�L���E��L���ݒ�</param>
    public void ToggleEnable( bool enable )
    {
        _enabled = enable;
    }

    /// <summary>
    /// ���U�C�N�̃u���b�N�T�C�Y���w�背�[�g�ōX�V���܂�
    /// </summary>
    /// <param name="sizeRate">�w�肷��T�C�Y���[�g</param>
    public void UpdateBlockSizeByRate( float sizeRate )
    {
        var size = sizeRate * _initialBlockSize;
        _mosaicMaterial.SetFloat(_blockSizeID, size);
    }

    /// <summary>
    /// 
    /// </summary>
    public void ResetBlockSize()
    {
        _mosaicMaterial.SetFloat(_blockSizeID, _initialBlockSize);
    }
}