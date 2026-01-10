using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Frontier.Entities;
using System.Collections.Generic;

namespace Frontier.UI
{
    public class StatusUI : UiMonoBehaviour
    {
        [SerializeField] private StatusItem TMPLevel;
        [SerializeField] private StatusItem TMPHP;
        [SerializeField] private StatusItem TMPMove;
        [SerializeField] private StatusItem TMPJump;
        [SerializeField] private StatusItem TMPAction;
        [SerializeField] private StatusItem TMPAttack;
        [SerializeField] private StatusItem TMPDeffence;
        [SerializeField] private RawImage _characterSnapshot    = null;
        [SerializeField] private Image _selectCursor            = null;
        [SerializeField] private SkillBoxUI[] SkillBoxes;

        private List<ITooltipContent> _statusItemList = new List<ITooltipContent>();

        public void AssignCharacter( Character chara )
        {
            var charaParam = chara.Params.CharacterParam;

            TMPLevel.SetValueText( charaParam.Level.ToString() );
            TMPHP.SetValueText( $"{charaParam.CurHP} / {charaParam.MaxHP}" );
            TMPMove.SetValueText( charaParam.moveRange.ToString() );
            TMPJump.SetValueText( charaParam.jumpForce.ToString() );
            TMPAction.SetValueText( charaParam.maxActionGauge.ToString() );
            TMPAttack.SetValueText( charaParam.Atk.ToString() );
            TMPDeffence.SetValueText( charaParam.Def.ToString() );

            _characterSnapshot.texture = chara.Snapshot;

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
            if( null == _characterSnapshot )
            {
                return ( 0, 0 );
            }
            return ( _characterSnapshot.rectTransform.rect.width, _characterSnapshot.rectTransform.rect.height);
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
            foreach( var item in SkillBoxes )
            {
                item.Setup();
            }

            _selectCursor.gameObject.SetActive( false );
            gameObject.SetActive( false );
        }
    }
}