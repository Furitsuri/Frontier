using Frontier.Combat;
using Frontier.Entities;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Frontier.Combat.Skill;

namespace Frontier
{
    public class CharacterParameterUI : MonoBehaviour
    {
        private enum SIDE
        {
            LEFT = 0,
            RIGHT
        }

        [SerializeField] private SIDE _side;
        [SerializeField] private int _layerMaskIndex = 0;
        [SerializeField] private float _camareAngleY;
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
        
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        private Character _character;
        private Camera _camera;
        private RenderTexture _targetTexture;
        private List<RawImage> _actGaugeElems;
        private float _alpha;
        private float _blinkingElapsedTime;

        void Start()
        {
            Debug.Assert( _hierarchyBld != null, "HierarchyBuilderBaseのインスタンスが生成されていません。Injectの設定を確認してください。" );

            var layerToName = LayerMask.LayerToName( _layerMaskIndex );
            _targetTexture = new RenderTexture( ( int ) TargetImage.rectTransform.rect.width * 2, ( int ) TargetImage.rectTransform.rect.height * 2, 16, RenderTextureFormat.ARGB32 );
            TargetImage.texture = _targetTexture;
            _camera = _hierarchyBld.CreateComponentAndOrganize<Camera>( true, "CharaParamCamera" );
            _camera.enabled = false;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color( 0, 0, 0, 0 );
            _camera.targetTexture = _targetTexture;
            _camera.cullingMask = 1 << LayerMask.NameToLayer( layerToName );
            _camera.gameObject.name = "CharaParamCamera_" + layerToName;

            _actGaugeElems = new List<RawImage>( Constants.ACTION_GAUGE_MAX );

            for( int i = 0; i < Constants.ACTION_GAUGE_MAX; ++i )
            {
                var elem = _hierarchyBld.CreateComponentAndOrganize<RawImage>( ActGaugeElemImage.gameObject, true );
                _actGaugeElems.Add( elem );
                elem.gameObject.SetActive( false );
                elem.transform.SetParent( PanelTransform, false );
            }
        }

        // Update is called once per frame
        void Update()
        {
            Debug.Assert( _character != null );   // キャラクターがnullの状態でGameObjectがActiveになっていることは想定しない

            UpdateParamRender( _character, _character.Params.CharacterParam, _character.Params.SkillModifiedParam );  // パラメータ表示を反映
            UpdateCameraRender( _character, _character.Params.CameraParam );  // カメラ描画を反映
        }

        /// <summary>
        /// パラメータUIに表示するキャラクターのパラメータを更新します
        /// </summary>
        /// <param name="selectCharacter">選択しているキャラクター</param>
        /// <param name="param">選択しているキャラクターのパラメータ</param>
        void UpdateParamRender( Character selectCharacter, in CharacterParameter param, in SkillModifiedParameter skillParam )
        {
            Debug.Assert( param.consumptionActionGauge <= param.curActionGauge );

            TMPMaxHPValue.text          = $"{param.MaxHP}";
            TMPCurHPValue.text          = $"{param.CurHP}";
            TMPAtkValue.text            = $"{param.Atk}";
            TMPDefValue.text            = $"{param.Def}";
            TMPAtkNumValue.text         = $"x {skillParam.AtkNum}";
            TMPActRecoveryValue.text    = $"+{param.recoveryActionGauge}";
            TMPAtkNumValue.gameObject.SetActive( 1 < skillParam.AtkNum );

            int hpChange, totalHpChange;
            selectCharacter.Params.TmpParam.AssignExpectedHpChange( out hpChange, out totalHpChange );

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

            // スキルボックスの表示
            for( int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                if( param.IsValidSkill( i ) )
                {
                    SkillBoxes[i].gameObject.SetActive( true );
                    string skillName = SkillsData.data[( int ) param.equipSkills[i]].Name;
                    var type = SkillsData.data[( int ) param.equipSkills[i]].Type;
                    SkillBoxes[i].SetSkillName( skillName, type );
                    SkillBoxes[i].ShowSkillCostImage( SkillsData.data[( int ) param.equipSkills[i]].Cost );
                }
                else
                {
                    SkillBoxes[i].gameObject.SetActive( false );
                }
            }
        }

        /// <summary>
        /// テキストの色を反映します
        /// </summary>
        /// <param name="changeHP">HPの変動量</param>
        void ApplyTextColor( int changeHP )
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

        /// <summary>
        /// パラメータUIに表示するキャラクターのカメラ描画を更新します
        /// </summary>
        /// <param name="selectCharacter">選択しているキャラクター</param>
        /// <param name="param">選択しているキャラクターのパラメータ</param>
        void UpdateCameraRender( Character selectCharacter, in CameraParameter camParam )
        {
            Transform characterTransform = selectCharacter.transform;
            Vector3 add = Quaternion.AngleAxis( _camareAngleY, Vector3.up ) * characterTransform.forward * camParam.UICameraLengthZ;
            _camera.transform.position = characterTransform.position + add + Vector3.up * camParam.UICameraLengthY;
            _camera.transform.LookAt( characterTransform.position + Vector3.up * camParam.UICameraLookAtCorrectY );
            _camera.Render();
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        public void Init()
        {
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
        public void SetDisplayCharacter( Character character, int layerMaskIndex )
        {
            // 以前ディスプレイに設定していたキャラクターのレイヤーマスクを元に戻す
            if( null != _character )
            {
                _character.gameObject.SetLayerRecursively( Constants.LAYER_MASK_INDEX_CHARACTER );
            }

            _character = character;

            // パラメータ画面表示用にキャラクターのレイヤーを変更
            _character.gameObject.SetLayerRecursively( layerMaskIndex );
        }

        /// <summary>
        /// キャラクターのレイヤーマスクを元に戻します
        /// </summary>
        public void ClearDisplayCharacter()
        {
            if( null == _character ) { return; }

            _character.gameObject.SetLayerRecursively( Constants.LAYER_MASK_INDEX_CHARACTER );
        }
    }
}