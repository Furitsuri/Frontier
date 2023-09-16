using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CharacterParameterUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI TMPMaxHPValue;
    [SerializeField]
    private TextMeshProUGUI TMPCurHPValue;
    [SerializeField]
    private TextMeshProUGUI TMPAtkValue;
    [SerializeField]
    private TextMeshProUGUI TMPDefValue;
    [SerializeField]
    private TextMeshProUGUI TMPAtkNumValue;
    [SerializeField]
    private TextMeshProUGUI TMPDiffHPValue;
    [SerializeField]
    private TextMeshProUGUI TMPActRecoveryValue;
    [SerializeField]
    private RawImage TargetImage;
    [SerializeField]
    private RectTransform PanelTransform;
    [SerializeField]
    private RawImage ActGaugeElemImage;
    [SerializeField]
    private SkillBoxUI[] SkillBoxes;
    [SerializeField]
    private float BlinkingDuration;

    private Character _character;
    private Camera _camera;
    private RenderTexture _targetTexture;
    private List<RawImage> _actGaugeElems;
    private float _camareAngleY;
    private float _alpha;
    private float _blinkingElapsedTime;
    private bool _isAttacking = false;

    void Start()
    {
        _targetTexture          = new RenderTexture((int)TargetImage.rectTransform.rect.width * 2, (int)TargetImage.rectTransform.rect.height * 2, 16, RenderTextureFormat.ARGB32);
        TargetImage.texture     = _targetTexture;
        GameObject gameObject   = new GameObject();
        _camera                 = gameObject.AddComponent<Camera>();
        _camera.enabled         = false;
        _camera.clearFlags      = CameraClearFlags.SolidColor;
        _camera.backgroundColor = new Color(0, 0, 0, 0);
        _camera.targetTexture   = _targetTexture;
        _camera.cullingMask     = 1 << LayerMask.NameToLayer("ParamRender");
        _actGaugeElems          = new List<RawImage>(Constants.ACTION_GAUGE_MAX);

        for (int i = 0; i < Constants.ACTION_GAUGE_MAX; ++i) {
            var elem = Instantiate(ActGaugeElemImage);
            _actGaugeElems.Add( elem );
            elem.gameObject.SetActive(false);
            elem.transform.SetParent(PanelTransform, true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // �L�����N�^�[��null�̏�Ԃ�GameObject��Active�ɂȂ��Ă��邱�Ƃ͑z�肵�Ȃ�
        Debug.Assert(_character != null);

        if( _isAttacking )
        {
            _camera.cullingMask = 1 << LayerMask.NameToLayer("ParamRenderAttacker");
        }
        else
        {
            _camera.cullingMask = 1 << LayerMask.NameToLayer("ParamRender");
        }

        if (_character == null)
        {
            Debug.Assert(false);

            return;
        }

        // �p�����[�^�\���𔽉f
        UpdateParamRender(_character, _character.param, _character.skillModifiedParam);
        // �J�����`��𔽉f
        UpdateCamraRender(_character, _character.camParam);
    }

    /// <summary>
    /// �p�����[�^UI�ɕ\������L�����N�^�[�̃p�����[�^���X�V���܂�
    /// </summary>
    /// <param name="selectCharacter">�I�����Ă���L�����N�^�[</param>
    /// <param name="param">�I�����Ă���L�����N�^�[�̃p�����[�^</param>
    void UpdateParamRender(Character selectCharacter, in Character.Parameter param, in Character.SkillModifiedParameter skillParam)
    {
        Debug.Assert(param.consumptionActionGauge <= param.curActionGauge);

        TMPMaxHPValue.text          = $"{param.MaxHP}";
        TMPCurHPValue.text          = $"{param.CurHP}";
        TMPAtkValue.text            = $"{param.Atk}";
        TMPDefValue.text            = $"{param.Def}";
        TMPAtkNumValue.text         = $"x {skillParam.AtkNum}";
        TMPActRecoveryValue.text    = $"+{param.recoveryActionGauge}";
        TMPAtkNumValue.gameObject.SetActive( 1 < skillParam.AtkNum );

        int changeHP = selectCharacter.tmpParam.totalExpectedChangeHP;
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

        // �A�N�V�����Q�[�W�̕\��
        for ( int i = 0; i < Constants.ACTION_GAUGE_MAX; ++i )
        {
            var elem = _actGaugeElems[i];

            if (i <= param.maxActionGauge - 1)
            {
                elem.gameObject.SetActive(true);

                if (i <= param.curActionGauge - 1)
                {
                    elem.color = Color.green;

                    // �A�N�V�����Q�[�W�g�p���͓_�ł�����
                    if ((param.curActionGauge - param.consumptionActionGauge) <= i)
                    {
                        _blinkingElapsedTime += Time.deltaTime;
                        _alpha = Mathf.PingPong(_blinkingElapsedTime / BlinkingDuration, 1.0f);
                        elem.color              = new Color( 0, 1, 0, _alpha);
                    }
                }
                else
                {
                    elem.color = Color.gray;
                }
            }
            else
            {
                elem.gameObject.SetActive(false);
            }
        }

        // �X�L���{�b�N�X�̕\��
        for( int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i )
        {
            if( param.IsValidSkill(i) )
            {
                SkillBoxes[i].gameObject.SetActive(true);
                string skillName = SkillsData.data[(int)param.equipSkills[i]].Name;
                var type = SkillsData.data[(int)param.equipSkills[i]].Type;
                SkillBoxes[i].SetSkillName(skillName, type);
                SkillBoxes[i].ShowSkillCostImage(SkillsData.data[(int)param.equipSkills[i]].Cost);
            }
            else
            {
                SkillBoxes[i].gameObject.SetActive(false);
            }
        }
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
    void UpdateCamraRender( Character selectCharacter, in Character.CameraParameter camParam )
    {
        Transform playerTransform   = selectCharacter.transform;
        Vector3 add                 = Quaternion.AngleAxis(_camareAngleY, Vector3.up) * playerTransform.forward * camParam.UICameraLengthZ;
        _camera.transform.position  = playerTransform.position + add + Vector3.up * camParam.UICameraLengthY;
        _camera.transform.LookAt(playerTransform.position + Vector3.up * camParam.UICameraLookAtCorrectY);
        _camera.Render();
    }

    /// <summary>
    /// ���������܂�
    /// </summary>
    /// <param name="angleY">����������Y���J�����A���O��</param>
    public void Init( float angleY )
    {
        _camareAngleY = angleY;
    }

    /// <summary>
    /// ����HP�p�e�L�X�g��Ԃ��܂�
    /// </summary>
    /// <returns>����HP�p�e�L�X�g</returns>
    public TextMeshProUGUI GetDiffHPText()
    {
        return TMPDiffHPValue;
    }

    /// <summary>
    /// �w��̃X�L���{�b�N�XUI���擾���܂�
    /// </summary>
    /// <param name="index"></param>
    /// <returns>�w��l</returns>
    public SkillBoxUI GetSkillBox( int index )
    {
        Debug.Assert( 0 <= index && index < Constants.EQUIPABLE_SKILL_MAX_NUM );

        return SkillBoxes[index];
    }

    /// <summary>
    /// �\������L�����N�^�[��ݒ肵�܂�
    /// </summary>
    /// <param name="character">�\���L�����N�^�[</param>
    public void SetCharacter( Character character )
    {
        _character = character;
    }

    /// <summary>
    /// �U����Ԃ��ۂ���ݒ肵�܂�
    /// </summary>
    /// <param name="isAttacking">�U����Ԃ�</param>
    public void SetAttacking( bool isAttacking )
    {
        _isAttacking = isAttacking;
    }
}