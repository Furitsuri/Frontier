using Frontier;
using Frontier.Entities;
using UnityEngine;
using Zenject;
using static Constants;

public class EntitySnapshot
{
    [Inject] HierarchyBuilderBase _hierarchyBld = null;

    private Camera _captureCamera; // 撮影専用カメラ

    public void Init()
    {
        _captureCamera = _hierarchyBld.CreateComponentAndOrganize<Camera>( true, "EntitySnapShotCamera" );
        _captureCamera.enabled = false;
        _captureCamera.clearFlags = CameraClearFlags.SolidColor;
        _captureCamera.backgroundColor = new Color( 0, 0, 0, 0 );
    }

    public void CaptureCharacter( int textureWidth, int textureHeight, Character targetCharacter, out Texture2D snapshot, bool isSnapAnimation, AnimDatas.AnimeConditionsTag animTag )
    {
        // 1. RenderTextureを準備
        RenderTexture rt = new RenderTexture( textureWidth, textureHeight, 16, RenderTextureFormat.ARGB32 );
        _captureCamera.targetTexture = rt;
        _captureCamera.backgroundColor = new Color( 0, 0, 0, 0 );
        _captureCamera.clearFlags = CameraClearFlags.SolidColor;

        // 2. カメラをキャラに向ける(必要に応じて位置調整)
        var originalActiveSelf = targetCharacter.gameObject.activeSelf;
        var originalPos = targetCharacter.GetTransformHandler.GetPosition();
        targetCharacter.gameObject.SetActive( true );
        targetCharacter.GetTransformHandler.SetPosition( new Vector3( ENTITY_SNAPSHOT_CHARACTER_SNAP_POS_X, ENTITY_SNAPSHOT_CHARACTER_SNAP_POS_Y, ENTITY_SNAPSHOT_CHARACTER_SNAP_POS_Z ) );
        if( isSnapAnimation || animTag != AnimDatas.AnimeConditionsTag.NONE )
        {
            // SnapToCharacterAnimation( targetCharacter, animTag );
        }
        _captureCamera.transform.position = targetCharacter.GetTransformHandler.GetPosition() + new Vector3( 0, 1.2f, 2f );
        _captureCamera.transform.LookAt( targetCharacter.GetTransformHandler.GetPosition() + new Vector3( 0, 0.45f, 0f ) );

        // 3. カメラでレンダリング実行
        _captureCamera.Render();

        // 4. RenderTexture → Texture2D へ変換
        RenderTexture.active = rt;
        snapshot = new Texture2D( textureWidth, textureHeight, TextureFormat.ARGB32, false );
        snapshot.ReadPixels( new Rect( 0, 0, textureWidth, textureHeight ), 0, 0 );
        snapshot.Apply();

        // 5. クリーンアップ
        if( isSnapAnimation || animTag != AnimDatas.AnimeConditionsTag.NONE ) {
            // targetCharacter.AnimCtrl.RestartAnimator();
        }
        targetCharacter.GetTransformHandler.SetPosition( originalPos );
        targetCharacter.gameObject.SetActive( originalActiveSelf );
        _captureCamera.targetTexture = null;
        RenderTexture.active = null;
    }

    private void SnapToCharacterAnimation( Character targetCharacter, AnimDatas.AnimeConditionsTag animTag )
    {
        var animCtrl = targetCharacter.AnimCtrl;
        if( animCtrl != null )
        {
            animCtrl.SnapToCurrentAnimationToStart( animTag );
        }
    }
}