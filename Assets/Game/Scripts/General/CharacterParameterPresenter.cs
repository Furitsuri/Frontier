using Frontier;
using Frontier.Entities;
using Frontier.StateMachine;
using Frontier.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class CharacterParameterPresenter : PhasePresenterBase
{
    [Inject] private HierarchyBuilderBase _hierarchyBld = null;

    private int _layerMaskIndex = 0;
    private float _cameraAngleY;
    private float _blinkingDuration;
    private Character _character;
    private CharacterCamera _characterCamera;
    private CharacterParameterUI _parameterUI;
    private RenderTexture _targetTexture;
    private List<RawImage> _actGaugeElems = new List<RawImage>( Constants.ACTION_GAUGE_MAX );
    private float _alpha;
    private float _blinkingElapsedTime;

    [Inject] public CharacterParameterPresenter( CharacterParameterUI parameterUI, bool isNeedCamera, HierarchyBuilderBase hierarchyBld )
    {
        _hierarchyBld       = hierarchyBld;
        _parameterUI        = parameterUI;
        _layerMaskIndex     = _parameterUI._layerMaskIndex;
        _cameraAngleY       = parameterUI._cameraAngleY;
        _blinkingDuration   = parameterUI.BlinkingDuration;

        if( isNeedCamera )
        {
            LazyInject.GetOrCreate( ref _characterCamera, () => _hierarchyBld.InstantiateWithDiContainer<CharacterCamera>( false ) );
            _characterCamera.Setup( _parameterUI.gameObject, "CharacterParameterCamera" );
        }

        if( null != _parameterUI.TargetImage )
        {
            LazyInject.GetOrCreate( ref _targetTexture, () => new RenderTexture( ( int ) _parameterUI.TargetImage.rectTransform.rect.width * 2, ( int ) _parameterUI.TargetImage.rectTransform.rect.height * 2, 16, RenderTextureFormat.ARGB32 ) );
        }
    }

    public void Init()
    {
        if( null != _parameterUI.TargetImage )
        {
            var layerToName = LayerMask.LayerToName( _layerMaskIndex );
            _parameterUI.TargetImage.texture = _targetTexture;
            _characterCamera?.Init( "CharaParamCamera_" + layerToName, _layerMaskIndex, _cameraAngleY, ref _parameterUI.TargetImage );
        }

        for( int i = 0; i < Constants.ACTION_GAUGE_MAX; ++i )
        {
            var elem = _hierarchyBld.CreateComponentAndOrganize<RawImage>( _parameterUI.ActGaugeElemImage.gameObject, true );
            _actGaugeElems.Add( elem );
            elem.gameObject.SetActive( false );
            elem.transform.SetParent( _parameterUI.PanelTransform, false );
        }
    }

    public void Update()
    {
        if( null == _character ) { return; }

        _characterCamera?.Update( _character.CameraParam );

        RefreshParamRender( _character, _character.GetStatusRef, _character.BattleParams.SkillModifiedParam );
    }

    public void SetActive( bool isActive )
    {
        _parameterUI.gameObject.SetActive( isActive );
    }

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

        // キャラクターのパラメータを反映
        RefreshParamRender( _character, _character.GetStatusRef, _character.BattleParams.SkillModifiedParam );
    }

    public void SetSkillBoxToUsing( int skillIndex )
    {
        _parameterUI.SkillBoxes[skillIndex].SetUsing();
    }

    private void RefreshParamRender( Character selectCharacter, in Status param, in SkillModifiedParameter skillParam )
    {
        Debug.Assert( param.ActGaugeConsumption <= param.CurActionGauge );

        _parameterUI.TMPMaxHPValue.text         = $"{param.MaxHP}";
        _parameterUI.TMPCurHPValue.text         = $"{param.CurHP}";
        _parameterUI.TMPAtkValue.text           = $"{param.Atk}";
        _parameterUI.TMPDefValue.text           = $"{param.Def}";
        _parameterUI.TMPMovValue.text           = $"{param.moveRange}";
        _parameterUI.TMPJmpValue.text           = $"{param.jumpForce}";
        _parameterUI.TMPAtkNumValue.text        = $"x {skillParam.AtkNum}";
        _parameterUI.TMPActRecoveryValue.text   = $"+{param.recoveryActionGauge}";
        _parameterUI.TMPAtkNumValue.gameObject.SetActive( 1 < skillParam.AtkNum );

        int hpChange, totalHpChange;
        selectCharacter.BattleParams.TmpParam.AssignExpectedHpChange( out hpChange, out totalHpChange );

        totalHpChange = Mathf.Clamp( totalHpChange, -param.CurHP, param.MaxHP - param.CurHP );
        if( 0 < totalHpChange )
        {
            _parameterUI.TMPDiffHPValue.text = $"+{totalHpChange}";
        }
        else if( totalHpChange < 0 )
        {
            _parameterUI.TMPDiffHPValue.text = $"{totalHpChange}";
        }
        else
        {
            // ダメージが0の場合は表示しない
            _parameterUI.TMPDiffHPValue.text = "";
        }

        // テキストの色を反映
        _parameterUI.ApplyTextColor( totalHpChange );

        // アクションゲージの表示
        for( int i = 0; i < Constants.ACTION_GAUGE_MAX; ++i )
        {
            var elem = _actGaugeElems[i];

            if( i <= param.maxActionGauge - 1 )
            {
                elem.gameObject.SetActive( true );

                if( i <= param.CurActionGauge - 1 )
                {
                    elem.color = Color.green;

                    // アクションゲージ使用時は点滅させる
                    if( ( param.CurActionGauge - param.ActGaugeConsumption ) <= i )
                    {
                        _blinkingElapsedTime += DeltaTimeProvider.DeltaTime;
                        _alpha = Mathf.PingPong( _blinkingElapsedTime / _blinkingDuration, 1.0f );
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
            _parameterUI.SkillBoxes[i].ApplySkill( selectCharacter, i );
            _parameterUI.SkillBoxes[i].SetUseable( selectCharacter.BattleParams.TmpParam.IsUseableSkill[i] );
            _parameterUI.SkillBoxes[i].SetFlickEnabled( selectCharacter.BattleParams.TmpParam.IsSkillsToggledON[i] );
        }
    }
}
