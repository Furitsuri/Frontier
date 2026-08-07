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
        [SerializeField] private GameObject CursorFrame;

        private string _tooltipText;
        private ColorFlicker<ImageColorAdapter> _imageFlicker;
        private List<RawImage> _actGaugeElems;
        private Image _uiImage;
        private Color _initialColor;

        /// <summary>
        /// OnEnableでEnableRefreshText()を呼び出さないため、空実装にしています。
        /// </summary>
        void OnEnable() { }

        public void UpdateImageFlick()
        {
            _imageFlicker.UpdateFlick();
        }

        public void ApplySkill( Character chara, int skillIdx )
        {
            ApplySkill( chara.GetEquipSkillID( skillIdx ) );
        }

        /// <summary>
        /// 指定したSkillIDの内容を表示します。無効なSkillIDの場合はゲームオブジェクト自体を非表示にします
        /// (戦闘中のパラメータ表示等、未装備のスキル枠を詰めて表示したい場面向け)。
        /// 装備スキル設定画面のように、空枠であっても選択できるよう表示を維持したい場合はApplySkillForEditingを使用してください。
        /// </summary>
        public void ApplySkill( SkillID skillID )
        {
            bool isValid = SkillsData.IsValidSkill( skillID );
            gameObject.SetActive( isValid );
            if( !isValid ) { return; }

            ApplySkillContent( skillID );
        }

        /// <summary>
        /// 指定したSkillIDの内容を表示します。ApplySkillと異なり、無効なSkillID(未装備枠)の場合も
        /// ゲームオブジェクトは非表示にせず、空枠として表示します(装備スキル設定画面で、
        /// 未装備の枠にもカーソルを合わせて新しくスキルを装備できるようにするため)。
        /// </summary>
        public void ApplySkillForEditing( SkillID skillID )
        {
            gameObject.SetActive( true );

            if( !SkillsData.IsValidSkill( skillID ) )
            {
                SetSkillName( "", SituationType.ATTACK );
                SetTooltipText( "" );
                ShowSkillCostImage( 0 );
                EnableRefreshText();
                return;
            }

            ApplySkillContent( skillID );
        }

        private void ApplySkillContent( SkillID skillID )
        {
            var skillData      = SkillsData.data[( int ) skillID];
            string skillName   = skillData.Name;
            var situationType  = skillData.SituationType;
            _textKey           = skillData.ExplainTextKey;
            SetSkillName( skillName, situationType );
            SetTooltipText( _textKey );
            ShowSkillCostImage( skillData.Cost );

            EnableRefreshText();
        }

        public void SetUsing()
        {
            _uiImage.color = Color.white;
        }

        private static readonly Vector3 CURSOR_HIGHLIGHT_SCALE = new Vector3( 1.1f, 1.1f, 1.1f );

        /// <summary>
        /// カーソルが現在この項目を指しているかどうかを見た目(外枠、及び任意で拡大表示)で示します。
        /// scaleUpをfalseにすると拡大表示を行わず、外枠のみで示します(装備スキル設定画面のように、
        /// 枠のサイズを他の要因(初期スケール等)で固定しておきたい場面向け)。
        /// </summary>
        public void SetCursorHighlighted( bool isHighlighted, bool scaleUp = true )
        {
            if( scaleUp )
            {
                transform.localScale = isHighlighted ? CURSOR_HIGHLIGHT_SCALE : Vector3.one;
            }

            // CursorFrameは未アサインの場合があり、その際はUnityの「フェイクnull」参照になるため、
            // ?.(null条件演算子)ではなく!=nullで判定する(?.は生のC#参照比較になり、
            // 未アサイン状態を正しくnull扱いできずSetActive呼び出しで例外になるため)。
            if( CursorFrame != null ) { CursorFrame.SetActive( isHighlighted ); }
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
        public void SetUseableOrNot( bool useable )
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

            // SkillsData側で名前が未設定(null)のスキルでも表示自体はクラッシュさせない
            TMPSkillName.text = name?.Replace( "_", Environment.NewLine ) ?? "";
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
            // SkillsData側で説明文キーが未設定(null)のスキルでも表示自体はクラッシュさせない
            _tooltipText = string.IsNullOrEmpty( textKey ) ? "" : _localization.Get( textKey );
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