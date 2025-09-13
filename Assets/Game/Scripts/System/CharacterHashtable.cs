using Frontier.Entities;
using System.Collections;
using UnityEngine;

namespace Frontier
{
    public class CharacterHashtable : Hashtable
    {
        static string[] tagStrings = new string[] { "PL", "EM", "OT" };

        public struct Key
        {
            public CHARACTER_TAG characterTag;
            public int characterIndex;
            public Key(CHARACTER_TAG tag, int index)
            {
                characterTag = tag;
                characterIndex = index;
            }

            static public bool operator ==(Key a, Key b)
            {
                return a.characterTag == b.characterTag && a.characterIndex == b.characterIndex;
            }

            static public bool operator !=(Key a, Key b)
            {
                return a.characterTag != b.characterTag || a.characterIndex != b.characterIndex;
            }

            override public bool Equals(object obj)
            {
                if (obj is Key)
                {
                    return this == (Key)obj;
                }

                return false;
            }

            override public int GetHashCode()
            {
                return characterTag.GetHashCode() ^ characterIndex.GetHashCode();
            }
        }

        public void Add(Key key, Character character)
        {
            string hashKey = ConvertHashKey(key.characterTag, key.characterIndex);

            base.Add(hashKey, character);
        }

        public object Get(Key key)
        {
            string hashKey = ConvertHashKey(key.characterTag, key.characterIndex);

            return this[hashKey];
        }

        private string ConvertHashKey(CHARACTER_TAG tag, int characterIndex)
        {
            // 3桁以上の設定は想定しない
            Debug.Assert(characterIndex.ToString().Length <= 2);

            string indexString = characterIndex.ToString();

            string hashKey = tagStrings[(int)tag] + ((indexString.Length <= 1) ? "0" + indexString : indexString);

            return hashKey;
        }
    }
}