using Froniter.StateMachine;
using Frontier;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using static Constants;
using static Frontier.CharacterParameterUI;

public class DeploymentCharacterSelectUI : MonoBehaviour
{
    [Inject] private HierarchyBuilderBase _hierarchyBld = null;

    [Header( "配置候補キャラクター表示洋プレハブ" )]
    [SerializeField] private DeploymentCharacterDisplay _deploymentCharacterDisplayPrefab;

    [Header( "キャラクターパタメータ表示UI" )]
    [SerializeField] private CharacterParameterUI _focusCharaParamUI;

    private DeploymentCharacterDisplay[] _deploymentCharacterDisplays = new DeploymentCharacterDisplay[DEPLOYMENT_SHOWABLE_CHARACTERS_NUM];

    public CharacterParameterUI FocusCharaParamUI => _focusCharaParamUI;

    void Awake()
    {
        for( int i = 0; i < DEPLOYMENT_SHOWABLE_CHARACTERS_NUM; ++i )
        {
            // ここで設定しているオブジェクト名が、DeploymentCharacterDisplay内のCameraのレイヤー設定にも用いられることに注意。
            // 詳しくはInspectorのLayers内の各User Layerを確認してください。
            _deploymentCharacterDisplays[i] = _hierarchyBld.CreateComponentNestedParentWithDiContainer<DeploymentCharacterDisplay>( _deploymentCharacterDisplayPrefab.gameObject, gameObject, true, false, "DeploymentCharaDisp_" + i );
            NullCheck.AssertNotNull( _deploymentCharacterDisplays[i], "_deploymentCharacterDisplay" + i );
            _deploymentCharacterDisplays[i].transform.SetSiblingIndex( i ); // 表示順を登録順に合わせる
        }
    }

    void Start()
    {
        int centralIndex = DEPLOYMENT_SHOWABLE_CHARACTERS_NUM / 2;

        for( int i = 0; i < DEPLOYMENT_SHOWABLE_CHARACTERS_NUM; ++i )
        {
            float imagePosX = DEPLOYMENT_CHARACTER_IMAGE_OFFSET_X * ( i - centralIndex );
            _deploymentCharacterDisplays[i].InitAnchoredPosition( imagePosX );
        }

        gameObject.SetActive( false );
        _focusCharaParamUI.gameObject.SetActive( false );
    }

    void Update()
    {
    }

    public bool UpdateSlideAnimation()
    {
        bool isCompleted = true;

        for( int i = 0; i < DEPLOYMENT_SHOWABLE_CHARACTERS_NUM; ++i )
        {
            if( !_deploymentCharacterDisplays[i].UpdateSlide() )
            {
                isCompleted = false;
            }
        }

        return isCompleted;
    }

    public void SetActive( bool isActive )
    {
        gameObject.SetActive( isActive );
        _focusCharaParamUI.gameObject.SetActive( isActive );
    }

    public void StartSlideAnimation( DeploymentPhasePresenter.SlideDirection direction )
    {
        Vector2 SlideGoalPos = ( direction == DeploymentPhasePresenter.SlideDirection.LEFT ) ? 
            new Vector2( DEPLOYMENT_CHARACTER_IMAGE_OFFSET_X, 0f ) :
            new Vector2( -DEPLOYMENT_CHARACTER_IMAGE_OFFSET_X, 0f );

        for( int i = 0; i < DEPLOYMENT_SHOWABLE_CHARACTERS_NUM; ++i )
        {
            _deploymentCharacterDisplays[i].StartSlide( SlideGoalPos );
        }
    }

    public void ResetDeploymentCharacterDispPositions()
    {
        for( int i = 0; i < DEPLOYMENT_SHOWABLE_CHARACTERS_NUM; ++i )
        {
            _deploymentCharacterDisplays[i].ResetAnchoredPosition();
        }
    }

    public void AssignSelectCandidates( ref DeploymentCandidate[] selectCandidates )
    {
        for( int i = 0; i < DEPLOYMENT_SHOWABLE_CHARACTERS_NUM; ++i )
        {
            _deploymentCharacterDisplays[i].AssignSelectCandidate( ref selectCandidates[i] );
        }
    }

    public void ClearSelectCharacter()
    {
        for( int i = 0; i < DEPLOYMENT_SHOWABLE_CHARACTERS_NUM; ++i )
        {
            _deploymentCharacterDisplays[i].ClearSelectCharacter();
        }
    }

    public (float, float) GetDeploymentCharacterDisplaySize()
    {
        var rect = _deploymentCharacterDisplayPrefab.GetComponent<RectTransform>();
        NullCheck.AssertNotNull( rect, "_deploymentCharacterDisplayPrefab->rectTransform" );

        return (rect.rect.width, rect.rect.height);
    }
}