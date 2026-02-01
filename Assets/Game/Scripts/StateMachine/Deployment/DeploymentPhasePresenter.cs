using Frontier.StateMachine;
using Frontier.Battle;
using Frontier.Entities;
using Frontier.UI;
using System;
using System.Collections.ObjectModel;
using UnityEngine;
using Zenject;
using static Constants;

public class DeploymentPhasePresenter : PhasePresenterBase
{
    [Inject] protected BattleRoutineController _btlRtnCtrl = null;

    private bool _isSlideAnimationPlaying           = false;
    private SlideDirection _slideDirection;
    private Character _currentGridSelectCharacter   = null;
    private DeploymentUISystem _deployUiSystem      = null;
    private CharacterCandidate[] _focusDeployments = new CharacterCandidate[DEPLOYMENT_SHOWABLE_CHARACTERS_NUM];
    private ReadOnlyCollection<CharacterCandidate> _refDeploymentCandidates;
    private Action<SlideDirection> _onCompletedeSlideAnimation;

    public void Init()
    {
        _deployUiSystem = _uiSystem.DeployUi;
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

        if( candidateCount <= centralIndex )
        {
            candidateCount = arrayLength;
        }

        // 中央のインデックスを基準に、左右に配置するキャラクターを決定していく
        // 配列の端を超えた場合はループさせる
        for( int i = 0; i < _focusDeployments.Length; ++i )
        {
            // 「中央」を中心に左右に割り当てる（不足分はループする）
            int offset = ( i - centralIndex + candidateCount ) % candidateCount;
            int targetIndex = ( focusCharaIndex + offset ) % candidateCount;

            if( targetIndex.IsBetween( 0, _refDeploymentCandidates.Count - 1 ) )
            {
                _focusDeployments[i] = _refDeploymentCandidates[targetIndex];
            }
            else { _focusDeployments[i] = null; }
        }

        _deployUiSystem.CharacterSelectUI.AssignSelectCandidates( ref _focusDeployments );
    }

    public void ClearFocusCharacter()
    {
        _deployUiSystem.CharacterSelectUI.ClearSelectCharacter();
    }

    /// <summary>
    /// 配置候補キャラクターリストの参照を受け取ります
    /// </summary>
    /// <param name="refCandidateCharas"></param>
    public void AssignDeploymentCandidates( ReadOnlyCollection<CharacterCandidate> refCandidateCharas )
    {
        _refDeploymentCandidates  = refCandidateCharas;
    }

    /// <summary>
    /// キャラクター選択UIの表示・非表示を切り替えます
    /// </summary>
    /// <param name="isActive"></param>
    public void SetActiveCharacterSelectUIs( bool isActive )
    {
        _deployUiSystem.CharacterSelectUI.SetActive( isActive );
    }

    public void SetActiveLeftInputArrow( bool isActiveLeft )
    {
        _deployUiSystem.CharacterSelectUI.SetActiveLeftInputArrow( isActiveLeft );
    }

    public void SetActiveRightInputArrow( bool isActiveRight )
    {
        _deployUiSystem.CharacterSelectUI.SetActiveRightInputArrow( isActiveRight );
    }

    /// <summary>
    /// 配置完了確認UIの表示・非表示を切り替えます
    /// </summary>
    /// <param name="isActive"></param>
    public void SetActiveConfirmUis( bool isActive )
    {
        _deployUiSystem.CharacterSelectUI.SetActive( !isActive );           // 配置キャラクター選択UIの表示を切替
        _deployUiSystem.ConfirmCompleted.gameObject.SetActive( isActive );  // 配置完了確認UIの表示を切替
    }

    public void ResetDeploymentCharacterDispPosition()
    {
        _deployUiSystem.CharacterSelectUI.ResetDeploymentCharacterDispPositions();
    }

    /// <summary>
    /// グリッドカーソルが選択中のキャラクター情報を更新します
    /// </summary>
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
        _deployUiSystem.CharacterSelectUI.FocusCharaParamUI.AssignCharacter( _focusDeployments[_focusDeployments.Length / 2].Character, LAYER_MASK_INDEX_DEPLOYMENT_FOCUS );
    }

    /// <summary>
    /// フォーカス中の配置可能キャラクター情報を更新します
    /// </summary>
    public void RefreshFocusDeploymentCharacter()
    {
        // フォーカス中のキャラクターのパラメータの表示
        Debug.Assert( _focusDeployments.Length % 2 == 1 );  // 奇数であることが前提
        _deployUiSystem.CharacterSelectUI.FocusCharaParamUI.AssignCharacter( _focusDeployments[_focusDeployments.Length / 2].Character, LAYER_MASK_INDEX_DEPLOYMENT_FOCUS );

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

    public void SlideAnimationCharacterSelectionDisplay( SlideDirection direction, Action<SlideDirection> onCompleted )
    {
        _isSlideAnimationPlaying    = true;
        _slideDirection             = direction;
        _onCompletedeSlideAnimation = onCompleted;

        _deployUiSystem.CharacterSelectUI.StartSlideAnimation( direction );
    }

    public bool IsSlideAnimationPlaying()
    {
        return _isSlideAnimationPlaying;
    }

    public ( int, int ) GetCharacterSelectionDisplaySize()
    {
        var size = _deployUiSystem.CharacterSelectUI.GetCharacterSelectionDisplaySize();

        return ( (int)size.Item1, ( int ) size.Item2 );
    }

    /// <summary>
    /// キャラクター選択UIのスライドアニメーションの更新
    /// </summary>
    private void UpdateSlideAnimation()
    {
        if( _isSlideAnimationPlaying )
        {
            _isSlideAnimationPlaying = !_deployUiSystem.CharacterSelectUI.UpdateSlideAnimation();
            if( !_isSlideAnimationPlaying )
            {
                _onCompletedeSlideAnimation?.Invoke( _slideDirection );
            }
        }
    }
}