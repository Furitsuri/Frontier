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
        Capture( textureWidth, textureHeight, targetCharacter, out snapshot, isSnapAnimation, animTag, false );
    }

    /// <summary>
    /// キャラクターの顔アイコン用の画像を撮影します。
    /// 構図は、パラメータ表示UIでキャラクターを映す際と同じく、キャラクター毎のカメラパラメータ(Character.CameraParam)に従います。
    /// </summary>
    /// <param name="textureSize">撮影する画像の一辺の長さ(ピクセル)</param>
    public void CapturePortrait( int textureSize, Character targetCharacter, out Texture2D snapshot )
    {
        Capture( textureSize, textureSize, targetCharacter, out snapshot, false, AnimDatas.AnimeConditionsTag.NONE, true );
    }

    private void Capture( int textureWidth, int textureHeight, Character targetCharacter, out Texture2D snapshot, bool isSnapAnimation, AnimDatas.AnimeConditionsTag animTag, bool isPortrait )
    {
        // 1. RenderTextureを準備
        RenderTexture rt = new RenderTexture( textureWidth, textureHeight, 16, RenderTextureFormat.ARGB32 );
        _captureCamera.targetTexture = rt;
        _captureCamera.backgroundColor = new Color( 0, 0, 0, 0 );
        _captureCamera.clearFlags = CameraClearFlags.SolidColor;

        // 2. カメラをキャラに向ける(必要に応じて位置調整)
        var originalActiveSelf = targetCharacter.gameObject.activeSelf;
        var originalPos = targetCharacter.GetPosition();
        targetCharacter.gameObject.SetActive( true );
        targetCharacter.SetPosition( new Vector3( ENTITY_SNAPSHOT_CHARACTER_SNAP_POS_X, ENTITY_SNAPSHOT_CHARACTER_SNAP_POS_Y, ENTITY_SNAPSHOT_CHARACTER_SNAP_POS_Z ) );
        if( isSnapAnimation || animTag != AnimDatas.AnimeConditionsTag.NONE )
        {
            // SnapToCharacterAnimation( targetCharacter, animTag );
        }
        if( isPortrait )
        {
            // キャラクターの正面から、キャラクター毎のカメラパラメータに従った構図で撮影する
            ref var camParam = ref targetCharacter.CameraParam;
            _captureCamera.transform.position = targetCharacter.GetPosition() + targetCharacter.transform.forward * camParam.UICameraLengthZ + Vector3.up * camParam.UICameraLengthY;
            _captureCamera.transform.LookAt( targetCharacter.GetPosition() + Vector3.up * camParam.UICameraLookAtCorrectY );
        }
        else
        {
            _captureCamera.transform.position = targetCharacter.GetPosition() + new Vector3( 0, 1.2f, 2f );
            _captureCamera.transform.LookAt( targetCharacter.GetPosition() + new Vector3( 0, 0.45f, 0f ) );
        }

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
        targetCharacter.SetPosition( originalPos );
        targetCharacter.gameObject.SetActive( originalActiveSelf );
        _captureCamera.targetTexture = null;
        RenderTexture.active = null;
        rt.Release();
        Object.Destroy( rt );
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