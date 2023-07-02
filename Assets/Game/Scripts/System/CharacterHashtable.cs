using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterHashtable : Hashtable
{
    static string[] tagStrings = new string[] {  "PL", "EM", "OT"};

    public struct Key
    {
        public Character.CHARACTER_TAG characterTag;
        public int characterIndex;
        public Key(Character.CHARACTER_TAG tag, int index)
        {
            characterTag = tag;
            characterIndex = index;
        }
    }

    public void Add(Key key, Character character )
    {
        string hashKey = ConvertHashKey(key.characterTag, key.characterIndex);

        base.Add(hashKey, character);
    }

    public object Get(Key key)
    {
        string hashKey = ConvertHashKey(key.characterTag, key.characterIndex);

        return this[hashKey];
    }

    private string ConvertHashKey(Character.CHARACTER_TAG tag, int characterIndex)
    {
        // 3åÖà»è„ÇÃê›íËÇÕëzíËÇµÇ»Ç¢
        Debug.Assert(characterIndex.ToString().Length <= 2);

        string indexString = characterIndex.ToString();

        string hashKey = tagStrings[(int)tag] + ((indexString.Length <= 1) ? "0" + indexString :indexString );

        return hashKey;
    }
}
