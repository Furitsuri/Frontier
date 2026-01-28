using Frontier.Combat;
using Frontier.Entities;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Frontier.Combat.Skill;

namespace Frontier.UI
{
    public class CharacterParameterUI : UiMonoBehaviour
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        [SerializeField] private int _layerMaskIndex = 0;
        [SerializeField] private float _cameraAngleY;
        [SerializeField] private float BlinkingDuration;
        [SerializeField] private TextMeshProUGUI TMPMaxHPValue;
        [SerializeField] private TextMeshProUGUI TMPCurHPValue;
        [SerializeField] private TextMeshProUGUI TMPAtkValue;
        [SerializeField] private TextMeshProUGUI TMPDefValue;
        [SerializeField] private TextMeshProUGUI TMPAtkNumValue;
        [SerializeField] private TextMeshProUGUI TMPDiffHPValue;
        [SerializeField] private TextMeshProUGUI TMPActRecoveryValue;
        [SerializeField] private RawImage TargetImage;
        [SerializeField] private RawImage ActGaugeElemImage;
        [SerializeField] private RectTransform PanelTransform;
        [SerializeField] private SkillBoxUI[] SkillBoxes;

        private Character _character;
        private CharacterCamera _characterCamera;
        private RenderTexture _targetTexture;
        private List<RawImage> _actGaugeElems = new List<RawImage>( Constants.ACTION_GAUGE_MAX );
        private float _alpha;
        private float _blinkingElapsedTime;

        // Update is called once per frame
        void Update()
        {
            Debug.Assert( _character != null );   // キャラクターがnullの状態でGameObjectがActiveになっていることは想定しない

            _characterCamera?.Update( _character.CameraParam );

            UpdateParamRender( _character, _character.GetStatusRef, _character.RefBattleParams.SkillModifiedParam );  // パラメータ表示を反映
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        public void Init()
        {
            var layerToName         = LayerMask.LayerToName( _layerMaskIndex );
            TargetImage.texture     = _targetTexture;

            _characterCamera?.Init( "CharaParamCamera_" + layerToName, _layerMaskIndex, _cameraAngleY, ref TargetImage );

            for( int i = 0; i < Constants.ACTION_GAUGE_MAX; ++i )
            {
                var elem = _hierarchyBld.CreateComponentAndOrganize<RawImage>( ActGaugeElemImage.gameObject, true );
                _actGaugeElems.Add( elem );
                elem.gameObject.SetActive( false );
                elem.transform.SetParent( PanelTransform, false );
            }
        }

        /// <summary>
        /// 差分HP用テキストを返します
        /// </summary>
        /// <returns>差分HP用テキスト</returns>
        public TextMeshProUGUI GetDiffHPText()
        {
            return TMPDiffHPValue;
        }

        /// <summary>
        /// 指定のスキルボックスUIを取得します
        /// </summary>
        /// <param name="index"></param>
        /// <returns>指定値</returns>
        public SkillBoxUI GetSkillBox( int index )
        {
            Debug.Assert( 0 <= index && index < Constants.EQUIPABLE_SKILL_MAX_NUM );

            return SkillBoxes[index];
        }

        /// <summary>
        /// 表示するキャラクターを設定します
        /// </summary>
        /// <param name="character">表示キャラクター</param>
        public void AssignCharacter( Character character, int layerMaskIndex )
        {
            // 以前ディスプレイに設定していたキャラクターのレイヤーマスクを元に戻す
            if( null != _character && _character != character )
            {
                _character.gameObject.SetLayerRecursively( Constants.LAYER_MASK_INDEX_CHARACTER );
            }

            _character = character;

            _character.gameObject.SetActive( true );
            _characterCamera?.AssignCharacter( character, layerMaskIndex );
        }

        /// <summary>
        /// パラメータUIに表示するキャラクターのパラメータを更新します
        /// </summary>
        /// <param name="selectCharacter">選択しているキャラクター</param>
        /// <param name="param">選択しているキャラクターのパラメータ</param>
        private void UpdateParamRender( Character selectCharacter, in Status param, in SkillModifiedParameter skillParam )
        {
            Debug.Assert( param.consumptionActionGauge <= param.curActionGauge );

            TMPMaxHPValue.text = $"{param.MaxHP}";
            TMPCurHPValue.text = $"{param.CurHP}";
            TMPAtkValue.text = $"{param.Atk}";
            TMPDefValue.text = $"{param.Def}";
            TMPAtkNumValue.text = $"x {skillParam.AtkNum}";
            TMPActRecoveryValue.text = $"+{param.recoveryActionGauge}";
            TMPAtkNumValue.gameObject.SetActive( 1 < skillParam.AtkNum );

            int hpChange, totalHpChange;
            selectCharacter.RefBattleParams.TmpParam.AssignExpectedHpChange( out hpChange, out totalHpChange );

            totalHpChange = Mathf.Clamp( totalHpChange, -param.CurHP, param.MaxHP - param.CurHP );
            if( 0 < totalHpChange )
            {
                TMPDiffHPValue.text = $"+{totalHpChange}";
            }
            else if( totalHpChange < 0 )
            {
                TMPDiffHPValue.text = $"{totalHpChange}";
            }
            else
            {
                // ダメージが0の場合は表示しない
                TMPDiffHPValue.text = "";
            }

            // テキストの色を反映
            ApplyTextColor( totalHpChange );

            // アクションゲージの表示
            for( int i = 0; i < Constants.ACTION_GAUGE_MAX; ++i )
            {
                var elem = _actGaugeElems[i];

                if( i <= param.maxActionGauge - 1 )
                {
                    elem.gameObject.SetActive( true );

                    if( i <= param.curActionGauge - 1 )
                    {
                        elem.color = Color.green;

                        // アクションゲージ使用時は点滅させる
                        if( ( param.curActionGauge - param.consumptionActionGauge ) <= i )
                        {
                            _blinkingElapsedTime += DeltaTimeProvider.DeltaTime;
                            _alpha = Mathf.PingPong( _blinkingElapsedTime / BlinkingDuration, 1.0f );
                            elem.color = new Color( 0, 1, 0, _alpha );
                        }
                    }
                    else
                    {
                        elem.color = Color.gray;
                    }
                }
                else
                {
                    elem.gameObject.SetActive( false );
                }
            }

            // スキルボックスUIの表示
            for( int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                SkillBoxes[i].ApplySkill( selectCharacter, i );
            }
        }

        /// <summary>
        /// テキストの色を反映します
        /// </summary>
        /// <param name="changeHP">HPの変動量</param>
        private void ApplyTextColor( int changeHP )
        {
            if( changeHP < 0 )
            {
                TMPDiffHPValue.color = Color.red;
            }
            else if( 0 < changeHP )
            {
                TMPDiffHPValue.color = Color.green;
            }
        }

        public override void Setup()
        {
            LazyInject.GetOrCreate( ref _targetTexture, () => new RenderTexture( ( int ) TargetImage.rectTransform.rect.width * 2, ( int ) TargetImage.rectTransform.rect.height * 2, 16, RenderTextureFormat.ARGB32 ) );
            LazyInject.GetOrCreate( ref _characterCamera, () => _hierarchyBld.InstantiateWithDiContainer<CharacterCamera>( false ) );

            foreach( var item in SkillBoxes )
            {
                item.Setup();
            }
        }
    }
}