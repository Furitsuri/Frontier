using System.Drawing;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// カメラにモザイク効果をかけるクラスです
/// ExecuteAlwaysを設定したいので、BattleCameraControllerとは別にしています
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
    /// 有効・非有効を設定します
    /// </summary>
    /// <param name="enable">有効・非有効設定</param>
    public void ToggleEnable( bool enable )
    {
        _enabled = enable;
    }

    /// <summary>
    /// モザイクのブロックサイズを指定レートで更新します
    /// </summary>
    /// <param name="sizeRate">指定するサイズレート</param>
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
        _mosaicMaterial.SetFloat( _blockSizeID, _initialBlockSize );
    }
}