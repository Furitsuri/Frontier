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

        UpdateParamRender( _character, _character.GetStatusRef, _character.BattleParams.ModifiedParam, _character.BattleParams.SkillModifiedParam );
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
        _character.RegistParameterPresenter( this );

        _character.gameObject.SetActive( true );
        _characterCamera?.AssignCharacter( character, layerMaskIndex );

        // キャラクターのパラメータを反映
        RefreshParamRender( _character, _character.GetStatusRef, _character.BattleParams.ModifiedParam );
    }

    public void SetSkillBoxToUsing( int skillIndex )
    {
        _parameterUI.SkillBoxes[skillIndex].SetUsing();
    }

    public void RefreshParamRender( Character selectCharacter, in Status status, in ModifiedParameter modifiedParam )
    {
        Debug.Assert( selectCharacter.BattleParams.TmpParam.ActGaugeConsumption <= status.CurActionGauge );

        _parameterUI.TMPMaxHPValue.text         = $"{status.MaxHP}";
        _parameterUI.TMPCurHPValue.text         = $"{status.CurHP}";
        _parameterUI.TMPAtkValue.text           = $"{status.Atk}";
        _parameterUI.TMPDefValue.text           = $"{status.Def}";
        _parameterUI.TMPMovValue.text           = $"{status.moveRange}";
        _parameterUI.TMPJmpValue.text           = $"{status.jumpForce}";

        if( null != _parameterUI.TMPAddAtkValue )
        {
            int addAtkValue                     = ( int ) ( modifiedParam.Atk );
            var addAtkText                      = ( addAtkValue < 0 ) ? $"- {addAtkValue}" : $"+ {addAtkValue}";
            _parameterUI.TMPAddAtkValue.text    = addAtkText;
            _parameterUI.TMPAddAtkValue.color   = ( addAtkValue < 0 ) ? Color.blue : Color.green;
            _parameterUI.TMPAddAtkValue.gameObject.SetActive( addAtkValue != 0 );
        }
        if( null != _parameterUI.TMPAddDefValue )
        {
            int addDefValue                     = ( int ) ( modifiedParam.Def );
            var addDefText                      = ( addDefValue < 0 ) ? $"- {addDefValue}" : $"+ {addDefValue}";
            _parameterUI.TMPAddDefValue.text    = addDefText;
            _parameterUI.TMPAddDefValue.color   = ( addDefValue < 0 ) ? Color.blue : Color.green;
            _parameterUI.TMPAddDefValue.gameObject.SetActive( addDefValue != 0 );
        }

        _parameterUI.TMPActRecoveryValue.text   = $"+ {status.recoveryActionGauge}";

        int hpChange, totalHpChange;
        selectCharacter.BattleParams.TmpParam.AssignExpectedHpChange( out hpChange, out totalHpChange );

        totalHpChange = Mathf.Clamp( totalHpChange, -status.CurHP, status.MaxHP - status.CurHP );
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

            if( i <= status.maxActionGauge - 1 )
            {
                elem.gameObject.SetActive( true );

                if( i <= status.CurActionGauge - 1 )
                {
                    elem.color = Color.green;

                    // アクションゲージ使用時の点滅開始
                    if( ( status.CurActionGauge - selectCharacter.BattleParams.TmpParam.ActGaugeConsumption ) <= i )
                    {
                        _blinkingElapsedTime = 0f;
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
            _parameterUI.SkillBoxes[i].SetUseableOrNot( selectCharacter.BattleParams.TmpParam.IsUseableSkill[i] );
            _parameterUI.SkillBoxes[i].SetFlickEnabled( selectCharacter.BattleParams.TmpParam.IsSkillsToggledON[i] );
        }
    }

    private void UpdateParamRender( Character selectCharacter, in Status status, in ModifiedParameter modifiedParam, in SkillModifiedParameter skillParam )
    {
        _parameterUI.TMPMaxHPValue.text = $"{status.MaxHP}";
        _parameterUI.TMPCurHPValue.text = $"{status.CurHP}";
        _parameterUI.TMPAtkValue.text   = $"{status.Atk}";
        _parameterUI.TMPDefValue.text   = $"{status.Def}";
        _parameterUI.TMPMovValue.text   = $"{status.moveRange}";
        _parameterUI.TMPJmpValue.text   = $"{status.jumpForce}";

        int hpChange, totalHpChange;
        selectCharacter.BattleParams.TmpParam.AssignExpectedHpChange( out hpChange, out totalHpChange );

        totalHpChange = Mathf.Clamp( totalHpChange, -status.CurHP, status.MaxHP - status.CurHP );
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

        // アクションゲージの表示
        for( int i = 0; i < Constants.ACTION_GAUGE_MAX; ++i )
        {
            var elem = _actGaugeElems[i];

            if( i <= status.maxActionGauge - 1 )
            {
                if( i <= status.CurActionGauge - 1 )
                {
                    // アクションゲージ使用時は点滅させる
                    if( ( status.CurActionGauge - selectCharacter.BattleParams.TmpParam.ActGaugeConsumption ) <= i )
                    {
                        _blinkingElapsedTime += DeltaTimeProvider.DeltaTime;
                        _alpha      = Mathf.PingPong( _blinkingElapsedTime / _blinkingDuration, 1.0f );
                        elem.color  = new Color( 0, 1, 0, _alpha );
                    }
                }
            }
        }

        // スキルボックスUIの表示
        for( int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i )
        {
            _parameterUI.SkillBoxes[i].UpdateImageFlick();
        }
    }
}
