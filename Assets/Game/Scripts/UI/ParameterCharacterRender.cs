using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class ParameterCharacterRender : MonoBehaviour
{
    public RawImage TargetImage;
    public Character.CHARACTER_TAG tag;

    Camera m_Camera;
    RenderTexture m_TargetTexture;

    // Start is called before the first frame update
    void Start()
    {
        m_TargetTexture = new RenderTexture((int)TargetImage.rectTransform.rect.width * 2, (int)TargetImage.rectTransform.rect.height * 2, 16, RenderTextureFormat.ARGB32);

        TargetImage.texture = m_TargetTexture;

        GameObject cameObject = new GameObject();
        m_Camera = cameObject.AddComponent<Camera>();
        m_Camera.enabled = false;
        m_Camera.clearFlags = CameraClearFlags.SolidColor;
        m_Camera.backgroundColor = new Color(0, 0, 0, 0);
        m_Camera.targetTexture = m_TargetTexture;
        m_Camera.cullingMask = 1 << LayerMask.NameToLayer("ParamRender");
    }

    // Update is called once per frame
    void Update()
    {
        var btlInstance = BattleManager.instance;

        Character selectCharacter = null;

        // 攻撃フェーズ状態では攻撃キャラクターを取得
        if (Character.CHARACTER_TAG.CHARACTER_PLAYER == tag && btlInstance.IsAttackPhaseState())
        {
            selectCharacter = btlInstance.AttackerCharacter;
            m_Camera.cullingMask = 1 << LayerMask.NameToLayer("ParamRenderAttacker");
        }
        // それ以外の状態ではグリッド選択中のキャラクターを取得
        else
        {
            selectCharacter = btlInstance.SearchCharacterFromCharaIndex(btlInstance.SelectCharacterIndex);
            m_Camera.cullingMask = 1 << LayerMask.NameToLayer("ParamRender");
        }
        
        if( selectCharacter == null )
        {
            Debug.Assert(false);

            return;
        }

        float angle = ( Character.CHARACTER_TAG.CHARACTER_PLAYER == tag ) ? 45f : -45f;
        Transform playerTransform = selectCharacter.transform;
        var param = selectCharacter.param;
        Vector3 add = Quaternion.AngleAxis(angle, Vector3.up) * playerTransform.forward * param.UICameraLengthZ;
        m_Camera.transform.position = playerTransform.position + add + Vector3.up * param.UICameraLengthY;
        m_Camera.transform.LookAt(playerTransform.position + Vector3.up * param.UICameraLookAtCorrectY);
        m_Camera.Render();
    }
}