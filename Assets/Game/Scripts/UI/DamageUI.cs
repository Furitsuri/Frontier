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
        // キャラクターの座標からUIカメラ(スクリーン)座標に変換
        var pos         = Vector2.zero;
        var worldCamera = Camera.main;
        var screenPos   = RectTransformUtility.WorldToScreenPoint(worldCamera, CharacterTransform.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_btlUiRectTransform, screenPos, _btlUiCamera, out pos);
        GetComponent<RectTransform>().localPosition = pos;
    }

    /// <summary>
    /// 初期化します
    /// </summary>
    /// <param name="rect">BattleUISystemのRectTransform</param>
    /// <param name="uiCamera">BattleUISystemに用いるUI用カメラ</param>
    public void Init( RectTransform rect, Camera uiCamera )
    {
        _btlUiRectTransform = rect;
        _btlUiCamera        = uiCamera;
    }
}
