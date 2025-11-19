using Froniter.StateMachine;
using Frontier.Battle;
using Frontier.Entities;
using Frontier.UI;
using System;
using System.Collections.ObjectModel;
using UnityEngine;
using Zenject;
using static Constants;


public class DeploymentPhasePresenter
{
    public enum SlideDirection
    {
        LEFT = 0,
        RIGHT,
    }

    [Inject] private IUiSystem uiSystem = null;
    [Inject] protected BattleRoutineController _btlRtnCtrl = null;

    private bool _isSlideAnimationPlaying = false;
    private SlideDirection _slideDirection;
    private Character _prevSelectCharacter = null;
    private DeploymentUISystem _deployUiSystem = null;
    private DeploymentCandidate[] _focusDeployments = new DeploymentCandidate[DEPLOYMENT_SHOWABLE_CHARACTERS_NUM];
    private ReadOnlyCollection<DeploymentCandidate> _refDeploymentCandidates;
    private Action<SlideDirection> _onCompletedeSlideAnimation;

    public void Init()
    {
        _deployUiSystem = uiSystem.DeployUi;
        _deployUiSystem.Init();
    }

    public void Update()
    {
        // フォーカス中のキャラクターのパラメータの表示
        Debug.Assert( _focusDeployments.Length % 2 == 1 );  // 奇数であることが前提
        int index = _focusDeployments.Length / 2;
        var showParamCharaOnDeployment = _focusDeployments[index].Character;
        _deployUiSystem.CharacterSelectUi.FocusCharaParamUI.SetDisplayCharacter( showParamCharaOnDeployment, LAYER_MASK_INDEX_DEPLOYMENT );

        // グリッドカーソルが現在選択中のキャラクターを取得
        var gridCursorSelectChara = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();

        // タイル上のキャラクターのパラメータは、フォーカス中の配置候補キャラクター以外であれば表示
        bool isActiveOnSelectCharaParam = ( null != gridCursorSelectChara && gridCursorSelectChara != showParamCharaOnDeployment );
        _deployUiSystem.GridCursorSelectCharaParam.gameObject.SetActive( isActiveOnSelectCharaParam );
        if( null != gridCursorSelectChara /* && _prevSelectCharacter != gridCursorSelectChara */ )
        {
            _deployUiSystem.GridCursorSelectCharaParam.SetDisplayCharacter( gridCursorSelectChara, LAYER_MASK_INDEX_DEPLOYMENT );
        }

        // キャラクター選択UIのスライドアニメーションの更新
        if( _isSlideAnimationPlaying )
        {
            _isSlideAnimationPlaying = !_deployUiSystem.CharacterSelectUi.UpdateSlideAnimation();
            if( !_isSlideAnimationPlaying ) { 
                _onCompletedeSlideAnimation?.Invoke( _slideDirection );
            }
        }

        _prevSelectCharacter = gridCursorSelectChara;
    }

    public void Exit()
    {
        _refDeploymentCandidates = null;    // 参照をクリアしておく

        _deployUiSystem.Exit();
    }

    /// <summary>
    /// 配置候補キャラクターリストから、指定されたインデックスを中心にキャラクターを抽出して表示させます
    /// </summary>
    /// <param name="focusCharaIndex"></param>
    public void SetFocusCharacters( int focusCharaIndex )
    {
        int arrayLength     = _focusDeployments.Length;  // 奇数前提であることに注意
        int centralIndex    = arrayLength / 2;
        int candidateCount  = _refDeploymentCandidates.Count;

        // 中央のインデックスを基準に、左右に配置するキャラクターを決定していく
        // 配列の端を超えた場合はループさせる
        for( int i = 0; i < _focusDeployments.Length; ++i )
        {
            // 「中央」を中心に左右に割り当てる（不足分はループする）
            int offset = ( i - centralIndex + candidateCount ) % candidateCount;
            int targetIndex = ( focusCharaIndex + offset ) % candidateCount;
            
            _focusDeployments[i] = _refDeploymentCandidates[targetIndex];
        }

        _deployUiSystem.CharacterSelectUi.AssignSelectCandidates( ref _focusDeployments );
    }

    public void ClearFocusCharacter()
    {
        _deployUiSystem.CharacterSelectUi.ClearSelectCharacter();
    }

    /// <summary>
    /// 配置候補キャラクターリストの参照を受け取ります
    /// </summary>
    /// <param name="refCandidateCharas"></param>
    public void AssignDeploymentCandidates( ReadOnlyCollection<DeploymentCandidate> refCandidateCharas )
    {
        _refDeploymentCandidates  = refCandidateCharas;
    }

    /// <summary>
    /// キャラクター選択UIの表示・非表示を切り替えます
    /// </summary>
    /// <param name="isActive"></param>
    public void SetActiveCharacterSelectUis( bool isActive )
    {
        _deployUiSystem.CharacterSelectUi.SetActive( isActive );
    }

    /// <summary>
    /// 配置完了確認UIの表示・非表示を切り替えます
    /// </summary>
    /// <param name="isActive"></param>
    public void SetActiveConfirmUis( bool isActive )
    {
        _deployUiSystem.DeployMessage.SetActive( !isActive );   // 配置メッセージはConfirmUIが表示されている間は非表示にする
        _deployUiSystem.CharacterSelectUi.SetActive( !isActive );

        _deployUiSystem.ConfirmCompleted.gameObject.SetActive( isActive );
    }

    public void ResetDeploymentCharacterDispPosition()
    {
        _deployUiSystem.CharacterSelectUi.ResetDeploymentCharacterDispPositions();
    }

    /// <summary>
    /// 配置完了確認UIのテキストカラーの選択・非選択状態を適用します
    /// </summary>
    /// <param name="selectIndex"></param>
    public void ApplyTextColor2ConfirmCompleted( int selectIndex )
    {
        _deployUiSystem.ConfirmCompleted.ApplyTextColor( selectIndex );
    }

    public void SlideAnimationDeploymentCharacterDisplay( SlideDirection direction, Action<DeploymentPhasePresenter.SlideDirection> onCompleted )
    {
        _isSlideAnimationPlaying    = true;
        _slideDirection             = direction;
        _onCompletedeSlideAnimation = onCompleted;

        _deployUiSystem.CharacterSelectUi.FocusCharaParamUI.ClearDisplayCharacter();    // 現在表示中のキャラクターの表示情報をクリア
        _deployUiSystem.CharacterSelectUi.StartSlideAnimation( direction );
    }

    public bool IsSlideAnimationPlaying()
    {
        return _isSlideAnimationPlaying;
    }

    public ( float, float ) GetDeploymentCharacterDisplaySize()
    {
        return _deployUiSystem.CharacterSelectUi.GetDeploymentCharacterDisplaySize();
    }
}