using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterParameterUI : MonoBehaviour
{
    public TextMeshProUGUI TMPMaxHPValue;
    public TextMeshProUGUI TMPCurHPValue;
    public TextMeshProUGUI TMPAtkValue;
    public TextMeshProUGUI TMPDefValue;
    public TextMeshProUGUI TMPDiffHPValue;
    public RawImage TargetImage;
    public Character.CHARACTER_TAG tag;

    Camera _Camera;
    RenderTexture _TargetTexture;

    void Start()
    {
        _TargetTexture = new RenderTexture((int)TargetImage.rectTransform.rect.width * 2, (int)TargetImage.rectTransform.rect.height * 2, 16, RenderTextureFormat.ARGB32);

        TargetImage.texture = _TargetTexture;

        GameObject cameObject = new GameObject();
        _Camera = cameObject.AddComponent<Camera>();
        _Camera.enabled = false;
        _Camera.clearFlags = CameraClearFlags.SolidColor;
        _Camera.backgroundColor = new Color(0, 0, 0, 0);
        _Camera.targetTexture = _TargetTexture;
        _Camera.cullingMask = 1 << LayerMask.NameToLayer("ParamRender");
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
            _Camera.cullingMask = 1 << LayerMask.NameToLayer("ParamRenderAttacker");
        }
        // それ以外の状態ではグリッド選択中のキャラクターを取得
        else
        {
            selectCharacter = btlInstance.SearchCharacterFromCharaIndex(btlInstance.SelectCharacterIndex);
            _Camera.cullingMask = 1 << LayerMask.NameToLayer("ParamRender");
        }

        if (selectCharacter == null)
        {
            Debug.Assert(false);

            return;
        }

        var param = selectCharacter.param;

        // パラメータ表示を反映
        UpdateParamRender(selectCharacter, ref param);
        // カメラ描画を反映
        UpdateCamraRender(selectCharacter, ref param);
    }

    /// <summary>
    /// パラメータUIに表示するキャラクターのパラメータを更新します
    /// </summary>
    /// <param name="selectCharacter">選択しているキャラクター</param>
    /// <param name="param">選択しているキャラクターのパラメータ</param>
    void UpdateParamRender(Character selectCharacter, ref Character.Parameter param)
    {
        TMPMaxHPValue.text = $"{param.MaxHP}";
        TMPCurHPValue.text = $"{param.CurHP}";
        TMPAtkValue.text = $"{param.Atk}";
        TMPDefValue.text = $"{param.Def}";

        int changeHP = selectCharacter.tmpParam.expectedChangeHP;
        if( 0 < changeHP)
        {
            TMPDiffHPValue.text = $"+{changeHP}";
        }
        else if( changeHP < 0 )
        {
            TMPDiffHPValue.text = $"{changeHP}";
        }
        else
        {
            // ダメージが0の場合は表示しない
            TMPDiffHPValue.text = "";
        }

        // テキストの色を反映
        ApplyTextColor(changeHP);
    }

    /// <summary>
    /// テキストの色を反映します
    /// </summary>
    /// <param name="tmpParam">該当キャラクターの一時パラメータ</param>
    void ApplyTextColor( int changeHP )
    {
        if (changeHP < 0)
        {
            TMPDiffHPValue.color = Color.red;
        }
        else if (0 < changeHP)
        {
            TMPDiffHPValue.color = Color.green;
        }
    }


    /// <summary>
    /// パラメータUIに表示するキャラクターのカメラ描画を更新します
    /// </summary>
    /// <param name="selectCharacter">選択しているキャラクター</param>
    /// <param name="param">選択しているキャラクターのパラメータ</param>
    void UpdateCamraRender( Character selectCharacter, ref Character.Parameter param )
    {
        float angle = (Character.CHARACTER_TAG.CHARACTER_PLAYER == tag) ? 45f : -45f;
        Transform playerTransform = selectCharacter.transform;
        Vector3 add = Quaternion.AngleAxis(angle, Vector3.up) * playerTransform.forward * param.UICameraLengthZ;
        _Camera.transform.position = playerTransform.position + add + Vector3.up * param.UICameraLengthY;
        _Camera.transform.LookAt(playerTransform.position + Vector3.up * param.UICameraLookAtCorrectY);
        _Camera.Render();
    }
}