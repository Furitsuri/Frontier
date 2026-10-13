using System.Collections.Generic;

namespace Frontier.Entities.Ai
{
    /// <summary>
    /// 思考タイプがINTERCEPTであるキャラクターを、集団単位で管理します。
    /// 単独型は「構成員が1人の集団」として扱うことで、行動開始の判定を集団型と共通化しています。
    /// </summary>
    public class InterceptGroupRegistry
    {
        /// <summary>
        /// 行動開始を共にする集団です
        /// </summary>
        private class Group
        {
            public CHARACTER_TAG Tag;
            public bool IsActivated;
            public List<AiIntercept> Members = new List<AiIntercept>();
        }

        private List<Group> _groups = new List<Group>();
        // 集団型の集団を、陣営と集団IDから引くためのテーブル(単独型の集団は登録されません)
        private Dictionary<(CHARACTER_TAG tag, int groupId), Group> _groupTable = new Dictionary<(CHARACTER_TAG tag, int groupId), Group>();
        private Dictionary<AiIntercept, Group> _memberToGroup = new Dictionary<AiIntercept, Group>();

        /// <summary>
        /// 初期化します
        /// </summary>
        public void Init()
        {
            _groups.Clear();
            _groupTable.Clear();
            _memberToGroup.Clear();
        }

        /// <summary>
        /// INTERCEPT型のAIを集団に登録します。
        /// 集団型は同じ陣営・同じ集団IDのもの同士が同じ集団となり、単独型は自身のみの集団となります
        /// </summary>
        /// <param name="member">登録するAI</param>
        /// <param name="tag">登録するAIの陣営</param>
        /// <param name="type">待ち構え方のタイプ</param>
        /// <param name="groupId">集団ID(集団型でのみ使用)</param>
        public void Register( AiIntercept member, CHARACTER_TAG tag, InterceptType type, int groupId )
        {
            if( _memberToGroup.ContainsKey( member ) ) { return; }

            Group group = null;

            if( type == InterceptType.GROUP )
            {
                if( !_groupTable.TryGetValue( (tag, groupId), out group ) )
                {
                    group = CreateGroup( tag );
                    _groupTable.Add( (tag, groupId), group );
                }
            }
            else
            {
                group = CreateGroup( tag );
            }

            group.Members.Add( member );
            _memberToGroup.Add( member, group );

            // 既に行動を開始している集団に後から加わった場合は、同様に行動を開始させる
            if( group.IsActivated ) { member.Activate(); }
        }

        /// <summary>
        /// INTERCEPT型のAIを集団から除外します(死亡時など)
        /// </summary>
        /// <param name="member">除外するAI</param>
        public void Unregister( AiIntercept member )
        {
            if( !_memberToGroup.TryGetValue( member, out var group ) ) { return; }

            group.Members.Remove( member );
            _memberToGroup.Remove( member );
        }

        /// <summary>
        /// 指定陣営の、まだ行動を開始していない集団それぞれについて、
        /// 構成員の誰かの行動範囲に攻撃対象が入っていれば、その集団の構成員全てに行動を開始させます
        /// </summary>
        /// <param name="tag">判定する陣営</param>
        public void ActivateGroupsDetectingOpponent( CHARACTER_TAG tag )
        {
            foreach( var group in _groups )
            {
                if( group.Tag != tag || group.IsActivated ) { continue; }

                foreach( var member in group.Members )
                {
                    if( !member.IsOpponentInActionRange() ) { continue; }

                    Activate( group );
                    break;
                }
            }
        }

        /// <summary>
        /// 指定のAIが所属する集団の構成員全てに、行動を開始させます。
        /// 行動範囲外から攻撃を受けた際など、範囲への侵入以外を契機として行動を開始させる場合に使用します
        /// </summary>
        /// <param name="member">行動を開始させる集団の構成員</param>
        public void ActivateGroupOf( AiIntercept member )
        {
            if( !_memberToGroup.TryGetValue( member, out var group ) ) { return; }

            Activate( group );
        }

        private Group CreateGroup( CHARACTER_TAG tag )
        {
            var group = new Group { Tag = tag, IsActivated = false };
            _groups.Add( group );

            return group;
        }

        private void Activate( Group group )
        {
            group.IsActivated = true;

            foreach( var member in group.Members )
            {
                member.Activate();
            }
        }
    }
}
