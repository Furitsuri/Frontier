using Frontier.StateMachine;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.UI
{
    public class DeploymentCharacterSelectUI : UiMonoBehaviour
    {
        [Header( "配置候補キャラクター表示洋プレハブ" )]
        [SerializeField] private DeploymentCharacterDisplay _deploymentCharacterDisplayPrefab;

        [Header( "キャラクターパタメータ表示UI" )]
        [SerializeField] private CharacterParameterUI _focusCharaParamUI;

        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        private DeploymentCharacterDisplay[] _deploymentCharacterDisplays = new DeploymentCharacterDisplay[DEPLOYMENT_SHOWABLE_CHARACTERS_NUM];

        public CharacterParameterUI FocusCharaParamUI => _focusCharaParamUI;

        public void Init()
        {
            int centralIndex = DEPLOYMENT_SHOWABLE_CHARACTERS_NUM / 2;

            _focusCharaParamUI.Setup();
            _focusCharaParamUI.Init();

            for( int i = 0; i < DEPLOYMENT_SHOWABLE_CHARACTERS_NUM; ++i )
            {
                float imagePosX = DEPLOYMENT_CHARACTER_IMAGE_OFFSET_X * ( i - centralIndex );
                _deploymentCharacterDisplays[i].InitAnchoredPosition( imagePosX );
            }

            gameObject.SetActive( false );
            _focusCharaParamUI.gameObject.SetActive( false );
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

        public override void Setup()
        {
            base.Setup();

            for( int i = 0; i < DEPLOYMENT_SHOWABLE_CHARACTERS_NUM; ++i )
            {
                LazyInject.GetOrCreate( ref _deploymentCharacterDisplays[i], () => _hierarchyBld.CreateComponentNestedParentWithDiContainer<DeploymentCharacterDisplay>( _deploymentCharacterDisplayPrefab.gameObject, gameObject, true, false, "DeploymentCharaDisp_" + i ) );

                _deploymentCharacterDisplays[i].transform.SetSiblingIndex( i ); // 表示順を登録順に合わせる
            }
        }
    }
}