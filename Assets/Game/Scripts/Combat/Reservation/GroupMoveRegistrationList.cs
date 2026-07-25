using System.Collections.Generic;
using Frontier.Entities;

namespace Frontier.Battle
{
    /// <summary>
    /// グループ移動の対象として登録されたキャラクターを管理します。
    /// SkillActionReservationQueueと同様、DIInstallerからシングルトンとして登録し、必要な箇所から注入して使用してください。
    /// </summary>
    public class GroupMoveRegistrationList
    {
        private readonly List<CharacterKey> _registered = new List<CharacterKey>();

        public int Count => _registered.Count;
        public bool IsEmpty => _registered.Count == 0;

        /// <summary>指定キャラクターが登録済みかどうかを返します</summary>
        public bool Contains( Character character )
        {
            return _registered.Contains( character.GetCharacterKey() );
        }

        /// <summary>指定キャラクターを登録します(登録済みの場合は何もしません)</summary>
        public void Add( Character character )
        {
            var key = character.GetCharacterKey();
            if( _registered.Contains( key ) ) { return; }

            _registered.Add( key );
        }

        /// <summary>指定キャラクターの登録を解除します</summary>
        public void Remove( Character character )
        {
            _registered.Remove( character.GetCharacterKey() );
        }

        /// <summary>指定キーの登録を解除します(キャラクターが解決できない場合に使用します)</summary>
        public void Remove( CharacterKey key )
        {
            _registered.Remove( key );
        }

        /// <summary>登録内容をすべて破棄します</summary>
        public void Clear()
        {
            _registered.Clear();
        }

        /// <summary>登録されているキャラクターキーを登録順に列挙します(読み取り専用)</summary>
        public IReadOnlyList<CharacterKey> GetAll() => _registered;
    }
}
