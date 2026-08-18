using Frontier;
using Frontier.Entities;
using Frontier.StateMachine;
using Frontier.UI;
using System;
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

    // 背景色をキャラクターに応じて決定する処理。呼び出し元(戦闘画面ならBattleRoutinePresenter等)が
    // SetBackgroundColorResolverで差し込む。未設定(null)の画面ではHierarchy側の既定の背景色のまま変更しない。
    // CharacterParameterPresenterは配置画面のカルーセルや編成・キャラ編集画面など他の多くの画面でも
    // 使い回されているため、「勢力」や「戦闘」の概念をこのクラス自身は一切知らないようにする。
    private Func<Character, Color> _backgroundColorResolver = null;

    // キャラクター切替スライド演出用(IncomingTargetImageが割り当てられている画面でのみ使用する)。
    // _characterCamera/TargetImageは「切替前」のキャラクターを表示し続け、
    // _incomingCharacterCamera/IncomingTargetImageが「切替後」のキャラクターを表示しながら、
    // 両者のRawImageをUI座標上でスライドさせる。完了後、_characterCamera側を切替後のキャラクターへ
    // retargetして定位置に戻すことで、常にTargetImage側が「静止状態の表示」であるようにしている。
    private CharacterCamera _incomingCharacterCamera;
    private Character _slideOutgoingCharacter;
    private bool _isPortraitSliding;
    private float _slideElapsed;
    private float _slideDuration;
    private SlideDirection _slideDirection;

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

        if( _isPortraitSliding )
        {
            UpdatePortraitSlide();
        }
        else
        {
            _characterCamera?.Update( _character.CameraParam );
        }

        UpdateParamRender( _character, _character.GetStatusRef, _character.BattleParams.ModifiedParam, _character.BattleParams.SkillModifiedParam );
    }

    public void SetActive( bool isActive )
    {
        _parameterUI.gameObject.SetActive( isActive );
    }

    /// <summary>
    /// 背景色をキャラクターに応じて決定する処理を差し込みます。未設定(null)のままなら背景色は変更しません。
    /// 「勢力」や「戦闘」といった概念はこのクラスの外(呼び出し元)で完結させてください。
    /// </summary>
    public void SetBackgroundColorResolver( Func<Character, Color> resolver )
    {
        _backgroundColorResolver = resolver;
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

        // 背景色を反映(resolver未設定の画面では何もしない)
        ApplyBackgroundColor( character );

        // キャラクターのパラメータを反映
        RefreshParamRender( _character, _character.GetStatusRef, _character.BattleParams.ModifiedParam );
    }

    /// <summary>
    /// SetBackgroundColorResolverで差し込まれた処理を使って背景色を適用します。
    /// </summary>
    private void ApplyBackgroundColor( Character character )
    {
        if( null == _backgroundColorResolver || null == _parameterUI.Background ) { return; }

        _parameterUI.Background.color = _backgroundColorResolver( character );
    }

    /// <summary>
    /// 表示中の3Dモデルをスライドさせながらキャラクターを切り替えます(数値パラメータは即時反映)。
    /// カメラ未使用(isNeedCamera:false)、またはIncomingTargetImage未設定の画面では
    /// AssignCharacterと同じ即時切替になります。
    /// </summary>
    /// <param name="direction">
    /// 切替前(fromCharacter)のモデルが移動していく向き。RIGHTなら現在のモデルが右へ、
    /// 次のモデルが左から入ってくる。LEFTならその逆。
    /// </param>
    public void AssignCharacterWithSlide( Character fromCharacter, Character toCharacter, int layerMaskIndex, SlideDirection direction )
    {
        if( null == _characterCamera || null == _parameterUI.IncomingTargetImage )
        {
            AssignCharacter( toCharacter, layerMaskIndex );
            return;
        }

        EnsureIncomingCharacterCamera();

        _slideOutgoingCharacter = fromCharacter;
        _isPortraitSliding      = true;
        _slideDirection         = direction;
        _slideElapsed           = 0f;
        _slideDuration          = Constants.CHARACTER_PARAM_PORTRAIT_SLIDE_DURATION;

        // 数値パラメータ・レイヤー・カーソル対象は即座に切り替える(3Dモデルの表示のみアニメーションさせる)
        if( null != _character && _character != toCharacter )
        {
            _character.gameObject.SetLayerRecursively( Constants.LAYER_MASK_INDEX_CHARACTER );
        }
        _character = toCharacter;
        _character.RegistParameterPresenter( this );
        _character.gameObject.SetActive( true );

        // 背景色を反映(resolver未設定の画面では何もしない)
        ApplyBackgroundColor( toCharacter );

        _incomingCharacterCamera.AssignCharacter( toCharacter, layerMaskIndex );

        float width = _parameterUI.TargetImage.rectTransform.rect.width;
        float sign  = ( direction == SlideDirection.RIGHT ) ? 1f : -1f;
        _parameterUI.TargetImage.rectTransform.anchoredPosition         = Vector2.zero;
        _parameterUI.IncomingTargetImage.gameObject.SetActive( true );
        _parameterUI.IncomingTargetImage.rectTransform.anchoredPosition = new Vector2( -sign * width, 0f );

        RefreshParamRender( _character, _character.GetStatusRef, _character.BattleParams.ModifiedParam );
    }

    private void EnsureIncomingCharacterCamera()
    {
        if( null != _incomingCharacterCamera ) { return; }

        LazyInject.GetOrCreate( ref _incomingCharacterCamera, () => _hierarchyBld.InstantiateWithDiContainer<CharacterCamera>( false ) );
        _incomingCharacterCamera.Setup( _parameterUI.gameObject, "CharacterParameterCameraIncoming" );

        var layerToName = LayerMask.LayerToName( _layerMaskIndex );
        _incomingCharacterCamera.Init( "CharaParamCamera_" + layerToName + "_Incoming", _layerMaskIndex, _cameraAngleY, ref _parameterUI.IncomingTargetImage );
    }

    private void UpdatePortraitSlide()
    {
        _characterCamera.Update( _slideOutgoingCharacter.CameraParam );
        _incomingCharacterCamera.Update( _character.CameraParam );

        _slideElapsed += DeltaTimeProvider.DeltaTime;
        float rawT = Mathf.Clamp01( _slideElapsed / _slideDuration );
        float t    = rawT * rawT * ( 3f - 2f * rawT );    // smoothstep

        float width = _parameterUI.TargetImage.rectTransform.rect.width;
        float sign  = ( _slideDirection == SlideDirection.RIGHT ) ? 1f : -1f;

        _parameterUI.TargetImage.rectTransform.anchoredPosition         = new Vector2( sign * width * t, 0f );
        _parameterUI.IncomingTargetImage.rectTransform.anchoredPosition = new Vector2( -sign * width * ( 1f - t ), 0f );

        if( 1f <= rawT )
        {
            CompletePortraitSlide();
        }
    }

    private void CompletePortraitSlide()
    {
        _isPortraitSliding = false;

        // 主系統(TargetImage/_characterCamera)を切替後のキャラクターへretargetし、定位置に戻す
        _characterCamera.AssignCharacter( _character, _layerMaskIndex );
        _parameterUI.TargetImage.rectTransform.anchoredPosition = Vector2.zero;

        _slideOutgoingCharacter.gameObject.SetLayerRecursively( Constants.LAYER_MASK_INDEX_CHARACTER );
        _slideOutgoingCharacter = null;

        _parameterUI.IncomingTargetImage.gameObject.SetActive( false );
    }

    public void ClearCharacter()
    {
        if( _isPortraitSliding )
        {
            _isPortraitSliding = false;
            _slideOutgoingCharacter?.gameObject.SetLayerRecursively( Constants.LAYER_MASK_INDEX_CHARACTER );
            _slideOutgoingCharacter = null;
            _parameterUI.TargetImage.rectTransform.anchoredPosition = Vector2.zero;
            _parameterUI.IncomingTargetImage?.gameObject.SetActive( false );
        }

        if( null != _character )
        {
            _character.gameObject.SetLayerRecursively( Constants.LAYER_MASK_INDEX_CHARACTER );
            _character = null;
        }

        SetActive( false );
    }

    public void SetSkillBoxToUsing( int skillIndex )
    {
        _parameterUI.SkillBoxes[skillIndex].SetUsing();
    }

    public SkillBoxUI[] GetSkillBoxes()
    {
        return _parameterUI.SkillBoxes;
    }

    /// <summary>
    /// 指定インデックスのSkillBoxUIのみをカーソルハイライト状態にします(-1で全解除)
    /// 外枠表示のみで示すため、拡大表示は行いません
    /// </summary>
    public void SetSkillBoxCursorIndex( int index )
    {
        var skillBoxes = _parameterUI.SkillBoxes;
        for( int i = 0; i < skillBoxes.Length; ++i )
        {
            skillBoxes[i].SetCursorHighlighted( i == index, scaleUp: false );
        }
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

        if( null != _parameterUI.TMPExpValue )
        {
            _parameterUI.TMPExpValue.text = LevelExpData.IsMaxLevel( status.Level )
                ? "MAX"
                : $"{LevelExpData.GetExpToNextLevel( status.Level, status.Exp )} Exp";
        }

        if( null != _parameterUI.TMPStatusPointValue )
        {
            _parameterUI.TMPStatusPointValue.text = $"{status.StatusPoint}";
        }

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
                    if( IsValidBlinkGaugeElement( i, status.CurActionGauge, selectCharacter.BattleParams.TmpParam.ActGaugeConsumption ) )
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
                    if( IsValidBlinkGaugeElement( i, status.CurActionGauge, selectCharacter.BattleParams.TmpParam.ActGaugeConsumption ) )
                    {
                        _blinkingElapsedTime += DeltaTimeProvider.DeltaTime;
                        _alpha      = Mathf.PingPong( _blinkingElapsedTime / _blinkingDuration, 1.0f );
                        elem.color  = new Color( 0, 1, 0, _alpha );
                    }
                    else
                    {
                        elem.color = Color.green;
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

    private bool IsValidBlinkGaugeElement( int elemIndex, int currentActionGauge, int actGaugeConsumption )
    {
        return ( currentActionGauge - actGaugeConsumption ) <= elemIndex;
    }
}
