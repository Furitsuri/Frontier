using Frontier.Entities;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using static Constants;

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
        [SerializeField] private SkillBoxUI[] PassiveSkillBoxes;     // パッシブスキルの表示枠(EQUIPABLE_PASSIVE_SKILL_MAX_NUM個)
        [SerializeField] private SkillBoxUI UniqueSkillBox;          // 固有スキルの表示枠
        [SerializeField] private GameObject UniqueSkillLabel;        // 固有スキルの見出し(固有スキルが無い種別の場合は枠と共に非表示)
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
            var charaParam = chara.GetStatusRef;

            TMPName.text = charaParam.Name;
            TMPLevel.SetValueText( charaParam.Level.ToString() );
            TMPHP.SetValueText( $"{charaParam.CurHP} / {charaParam.MaxHP}" );
            TMPMove.SetValueText( charaParam.moveRange.ToString() );
            TMPJump.SetValueText( charaParam.jumpForce.ToString() );
            TMPAction.SetValueText( charaParam.maxActionGauge.ToString() );
            TMPAttack.SetValueText( charaParam.Atk.ToString() );
            TMPAttack.SetBonusText( FormatBonusValue( chara.BattleParams.ModifiedParam.Atk ) );
            TMPDeffence.SetValueText( charaParam.Def.ToString() );
            TMPDeffence.SetBonusText( FormatBonusValue( chara.BattleParams.ModifiedParam.Def ) );

            // _characterCameraImage.texture = chara.Snapshot;
            _characterCameraImage.texture = _targetTexture;
            _characterCamera.Init( "StatusCamera", LAYER_MASK_INDEX_CHARACTER_STATUS, _cameraAngleY, ref _characterCameraImage );
            _characterCamera.AssignCharacter( chara, LAYER_MASK_INDEX_CHARACTER_STATUS );

            // スキルボックスUIの表示
            for( int i = 0; i < EQUIPABLE_ACTIVE_SKILL_MAX_NUM; ++i )
            {
                SkillBoxes[i].ApplySkill( chara, i );
            }

            // パッシブスキルの表示(未装備の枠は非表示)
            for( int i = 0; i < PassiveSkillBoxes.Length; ++i )
            {
                PassiveSkillBoxes[i].ApplyPassiveSkill( chara.GetPassiveSkillID( i ) );
            }

            // 固有スキルの表示(固有スキルが無い種別の場合は非表示)
            if( UniqueSkillBox != null )
            {
                UniqueSkillBox.ApplyUniqueSkill( chara.GetUniqueSkillID() );
                if( UniqueSkillLabel != null ) { UniqueSkillLabel.SetActive( UniqueSkillBox.gameObject.activeSelf ); }
            }

            InitStatusItemRectList();
        }

        /// <summary>
        /// 基礎値に加算されている補正値を「+ 1」/「- 1」の形式で返します。補正が無い場合は空文字列(非表示)を返します。
        /// </summary>
        private static string FormatBonusValue( int bonusValue )
        {
            if( bonusValue == 0 ) { return string.Empty; }

            return ( bonusValue < 0 ) ? $"- {-bonusValue}" : $"+ {bonusValue}";
        }

        /// <summary>
        /// 攻撃力・防御力の項目のツールチップに追記する、補正値の内訳を設定します。
        /// </summary>
        public void SetBonusBreakdownText( string atkText, string defText )
        {
            TMPAttack.SetTooltipDetailText( atkText );
            TMPDeffence.SetTooltipDetailText( defText );
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

            // パッシブスキルもカーソルで選択し、説明文(ツールチップ)を確認出来るようにする
            foreach( var item in PassiveSkillBoxes )
            {
                if( !item.gameObject.activeSelf ) { continue; }
                _statusItemList.Add( item );
            }

            // 固有スキルも同様に選択対象とする
            if( UniqueSkillBox != null && UniqueSkillBox.gameObject.activeSelf )
            {
                _statusItemList.Add( UniqueSkillBox );
            }

            // カーソルの初期位置をリストの先頭の要素で初期化
            SetSelectCursorRect( _statusItemList[0].GetRectTransform().anchoredPosition, _statusItemList[0].GetRectTransform().sizeDelta );
        }

        public override void Setup()
        {
            LazyInject.GetOrCreate( ref _targetTexture, () => new RenderTexture( ( int ) _characterCameraImage.rectTransform.rect.width * 2, ( int ) _characterCameraImage.rectTransform.rect.height * 2, 16, RenderTextureFormat.ARGB32 ) );
            LazyInject.GetOrCreate( ref _characterCamera, () => _hierarchyBld.InstantiateWithDiContainer<CharacterCamera>( false ) );

            foreach( var item in SkillBoxes )
            {
                item.Setup();
            }
            foreach( var item in PassiveSkillBoxes )
            {
                item.Setup();
            }
            if( UniqueSkillBox != null ) { UniqueSkillBox.Setup(); }

            _selectCursor.gameObject.SetActive( false );
            gameObject.SetActive( false );
        }
    }
}