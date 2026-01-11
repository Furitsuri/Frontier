using Frontier.Entities;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Frontier.UI
{
    public class StatusUI : UiMonoBehaviour
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        [SerializeField] private TextMeshProUGUI TMPName;
        [SerializeField] private StatusItem TMPLevel;
        [SerializeField] private StatusItem TMPHP;
        [SerializeField] private StatusItem TMPMove;
        [SerializeField] private StatusItem TMPJump;
        [SerializeField] private StatusItem TMPAction;
        [SerializeField] private StatusItem TMPAttack;
        [SerializeField] private StatusItem TMPDeffence;
        [SerializeField] private RawImage _characterCameraImage = null;
        [SerializeField] private Image _selectCursor            = null;
        [SerializeField] private SkillBoxUI[] SkillBoxes;
        [SerializeField] private int _cameraAngleY              = 0;
        [SerializeField] private float _cameraLengthY = 1.0f;
        [SerializeField] private float _cameraLengthZ = 1.5f;
        [SerializeField] private float _cameraLookAtCorrectY = 0.0f;

        private CharacterCamera _characterCamera;
        private RenderTexture _targetTexture;
        private List<ITooltipContent> _statusItemList = new List<ITooltipContent>();

        private void Update()
        {
            if( _characterCamera != null )
            {
                _characterCamera.Update( new CameraParameter() { UICameraLengthY = _cameraLengthY, UICameraLengthZ = _cameraLengthZ, UICameraLookAtCorrectY = _cameraLookAtCorrectY } );
            }
        }

        public void AssignCharacter( Character chara )
        {
            var charaParam = chara.Params.CharacterParam;

            TMPName.text = charaParam.Name;
            TMPLevel.SetValueText( charaParam.Level.ToString() );
            TMPHP.SetValueText( $"{charaParam.CurHP} / {charaParam.MaxHP}" );
            TMPMove.SetValueText( charaParam.moveRange.ToString() );
            TMPJump.SetValueText( charaParam.jumpForce.ToString() );
            TMPAction.SetValueText( charaParam.maxActionGauge.ToString() );
            TMPAttack.SetValueText( charaParam.Atk.ToString() );
            TMPDeffence.SetValueText( charaParam.Def.ToString() );

            // _characterCameraImage.texture = chara.Snapshot;
            _characterCameraImage.texture = _targetTexture;
            _characterCamera.Init( "StatusCamera", chara.gameObject.layer, _cameraAngleY, ref _characterCameraImage );
            _characterCamera.AssignCharacter( chara, chara.gameObject.layer );

            // スキルボックスUIの表示
            for( int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                SkillBoxes[i].ApplySkill( chara, i );
            }

            InitStatusItemRectList();
        }

        public void SetSelectCursorRect( Vector2 pos, Vector2 size )
        {
            _selectCursor.rectTransform.anchoredPosition = pos;
            _selectCursor.rectTransform.sizeDelta = size;
        }

        public void SetSelectCursorActive( bool isActive )
        {
            _selectCursor.gameObject.SetActive( isActive );
        }

        public ( float, float ) GetSnapshotRectSize()
        {
            if( null == _characterCameraImage )
            {
                return ( 0, 0 );
            }
            return ( _characterCameraImage.rectTransform.rect.width, _characterCameraImage.rectTransform.rect.height);
        }

        public List<ITooltipContent> GetStatusItemList()
        {
            return _statusItemList;
        }

        private void InitStatusItemRectList()
        {
            _statusItemList.Clear();
            _statusItemList.Add( TMPLevel );
            _statusItemList.Add( TMPHP );
            _statusItemList.Add( TMPMove );
            _statusItemList.Add( TMPJump );
            _statusItemList.Add( TMPAction );
            _statusItemList.Add( TMPAttack );
            _statusItemList.Add( TMPDeffence );

            foreach( var item in SkillBoxes )
            {
                if( !item.gameObject.activeSelf ) { continue; }
                _statusItemList.Add( item );
            }

            // カーソルの初期位置をリストの先頭の要素で初期化
            SetSelectCursorRect( _statusItemList[0].GetRectTransform().anchoredPosition, _statusItemList[0].GetRectTransform().sizeDelta );
        }

        override public void Setup()
        {
            LazyInject.GetOrCreate( ref _targetTexture, () => new RenderTexture( ( int ) _characterCameraImage.rectTransform.rect.width * 2, ( int ) _characterCameraImage.rectTransform.rect.height * 2, 16, RenderTextureFormat.ARGB32 ) );
            LazyInject.GetOrCreate( ref _characterCamera, () => _hierarchyBld.InstantiateWithDiContainer<CharacterCamera>( false ) );

            foreach( var item in SkillBoxes )
            {
                item.Setup();
            }

            _selectCursor.gameObject.SetActive( false );
            gameObject.SetActive( false );
        }
    }
}