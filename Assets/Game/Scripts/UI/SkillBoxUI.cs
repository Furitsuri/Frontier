using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class SkillBoxUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI TMPSkillName;
    [SerializeField]
    private RectTransform PanelTransform;
    [SerializeField]
    private RawImage ActGaugeElemImage;
    [SerializeField]
    private UnityEngine.UI.Image CurtainImage;

    private ColorFlicker<ImageColorAdapter> _imageFlicker;
    private List<RawImage> _actGaugeElems;
    private UnityEngine.UI.Image _uiImage;
    private Color _initialColor;

    private void Start()
    {
        _actGaugeElems  = new List<RawImage>(Constants.ACTION_GAUGE_MAX);
        _uiImage        = GetComponent<UnityEngine.UI.Image>();
        _imageFlicker   = new ColorFlicker<ImageColorAdapter>(new ImageColorAdapter(_uiImage));
        _initialColor   = _uiImage.color;

        for (int i = 0; i < Constants.ACTION_GAUGE_MAX; ++i)
        {
            var elem = Instantiate(ActGaugeElemImage);
            _actGaugeElems.Add(elem);
            elem.gameObject.SetActive(false);
            elem.transform.SetParent(PanelTransform, true);
        }
    }

    private void Update()
    {
        _imageFlicker.UpdateFlick();
    }

    /// <summary>
    /// スキル名テキストを設定します
    /// </summary>
    /// <param name="name">設定するスキル名</param>
    public void SetSkillName( string name )
    {
        TMPSkillName.text = name.Replace( "_", Environment.NewLine );
    }

    /// <summary>
    /// 拝啓イメージのカラーをフリックするか否かを設定します
    /// </summary>
    /// <param name="enabled">フリックのON・OFF</param>
    public void SetFlickEnabled( bool enabled )
    {
        _imageFlicker.setEnabled( enabled );

        // 選択から外された場合は色を元に戻す
        if( !enabled )
        {
            _uiImage.color = _initialColor;
        }
    }

    public void SetUseable( bool useable )
    {
        if( useable )
        {
            CurtainImage.color = new Color(0,0,0,0);
        }
        else
        {
            CurtainImage.color = new Color(0, 0, 0, 0.75f);
        }
    }

    /// <summary>
    /// スキルのコストをUIで表示します
    /// </summary>
    /// <param name="cost">スキルコスト</param>
    public void ShowSkillCostImage( int cost )
    {
        // アクションゲージの表示
        for (int i = 0; i < Constants.ACTION_GAUGE_MAX; ++i)
        {
            var elem = _actGaugeElems[i];

            elem.gameObject.SetActive(i < cost);
            elem.color = Color.green;
        }
    }
}
