using System.Collections.Generic;
using Frontier.Entities;

namespace Frontier
{
    /// <summary>
    /// キャラクター辞書
    /// </summary>
    public class CharacterDictionary
    {
        private Dictionary<CharacterKey, Character> dict = new Dictionary<CharacterKey, Character>();

        public void Add( in CharacterKey key, Character character )
        {
            dict.Add( key, character );
        }

        public void Remove( in CharacterKey key )
        {
            dict.Remove( key );
        }

        public bool IsContains( in CharacterKey key )
        {
            return dict.ContainsKey( key );
        }

        public Character Get( in CharacterKey key )
        {
            if( !key.IsValid() ) { return null; }

            return dict[key];
        }

        public Character Get( CHARACTER_TAG tag, int index )
        {
            return dict[new CharacterKey( tag, index )];
        }

        public List<Character> GetAllCharacters()
        {
            List<Character> characters = new List<Character>();
            foreach( var kvp in dict )
            {
                characters.Add( kvp.Value );
            }

            return characters;
        }

        public List<Character> GetCharacterList( CHARACTER_TAG tag )
        {   List<Character> characters = new List<Character>();
            foreach( var kvp in dict )
            {
                if( kvp.Key.CharacterTag == tag )
                {
                    characters.Add( kvp.Value );
                }
            }

            return characters;
        }
    }
}