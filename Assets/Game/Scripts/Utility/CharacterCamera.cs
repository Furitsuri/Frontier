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
    private int _layerIndex;            // このカメラに映すレイヤー
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
        _layerIndex             = layerIndex;
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

        // 同じキャラクターが別のカメラ(別のウィンドウ)にも割り当てられている場合、キャラクターのレイヤーはそちらのものになっているため、
        // 描画する間だけこのカメラのレイヤーへ切り替え、描画後に元へ戻す
        // (スキルの対象が使用者自身である場合など、同じキャラクターを左右のウィンドウに同時に表示するため)
        // MEMO : このカメラは常時描画せず、ここで明示的に描画しているため、描画の直前・直後の切り替えで他のカメラの描画に影響しない。
        //        レイヤーが既にこのカメラのものである通常の場合は、切り替えを行わない
        var characterObj    = _dispCharacter.gameObject;
        int prevLayer       = characterObj.layer;
        bool isSwitchLayer  = ( prevLayer != _layerIndex );
        if( isSwitchLayer ) { characterObj.SetLayerRecursively( _layerIndex ); }

        _camera.Render();

        if( isSwitchLayer ) { characterObj.SetLayerRecursively( prevLayer ); }
    }

    public void AssignCharacter( Character character, int layerMaskIndex )
    {
        _dispCharacter = character;
        _dispCharacter.gameObject.SetLayerRecursively( layerMaskIndex );    // 配置用にキャラクターのレイヤーを変更
    }
}
