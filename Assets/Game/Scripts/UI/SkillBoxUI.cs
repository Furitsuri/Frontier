using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Frontier.Combat;
using Frontier.Entities;

namespace Frontier
{
    public sealed class SkillBoxUI : UIMonoBehaviourIncludingText, ITooltipContent
    {
        [SerializeField] private TextMeshProUGUI TMPSkillName;
        [SerializeField] private RectTransform PanelTransform;
        [SerializeField] private RawImage ActGaugeElemImage;
        [SerializeField] private Image CurtainImage;

        private string _tooltipText;
        private ColorFlicker<ImageColorAdapter> _imageFlicker;
        private List<RawImage> _actGaugeElems;
        private Image _uiImage;
        private Color _initialColor;

        void Update()
        {
            _imageFlicker.UpdateFlick();
        }

        /// <summary>
        /// OnEnableでEnableRefreshText()を呼び出さないため、空実装にしています。
        /// </summary>
        void OnEnable() { }

        public void ApplySkill( Character chara, int skillIdx )
        {
            SkillID skillID     = chara.GetEquipSkillID( skillIdx );
            bool isValid        = SkillsData.IsValidSkill( skillID );
            gameObject.SetActive( isValid );
            if( !isValid ) { return; }

            var skillData       = SkillsData.data[( int ) chara.GetEquipSkillID( skillIdx )];
            string skillName    = skillData.Name;
            var situationType   = skillData.SituationType;
            _textKey            = skillData.ExplainTextKey;
            SetSkillName( skillName, situationType );
            SetTooltipText( _textKey );
            ShowSkillCostImage( SkillsData.data[( int ) chara.GetEquipSkillID( skillIdx )].Cost );

            EnableRefreshText();
        }

        public void SetUsing()
        {
            _uiImage.color = Color.white;
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

        /// <summary>
        /// スキル使用の可否を示します
        /// </summary>
        /// <param name="useable">使用の可否</param>
        public void SetUseable( bool useable )
        {
            if( useable )
            {
                CurtainImage.color = new Color( 0, 0, 0, 0 );
            }
            else
            {
                CurtainImage.color = new Color( 0, 0, 0, 0.75f );
            }
        }

        /// <summary>
        /// フリック描画を停止します
        /// </summary>
        public void StopFlick()
        {
            _imageFlicker.StopFlickOnStart();
        }

        /// <summary>
        /// スキル名テキストを設定します
        /// </summary>
        /// <param name="name">設定するスキル名</param>
        private void SetSkillName( string name, SituationType type )
        {
            Color[] typeColor = new Color[( int ) SituationType.NUM] { Color.red, new Color( 0.1f, 0.6f, 1.0f ), Color.yellow };

            TMPSkillName.text = name.Replace( "_", Environment.NewLine );
            TMPSkillName.color = typeColor[( int ) type];
        }

        /// <summary>
        /// スキルのコストをUIで表示します
        /// </summary>
        /// <param name="cost">スキルコスト</param>
        private void ShowSkillCostImage( int cost )
        {
            // アクションゲージの表示
            for( int i = 0; i < Constants.ACTION_GAUGE_MAX; ++i )
            {
                var elem = _actGaugeElems[i];

                elem.gameObject.SetActive( i < cost );
                elem.color = Color.green;
            }
        }

        public override void Setup()
        {
            LazyInject.GetOrCreate( ref _actGaugeElems, () => new List<RawImage>( Constants.ACTION_GAUGE_MAX ) );
            LazyInject.GetOrCreate( ref _uiImage, () => GetComponent<Image>() );
            LazyInject.GetOrCreate( ref _imageFlicker, () => new ColorFlicker<ImageColorAdapter>( new ImageColorAdapter( _uiImage ) ) );

            _initialColor = _uiImage.color;

            if( 0 < _actGaugeElems.Count ) { return; }

            for( int i = 0; i < Constants.ACTION_GAUGE_MAX; ++i )
            {
                var elem = Instantiate( ActGaugeElemImage );
                _actGaugeElems.Add( elem );
                elem.gameObject.SetActive( false );
                elem.transform.SetParent( PanelTransform, false );
            }
        }

        #region ITooltipContent implementation

        public void SetTooltipText( string textKey )
        {
            _tooltipText = _localization.Get( textKey );
        }

        public string GetTooltipText()
        {
            return _tooltipText;
        }

        public RectTransform GetRectTransform()
        {
            return this.GetComponent<RectTransform>();
        }

        #endregion  // ITooltipContent implementation

        #region ILocalizedText implementation

        public override void RefreshText()
        {
            // スキル名はローカライズ対象外のため、何もしない

            SetTooltipText( _textKey );
        }

        #endregion  // ILocalizedText implementation
    }
}