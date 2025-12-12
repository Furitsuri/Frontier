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
    private Character _currentGridSelectCharacter = null;
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
        UpdateSlideAnimation();
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
        _deployUiSystem.CharacterSelectUi.SetActive( !isActive );           // 配置キャラクター選択UIの表示を切替
        _deployUiSystem.ConfirmCompleted.gameObject.SetActive( isActive );  // 配置完了確認UIの表示を切替
    }

    public void ResetDeploymentCharacterDispPosition()
    {
        _deployUiSystem.CharacterSelectUi.ResetDeploymentCharacterDispPositions();
    }

    public void RefreshGridCursorSelectCharacter()
    {
        // グリッドカーソルが現在選択中のキャラクターを取得
        _currentGridSelectCharacter = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();

        // タイル上のキャラクターのパラメータは、フォーカス中の配置候補キャラクター以外であれば表示
        bool isActiveOnSelectCharaParam = ( null != _currentGridSelectCharacter && _currentGridSelectCharacter != _focusDeployments[_focusDeployments.Length / 2].Character );
        _deployUiSystem.GridCursorSelectCharaParam.gameObject.SetActive( isActiveOnSelectCharaParam );
        if( !isActiveOnSelectCharaParam ) { return; }

        if( null != _currentGridSelectCharacter )
        {
            _deployUiSystem.GridCursorSelectCharaParam.AssignCharacter( _currentGridSelectCharacter, LAYER_MASK_INDEX_DEPLOYMENT_GRID );
        }

        // 配置候補UI内でフォーカス中のキャラクターも更新
        // MEMO : RefreshFocusDeploymentCharacter()を呼んでしまうと無限ループに陥るため注意
        _deployUiSystem.CharacterSelectUi.FocusCharaParamUI.AssignCharacter( _focusDeployments[_focusDeployments.Length / 2].Character, LAYER_MASK_INDEX_DEPLOYMENT_FOCUS );
    }

    public void RefreshFocusDeploymentCharacter()
    {
        // フォーカス中のキャラクターのパラメータの表示
        Debug.Assert( _focusDeployments.Length % 2 == 1 );  // 奇数であることが前提
        _deployUiSystem.CharacterSelectUi.FocusCharaParamUI.AssignCharacter( _focusDeployments[_focusDeployments.Length / 2].Character, LAYER_MASK_INDEX_DEPLOYMENT_FOCUS );

        RefreshGridCursorSelectCharacter();
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

    public ( int, int ) GetDeploymentCharacterDisplaySize()
    {
        var size = _deployUiSystem.CharacterSelectUi.GetDeploymentCharacterDisplaySize();

        return ( (int)size.Item1, ( int ) size.Item2 );
    }

    /// <summary>
    /// キャラクター選択UIのスライドアニメーションの更新
    /// </summary>
    private void UpdateSlideAnimation()
    {
        if( _isSlideAnimationPlaying )
        {
            _isSlideAnimationPlaying = !_deployUiSystem.CharacterSelectUi.UpdateSlideAnimation();
            if( !_isSlideAnimationPlaying )
            {
                _onCompletedeSlideAnimation?.Invoke( _slideDirection );
            }
        }
    }
}