using UnityEngine;
using Frontier.Entities;
using Zenject;
using static Constants;

public class EntitySnapshot
{
    [Inject] HierarchyBuilderBase _hierarchyBld = null;

    private int _textureWidth = 160;
    private int _textureHeight = 160;

    private Camera _captureCamera; // 撮影専用カメラ

    public void Init( float width, float height )
    {
        _textureWidth   = ( int ) width;
        _textureHeight  = ( int ) height;

        InitCamera();
    }

    public void CaptureCharacter( Character targetCharacter, out Texture2D snapshot )
    {
        // 1. RenderTextureを準備
        RenderTexture rt = new RenderTexture( _textureWidth, _textureHeight, 16, RenderTextureFormat.ARGB32 );
        _captureCamera.targetTexture = rt;
        _captureCamera.backgroundColor = new Color( 0, 0, 0, 0 );
        _captureCamera.clearFlags = CameraClearFlags.SolidColor;

        // 2. カメラをキャラに向ける(必要に応じて位置調整)
        var originalActiveSelf = targetCharacter.gameObject.activeSelf;
        var originalPos = targetCharacter.GetTransformHandler.GetPosition();
        targetCharacter.gameObject.SetActive( true );
        targetCharacter.GetTransformHandler.SetPosition( new Vector3( ENTITY_SNAPSHOT_CHARACTER_SNAP_POS_X, ENTITY_SNAPSHOT_CHARACTER_SNAP_POS_Y, ENTITY_SNAPSHOT_CHARACTER_SNAP_POS_Z ) );
        _captureCamera.transform.position = targetCharacter.GetTransformHandler.GetPosition() + new Vector3( 0, 1.2f, 2f );
        _captureCamera.transform.LookAt( targetCharacter.GetTransformHandler.GetPosition() + new Vector3( 0, 0.45f, 0f ) );

        // 3. カメラでレンダリング実行
        _captureCamera.Render();

        // 4. RenderTexture → Texture2D へ変換
        RenderTexture.active = rt;
        snapshot = new Texture2D( _textureWidth, _textureHeight, TextureFormat.ARGB32, false );
        snapshot.ReadPixels( new Rect( 0, 0, _textureWidth, _textureHeight ), 0, 0 );
        snapshot.Apply();

        // 5. クリーンアップ
        targetCharacter.GetTransformHandler.SetPosition( originalPos );
        targetCharacter.gameObject.SetActive( originalActiveSelf );
        _captureCamera.targetTexture = null;
        RenderTexture.active = null;
    }

    private void InitCamera()
    {
        _captureCamera = _hierarchyBld.CreateComponentAndOrganize<Camera>( true, "EntitySnapShotCamera" );
        _captureCamera.enabled = false;
        _captureCamera.clearFlags = CameraClearFlags.SolidColor;
        _captureCamera.backgroundColor = new Color( 0, 0, 0, 0 );
    }
}