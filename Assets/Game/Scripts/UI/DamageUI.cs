using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DamageUI : MonoBehaviour
{
    private RectTransform _btlUiRectTransform;
    private Camera _btlUiCamera;
    public TextMeshProUGUI damageText;
    public Transform CharacterTransform { get; set; }

    void Update()
    {
        // �L�����N�^�[�̍��W����UI�J����(�X�N���[��)���W�ɕϊ�
        var pos         = Vector2.zero;
        var worldCamera = Camera.main;
        var screenPos   = RectTransformUtility.WorldToScreenPoint(worldCamera, CharacterTransform.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_btlUiRectTransform, screenPos, _btlUiCamera, out pos);
        GetComponent<RectTransform>().localPosition = pos;
    }

    /// <summary>
    /// ���������܂�
    /// </summary>
    /// <param name="rect">BattleUISystem��RectTransform</param>
    /// <param name="uiCamera">BattleUISystem�ɗp����UI�p�J����</param>
    public void Init( RectTransform rect, Camera uiCamera )
    {
        _btlUiRectTransform = rect;
        _btlUiCamera        = uiCamera;
    }
}
