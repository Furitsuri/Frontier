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

        // �U���t�F�[�Y��Ԃł͍U���L�����N�^�[���擾
        if (Character.CHARACTER_TAG.CHARACTER_PLAYER == tag && btlInstance.IsAttackPhaseState())
        {
            selectCharacter = btlInstance.AttackerCharacter;
            _Camera.cullingMask = 1 << LayerMask.NameToLayer("ParamRenderAttacker");
        }
        // ����ȊO�̏�Ԃł̓O���b�h�I�𒆂̃L�����N�^�[���擾
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

        // �p�����[�^�\���𔽉f
        UpdateParamRender(selectCharacter, ref param);
        // �J�����`��𔽉f
        UpdateCamraRender(selectCharacter, ref param);
    }

    /// <summary>
    /// �p�����[�^UI�ɕ\������L�����N�^�[�̃p�����[�^���X�V���܂�
    /// </summary>
    /// <param name="selectCharacter">�I�����Ă���L�����N�^�[</param>
    /// <param name="param">�I�����Ă���L�����N�^�[�̃p�����[�^</param>
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
            // �_���[�W��0�̏ꍇ�͕\�����Ȃ�
            TMPDiffHPValue.text = "";
        }

        // �e�L�X�g�̐F�𔽉f
        ApplyTextColor(changeHP);
    }

    /// <summary>
    /// �e�L�X�g�̐F�𔽉f���܂�
    /// </summary>
    /// <param name="tmpParam">�Y���L�����N�^�[�̈ꎞ�p�����[�^</param>
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
    /// �p�����[�^UI�ɕ\������L�����N�^�[�̃J�����`����X�V���܂�
    /// </summary>
    /// <param name="selectCharacter">�I�����Ă���L�����N�^�[</param>
    /// <param name="param">�I�����Ă���L�����N�^�[�̃p�����[�^</param>
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