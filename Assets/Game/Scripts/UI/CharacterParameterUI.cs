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

    float _camareAngleY;
    Character _character;
    Camera _Camera;
    RenderTexture _TargetTexture;
    bool _isAttacking = false;

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
        var btlInstance = BattleManager.Instance;

        // �L�����N�^�[��null�̏�Ԃ�GameObject��Active�ɂȂ��Ă��邱�Ƃ͑z�肵�Ȃ�
        Debug.Assert(_character != null);

        if( _isAttacking )
        {
            _Camera.cullingMask = 1 << LayerMask.NameToLayer("ParamRenderAttacker");
        }
        else
        {
            _Camera.cullingMask = 1 << LayerMask.NameToLayer("ParamRender");
        }

        if (_character == null)
        {
            Debug.Assert(false);

            return;
        }

        var param = _character.param;

        // �p�����[�^�\���𔽉f
        UpdateParamRender(_character, ref param);
        // �J�����`��𔽉f
        UpdateCamraRender(_character, ref param);
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
        changeHP = Mathf.Clamp(changeHP, -param.CurHP, param.MaxHP - param.CurHP);
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
        Transform playerTransform = selectCharacter.transform;
        Vector3 add = Quaternion.AngleAxis(_camareAngleY, Vector3.up) * playerTransform.forward * param.UICameraLengthZ;
        _Camera.transform.position = playerTransform.position + add + Vector3.up * param.UICameraLengthY;
        _Camera.transform.LookAt(playerTransform.position + Vector3.up * param.UICameraLookAtCorrectY);
        _Camera.Render();
    }

    public void Init( float angleY )
    {
        _camareAngleY = angleY;
    }

    /// <summary>
    /// �\������L�����N�^�[��ݒ肵�܂�
    /// </summary>
    /// <param name="character">�\���L�����N�^�[</param>
    public void SetCharacter( Character character )
    {
        _character = character;
    }

    public void SetAttacking( bool isAttacking )
    {
        _isAttacking = isAttacking;
    }
}