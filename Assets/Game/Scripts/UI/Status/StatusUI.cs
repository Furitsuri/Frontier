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

        // 各スキル枠にスキルが装備されているか(未装備の空枠はカーソルの選択対象から外す)
        private bool[] _isSkillEquipped         = System.Array.Empty<bool>();
        private bool[] _isPassiveSkillEquipped  = System.Array.Empty<bool>();

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
            TMPAction.SetValueText( charaParam.EffectiveMaxActionGauge.ToString() );
            TMPAttack.SetValueText( charaParam.Atk.ToString() );
            TMPAttack.SetBonusText( FormatBonusValue( chara.BattleParams.ModifiedParam.Atk ) );
            TMPDeffence.SetValueText( charaParam.Def.ToString() );
            TMPDeffence.SetBonusText( FormatBonusValue( chara.BattleParams.ModifiedParam.Def ) );

            // _characterCameraImage.texture = chara.Snapshot;
            _characterCameraImage.texture = _targetTexture;
            _characterCamera.Init( "StatusCamera", LAYER_MASK_INDEX_CHARACTER_STATUS, _cameraAngleY, ref _characterCameraImage );
            _characterCamera.AssignCharacter( chara, LAYER_MASK_INDEX_CHARACTER_STATUS );

            // スキルボックスUIの表示(未装備の枠も、文字・コスト表示の無い空枠として表示する)
            _isSkillEquipped = new bool[SkillBoxes.Length];
            for( int i = 0; i < SkillBoxes.Length; ++i )
            {
                var skillID = chara.GetEquipSkillID( i );
                _isSkillEquipped[i] = Frontier.Combat.ActiveSkillsData.IsValidSkill( skillID );
                SkillBoxes[i].ApplySkillForEditing( skillID );
                // 未装備の空枠、及びパッシブスキルの装備コストでアクションゲージ最大値が使用コストを下回り使用出来ないスキルは、
                // グレー表示にする(戦闘中のゲージ不足と同じ表現)
                SkillBoxes[i].SetUseableOrNot( _isSkillEquipped[i] && charaParam.IsEquipSkillWithinMaxActionGauge( i ) );
            }

            // パッシブスキルの表示(未装備の枠も、文字・コスト表示の無い空枠として表示する)
            _isPassiveSkillEquipped = new bool[PassiveSkillBoxes.Length];
            for( int i = 0; i < PassiveSkillBoxes.Length; ++i )
            {
                var skillID = chara.GetPassiveSkillID( i );
                _isPassiveSkillEquipped[i] = Frontier.Combat.PassiveSkillsData.IsValidSkill( skillID );
                PassiveSkillBoxes[i].ApplyPassiveSkillForEditing( skillID );
                // 未装備の空枠はグレー表示にする
                PassiveSkillBoxes[i].SetUseableOrNot( _isPassiveSkillEquipped[i] );
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

            // スキルは画面の並び順(固有スキル→パッシブスキル→アクティブスキル)に合わせて選択対象とする
            if( UniqueSkillBox != null && UniqueSkillBox.gameObject.activeSelf )
            {
                _statusItemList.Add( UniqueSkillBox );
            }

            // パッシブスキルもカーソルで選択し、説明文(ツールチップ)を確認出来るようにする
            // (未装備の空枠は説明文が無いため選択対象から外す)
            for( int i = 0; i < PassiveSkillBoxes.Length; ++i )
            {
                if( i < _isPassiveSkillEquipped.Length && !_isPassiveSkillEquipped[i] ) { continue; }
                _statusItemList.Add( PassiveSkillBoxes[i] );
            }

            for( int i = 0; i < SkillBoxes.Length; ++i )
            {
                if( i < _isSkillEquipped.Length && !_isSkillEquipped[i] ) { continue; }
                _statusItemList.Add( SkillBoxes[i] );
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