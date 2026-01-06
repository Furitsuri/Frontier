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

        private List<RectTransform> _statusItemRectList = new List<RectTransform>();

        public void AssignCharacter( Character chara )
        {
            var charaParam = chara.Params.CharacterParam;

            TMPLevel.SetValueText( charaParam.Level.ToString() );
            TMPHP.SetValueText( $"{charaParam.CurHP} / {charaParam.MaxHP}" );
            TMPLevel.SetValueText( charaParam.moveRange.ToString() );
            TMPLevel.SetValueText( charaParam.jumpForce.ToString() );
            TMPLevel.SetValueText( charaParam.maxActionGauge.ToString() );
            TMPLevel.SetValueText( charaParam.Atk.ToString() );
            TMPLevel.SetValueText( charaParam.Def.ToString() );

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

        public List<RectTransform> GetStatusItemRectList()
        {
            return _statusItemRectList;
        }

        private void InitStatusItemRectList()
        {
            _statusItemRectList.Clear();
            _statusItemRectList.Add( TMPLevel.GetRectTransform() );
            _statusItemRectList.Add( TMPHP.GetRectTransform() );
            _statusItemRectList.Add( TMPMove.GetRectTransform() );
            _statusItemRectList.Add( TMPJump.GetRectTransform() );
            _statusItemRectList.Add( TMPAction.GetRectTransform() );
            _statusItemRectList.Add( TMPAttack.GetRectTransform() );
            _statusItemRectList.Add( TMPDeffence.GetRectTransform() );

            foreach( var item in SkillBoxes )
            {
                if( !item.gameObject.activeSelf ) { continue; }
                _statusItemRectList.Add( item.GetRectTransform() );
            }

            // カーソルの初期位置をリストの先頭の要素で初期化
            SetSelectCursorRect( _statusItemRectList[0].anchoredPosition, _statusItemRectList[0].sizeDelta );
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