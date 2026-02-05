using Frontier.Entities;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.UI
{
    public class CharacterSelectionUI : UiMonoBehaviour
    {
        [Header( "配置候補キャラクター表示用プレハブ" )]
        [SerializeField] private CharacterSelectionDisplay _characterSelectionDisplayPrefab;

        [Header( "キャラクターパタメータ表示UI" )]
        [SerializeField] private CharacterParameterUI _focusCharaParamUI;

        [Header( "フォーカスイメージ" )]
        [SerializeField] private GameObject _focusImageObject;

        [Header( "左方向入力矢印" )]
        [SerializeField] private GameObject _leftInputArrow;

        [Header( "右方向入力矢印" )]
        [SerializeField] private GameObject _rightInputArrow;

        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        protected CharacterSelectionDisplay[] _characterSelectionDisplays = new CharacterSelectionDisplay[SHOWABLE_SELECTION_CHARACTERS_NUM];
        private float _offsetX = 0f;

        public CharacterParameterUI FocusCharaParamUI => _focusCharaParamUI;

        public void Init( CharacterSelectionDisplayMode mode )
        {
            _offsetX = _focusImageObject.GetComponent<RectTransform>().rect.width;
            int centralIndex = SHOWABLE_SELECTION_CHARACTERS_NUM / 2;

            _focusCharaParamUI.Setup();
            _focusCharaParamUI.Init();

            for( int i = 0; i < SHOWABLE_SELECTION_CHARACTERS_NUM; ++i )
            {
                float imagePosX = _offsetX * ( i - centralIndex );
                _characterSelectionDisplays[i].InitAnchoredPosition( imagePosX );
                _characterSelectionDisplays[i].SetMode( mode );
            }

            gameObject.SetActive( false );
            _focusCharaParamUI.gameObject.SetActive( false );
        }

        public bool UpdateSlideAnimation()
        {
            bool isCompleted = true;

            for( int i = 0; i < SHOWABLE_SELECTION_CHARACTERS_NUM; ++i )
            {
                if( !_characterSelectionDisplays[i].UpdateSlide() )
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

        public void SetActiveLeftInputArrow( bool isLeftActive )
        {
            _leftInputArrow.SetActive( isLeftActive );
        }

        public void SetActiveRightInputArrow( bool isRightActive )
        {
            _rightInputArrow.SetActive( isRightActive );
        }

        public void StartSlideAnimation( SlideDirection direction )
        {
            Vector2 SlideGoalPos = ( direction == SlideDirection.LEFT ) ?
                new Vector2( _offsetX, 0f ) :
                new Vector2( -_offsetX, 0f );

            for( int i = 0; i < SHOWABLE_SELECTION_CHARACTERS_NUM; ++i )
            {
                _characterSelectionDisplays[i].StartSlide( SlideGoalPos );

                // スライド開始前にフォーカスしている項目とそうでない項目のカラー変更を適応させる
                if( direction == SlideDirection.LEFT )
                {
                    _characterSelectionDisplays[i].SetFocusedColor( i == ( SHOWABLE_SELECTION_CHARACTERS_NUM / 2 - 1 ) );
                }
                else if( direction == SlideDirection.RIGHT )
                {
                    _characterSelectionDisplays[i].SetFocusedColor( i == ( SHOWABLE_SELECTION_CHARACTERS_NUM / 2 + 1 ) );
                }
            }
        }

        public void ResetDeploymentCharacterDispPositions()
        {
            for( int i = 0; i < SHOWABLE_SELECTION_CHARACTERS_NUM; ++i )
            {
                _characterSelectionDisplays[i].ResetAnchoredPosition();
            }
        }


        public void ClearSelectCharacter()
        {
            for( int i = 0; i < SHOWABLE_SELECTION_CHARACTERS_NUM; ++i )
            {
                _characterSelectionDisplays[i].ClearSelectCharacter();
            }
        }

        public (float, float) GetCharacterSelectionDisplaySize()
        {
            var rect = _characterSelectionDisplayPrefab.GetComponent<RectTransform>();
            NullCheck.AssertNotNull( rect, "_characterSelectionDisplayPrefab->rectTransform" );

            return (rect.rect.width, rect.rect.height);
        }

        public virtual void AssignSelectCandidates( ref CharacterCandidate[] selectCandidates )
        {
            for( int i = 0; i < SHOWABLE_SELECTION_CHARACTERS_NUM; ++i )
            {
                if( selectCandidates[i] == null )
                {
                    _characterSelectionDisplays[i].gameObject.SetActive( false );
                    continue;
                }

                _characterSelectionDisplays[i].gameObject.SetActive( true );
                _characterSelectionDisplays[i].AssignSelectCandidate( ref selectCandidates[i] );

                // 中央のキャラクターのみフォーカス色にする
                _characterSelectionDisplays[i].SetFocusedColor( i == ( SHOWABLE_SELECTION_CHARACTERS_NUM / 2 ) );
            }
        }

        public override void Setup()
        {
            base.Setup();

            for( int i = 0; i < SHOWABLE_SELECTION_CHARACTERS_NUM; ++i )
            {
                LazyInject.GetOrCreate( ref _characterSelectionDisplays[i],
                    () => _hierarchyBld.CreateComponentNestedParentWithDiContainer<CharacterSelectionDisplay>( _characterSelectionDisplayPrefab.gameObject, gameObject, true, false, "CharacterDisp_" + i ) );
                _characterSelectionDisplays[i].Setup();
                _characterSelectionDisplays[i].transform.SetSiblingIndex( i ); // 表示順を登録順に合わせる
            }

            _leftInputArrow.SetActive( true );
            _rightInputArrow.SetActive( true );
        }
    }
}