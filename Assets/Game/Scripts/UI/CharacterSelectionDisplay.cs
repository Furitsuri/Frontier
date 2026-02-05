using Frontier.Entities;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;
using Zenject;

namespace Frontier.UI
{
    public class CharacterSelectionDisplay : UiMonoBehaviour
    {
        [Header( "スライド時間" )]
        [SerializeField] private float _slideTime = 0.1f;

        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        private float _slideCurrentTime             = 0f;
        private CharacterSelectionDisplayMode _mode = CharacterSelectionDisplayMode.Texture;
        private RectTransform _rectTransform        = null;
        private CharacterCamera _characterCamera    = null;
        private CharacterCandidate _candidate       = null;
        private RawImage _backGround                = null;
        private Vector2 _slideStartPosition;
        private Vector2 _slideGoalPosition;

        void Update()
        {
            if( null == _candidate ) { return; }

            UpdateIsDisplayed();

            _characterCamera?.Update( _candidate.Character.CameraParam );
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
            _slideGoalPosition = goalPos;
            _slideCurrentTime = 0f;
        }

        public void ClearSelectCharacter()
        {
            _candidate = null;
        }

        public void SetTextureMode()
        {
            _mode = CharacterSelectionDisplayMode.Texture;
            if( null != _characterCamera )
            {
                _characterCamera.Dispose();
                _characterCamera = null;
            }
        }

        public void SetMode( CharacterSelectionDisplayMode mode )
        {
            _mode = mode;

            if( _mode == CharacterSelectionDisplayMode.Texture )
            {
                if( null != _characterCamera )
                {
                    _characterCamera.Dispose();
                    _characterCamera = null;
                }
            }
            else if( _mode == CharacterSelectionDisplayMode.Camera )
            {
                LazyInject.GetOrCreate( ref _characterCamera, () => _hierarchyBld.InstantiateWithDiContainer<CharacterCamera>( false ) );
            }
        }

        public bool UpdateSlide()
        {
            _slideCurrentTime = Mathf.Clamp( _slideCurrentTime + DeltaTimeProvider.DeltaTime, 0f, _slideTime );
            var lerpRate = _slideCurrentTime / _slideTime;
            _rectTransform.anchoredPosition = Vector2.Lerp( _rectTransform.anchoredPosition, _slideStartPosition + _slideGoalPosition, lerpRate );

            return ( 1f <= lerpRate );
        }

        public virtual void AssignSelectCandidate( ref CharacterCandidate candidate )
        {
            _candidate = candidate;

            if( _mode == CharacterSelectionDisplayMode.Texture ) { _backGround.texture = _candidate.SnapshotImg; }
            _characterCamera?.Init( "CharacterSelectionCamera", _candidate.Character.gameObject.layer, 0f, ref _backGround );
            _characterCamera?.AssignCharacter( _candidate.Character, _candidate.Character.gameObject.layer );
        }

        public virtual void SetFocusedColor( bool isFocused )
        {
            if( !isFocused )
            {
                _candidate.Character.SetMaterialsGrayColor();
            }
            else
            {
                _candidate.Character.RestoreMaterialsOriginalColor();
            }
        }

        public override void Setup()
        {
            base.Setup();

            LazyInject.GetOrCreate( ref _rectTransform, () => GetComponent<RectTransform>() );
            LazyInject.GetOrCreate( ref _backGround, () => GetComponent<RawImage>() );

            _candidate = null;
        }

        private void UpdateIsDisplayed()
        {
            _backGround.color = _candidate.IsSelected
            ? new Color( 0.5f, 0.5f, 0.5f, 1f ) // 暗くする
            : Color.white;                      // 元に戻す
        }
    }
}