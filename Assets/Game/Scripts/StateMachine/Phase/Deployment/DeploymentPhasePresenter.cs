using Froniter.StateMachine;
using Frontier.Entities;
using Frontier.UI;
using UnityEngine.UI;
using System;
using System.Collections.ObjectModel;
using Zenject;
using static Constants;
using UnityEngine;


public class DeploymentPhasePresenter
{
    public enum SlideDirection
    {
        LEFT = 0,
        RIGHT,
    }

    [Inject] private IUiSystem uiSystem = null;

    private bool _isSlideAnimationPlaying = false;
    private SlideDirection _slideDirection;
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
        if( _isSlideAnimationPlaying )
        {
            _isSlideAnimationPlaying = !_deployUiSystem.CharacterSelectUi.UpdateSlideAnimation();
            if( !_isSlideAnimationPlaying ) { 
                _onCompletedeSlideAnimation?.Invoke( _slideDirection );
            }
        }

        Debug.Assert( _focusDeployments.Length % 2 == 1 );  // 奇数であることが前提
        var showParamCharacter = _focusDeployments[_focusDeployments.Length / 2];
        _deployUiSystem.CharacterSelectUi.FocusCharaParamUI.SetDisplayCharacter( showParamCharacter.Character );
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