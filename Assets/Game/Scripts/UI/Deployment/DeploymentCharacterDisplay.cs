using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Frontier.Entities;
using Froniter.StateMachine;
using static Constants;

public class DeploymentCharacterDisplay : MonoBehaviour
{
    [SerializeField]
    [Header( "スライド時間" )]
    private float _slideTime = 0.1f;

    private float _slideCurrentTime         = 0f;
    private RectTransform _rectTransform    = null;
    private DeploymentCandidate _candidate  = null;
    private RawImage _backGround            = null;
    private Vector2 _slideStartPosition;
    private Vector2 _slideGoalPosition;

    void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        NullCheck.AssertNotNull( _rectTransform, "_rectTransform" );

        _backGround = GetComponent<RawImage>();
        NullCheck.AssertNotNull( _backGround, "_backGround" );

        _candidate = null;
    }

    void Update()
    {
        if( null == _candidate ) { return; }

        UpdateIsDisplayed();
    }

    public void Init()
    {

    }

    public void InitAnchoredPosition( float posX )
    {
        _rectTransform.anchoredPosition = new Vector2( posX, 0f );
    }

    public void ResetAnchoredPosition()
    {
        _rectTransform.anchoredPosition = _slideStartPosition;
    }

    public void StartSlide( in Vector2 goalPos )
    {
        _slideStartPosition = _rectTransform.anchoredPosition;
        _slideGoalPosition  = goalPos;
        _slideCurrentTime   = 0f;
    }

    public void AssignSelectCandidate( ref DeploymentCandidate candidate )
    {
        _candidate = candidate;

        _backGround.texture = _candidate.CandidateImg;
    }

    public void ClearSelectCharacter()
    {
        _candidate = null;
    }

    public bool UpdateSlide()
    {
        _slideCurrentTime = Mathf.Clamp( _slideCurrentTime + DeltaTimeProvider.DeltaTime, 0f, _slideTime );
        var lerpRate = _slideCurrentTime / _slideTime;
        _rectTransform.anchoredPosition = Vector2.Lerp( _rectTransform.anchoredPosition, _slideStartPosition + _slideGoalPosition, lerpRate );

        return ( 1f <= lerpRate );
    }

    private void UpdateIsDisplayed()
    {
        _backGround.color = _candidate.IsDeployed
        ? new Color( 0.5f, 0.5f, 0.5f, 1f ) // 暗くする
        : Color.white;                      // 元にも戻す
    }
}