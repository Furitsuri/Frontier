using Frontier.Entities;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class CharacterCamera
{
    [Inject] private HierarchyBuilderBase _hierarchyBld = null;

    private RawImage _refTargetImage;
    private Camera _camera;
    private RenderTexture _targetTexture;
    private Character _dispCharacter;   // 表示中のキャラクター  
    private float _angleY = 30.0f;

    public void Setup( GameObject parentObj,  string cameraName )
    {
        LazyInject.GetOrCreate( ref _camera, () => _hierarchyBld.CreateComponentNestedParentWithDiContainer<Camera>( parentObj, true, false, cameraName ) );
        // _camera = _hierarchyBld.CreateComponentAndOrganize<Camera>( true, cameraName );
    }

    public void Init( string cameraName, int layerIndex, float angleY, ref RawImage refTargetImage )
    {
        _refTargetImage         = refTargetImage;
        _targetTexture          = new RenderTexture( ( int ) _refTargetImage.rectTransform.rect.width * 2, ( int ) _refTargetImage.rectTransform.rect.height * 2, 16, RenderTextureFormat.ARGB32 );
        _refTargetImage.texture = _targetTexture;
        _angleY                 = angleY;
        if( _camera == null )
        LazyInject.GetOrCreate( ref _camera, () => _hierarchyBld.CreateComponentAndOrganize<Camera>( true, cameraName ) );
        _camera.enabled         = false;
        _camera.clearFlags      = CameraClearFlags.SolidColor;
        _camera.backgroundColor = new Color( 0, 0, 0, 0 );
        _camera.targetTexture   = _targetTexture;
        _camera.cullingMask     = 1 << layerIndex;
    }

    public void Dispose()
    {
        if( _camera != null )
        {
            Object.Destroy( _camera.gameObject );
            _camera = null;
        }
        if( _targetTexture != null )
        {
            _targetTexture.Release();
            Object.Destroy( _targetTexture );
            _targetTexture = null;
        }
        _refTargetImage = null;
        _dispCharacter = null;
    }

    public void Update( in CameraParameter camParam )
    {
        Transform characterTransform = _dispCharacter.transform;
        Vector3 add = Quaternion.AngleAxis( _angleY, Vector3.up ) * characterTransform.forward * camParam.UICameraLengthZ;
        _camera.transform.position = characterTransform.position + add + Vector3.up * camParam.UICameraLengthY;
        _camera.transform.LookAt( characterTransform.position + Vector3.up * camParam.UICameraLookAtCorrectY );
        _camera.Render();
    }

    public void AssignCharacter( Character character, int layerMaskIndex )
    {
        _dispCharacter = character;
        _dispCharacter.gameObject.SetLayerRecursively( layerMaskIndex );    // 配置用にキャラクターのレイヤーを変更
    }
}
