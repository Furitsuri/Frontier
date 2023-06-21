using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterHashtable : Hashtable
{
    string[] tagStrings = new string[] {  "PL", "EM", "OT"};

    public void Add((Character.CHARACTER_TAG tag, int characterIndex) tuple, Character character )
    {
        string hashKey = ConvertHashKey(tuple.tag, tuple.characterIndex);

        base.Add(hashKey, character);
    }

    public object Get( (Character.CHARACTER_TAG tag, int characterIndex) tuple )
    {
        string hashKey = ConvertHashKey(tuple.tag, tuple.characterIndex);

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
