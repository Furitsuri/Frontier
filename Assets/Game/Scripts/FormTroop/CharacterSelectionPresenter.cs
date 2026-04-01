using Frontier.Entities;
using Frontier.StateMachine;
using Frontier.UI;
using System;
using System.Collections.ObjectModel;
using Zenject;

public class CharacterSelectionPresenter : PhasePresenterBase
{
    [Inject] private HierarchyBuilderBase _hierarchyBld = null;

    protected CharacterSelectionUI _characterSelectUI;
    protected CharacterParameterPresenter _parameterPresenter = null;
    protected CharacterCandidate[] _focusCandidates;
    protected ReadOnlyCollection<CharacterCandidate> _refCandidates = null;
    private SlideDirection _slideDirection;
    private bool _isSlideAnimationPlaying = false;
    private Action<SlideDirection> _onCompletedeSlideAnimation;
    private ReadOnlyReference<bool> _refIsSlideLoop = null;

    public bool RefIsSlideLoop => _refIsSlideLoop.Value;

    [Inject] public CharacterSelectionPresenter( CharacterSelectionUI characterSelectionUI, int focusCandidatesArrayLength, bool isNeededParamWinCamera, HierarchyBuilderBase hierarchyBld )
    {
        _characterSelectUI  = characterSelectionUI;
        _hierarchyBld       = hierarchyBld;
        _focusCandidates    = new CharacterCandidate[focusCandidatesArrayLength];
        _refIsSlideLoop     = new ReadOnlyReference<bool>( _characterSelectUI.IsSlideLoop );

        LazyInject.GetOrCreate( ref _parameterPresenter,
            () => _hierarchyBld.InstantiateWithDiContainer<CharacterParameterPresenter>
            ( new object[] { _characterSelectUI.FocusCharaParamUI, isNeededParamWinCamera }, false )
            );
    }

    public virtual void Init()
    {
        _parameterPresenter.Init();
    }

    public virtual void Exit()
    {
        _refCandidates  = null;
        _refIsSlideLoop = null;
    }

    /// <summary>
    /// 配置候補キャラクターリストから、指定されたインデックスを中心にキャラクターを抽出して表示させます
    /// </summary>
    /// <param name="focusCharaIndex"></param>
    public virtual void SetFocusCharacters( int focusCharaIndex )
    {
        int arrayLength = _focusCandidates.Length;  // 奇数前提であることに注意
        int centralIndex = arrayLength / 2;
        int candidateCount = _refCandidates.Count;

        if( candidateCount <= centralIndex )
        {
            candidateCount = arrayLength;
        }

        // 中央のインデックスを基準に、左右に配置するキャラクターを決定していく
        // ループフラグが設定されている場合は、配列の端を超えた場合はループさせる
        for( int i = 0; i < _focusCandidates.Length; ++i )
        {
            int offset      = i - centralIndex;
            int targetIndex = focusCharaIndex + offset;

            // ループフラグが設定されている場合は「中央」を中心に左右に割り当てる(不足分はループする)
            if( _characterSelectUI.IsSlideLoop )
            {
                offset      = ( i - centralIndex + candidateCount ) % candidateCount;
                targetIndex = ( focusCharaIndex + offset ) % candidateCount;
            }

            if( targetIndex.IsBetween( 0, _refCandidates.Count - 1 ) )
            {
                _focusCandidates[i] = _refCandidates[targetIndex];
            }
            else { _focusCandidates[i] = null; }
        }

        _characterSelectUI.AssignSelectCandidates( ref _focusCandidates );
    }

    public void ResetCharacterDispPosition()
    {
        _characterSelectUI.ResetCharacterDispPositions();
    }

    public void ClearFocusCharacter()
    {
        _characterSelectUI.ClearSelectCharacter();
    }

    public void AssignCandidates( ReadOnlyCollection<CharacterCandidate> candidates )
    {
        _refCandidates = candidates;
    }

    /// <summary>
    /// キャラクター選択UIの表示・非表示を切り替えます
    /// </summary>
    /// <param name="isActive"></param>
    public void SetActiveCharacterSelectUIs( bool isActive )
    {
        _characterSelectUI.SetActive( isActive );
    }

    public void SetActiveLeftInputArrow( bool isActiveLeft )
    {
        _characterSelectUI.SetActiveLeftInputArrow( isActiveLeft );
    }

    public void SetActiveRightInputArrow( bool isActiveRight )
    {
        _characterSelectUI.SetActiveRightInputArrow( isActiveRight );
    }

    public void SlideAnimationCharacterSelectionDisplay( SlideDirection direction, Action<SlideDirection> onCompleted )
    {
        _isSlideAnimationPlaying    = true;
        _slideDirection             = direction;
        _onCompletedeSlideAnimation = onCompleted;

        _characterSelectUI.StartSlideAnimation( direction );
    }

    /// <summary>
    /// キャラクター選択UIのスライドアニメーションの更新
    /// </summary>
    protected void UpdateSlideAnimation()
    {
        if( _isSlideAnimationPlaying )
        {
            _isSlideAnimationPlaying = !_characterSelectUI.UpdateSlideAnimation();
            if( !_isSlideAnimationPlaying )
            {
                _onCompletedeSlideAnimation?.Invoke( _slideDirection );
            }
        }
    }
}