using Frontier.Entities;
using System.Collections;
using UnityEngine;

namespace Frontier
{
    public struct CharacterKey
    {
        public CHARACTER_TAG CharacterTag;
        public int CharacterIndex;
        public static readonly CharacterKey Invalid = new CharacterKey( CHARACTER_TAG.NONE, -1 );

        public CharacterKey( CHARACTER_TAG tag, int index )
        {
            CharacterTag    = tag;
            CharacterIndex  = index;
        }

        public bool IsValid()
        {
            return this != Invalid;
        }

        static public bool operator ==( CharacterKey a, CharacterKey b )
        {
            return a.CharacterTag == b.CharacterTag && a.CharacterIndex == b.CharacterIndex;
        }

        static public bool operator !=( CharacterKey a, CharacterKey b )
        {
            return a.CharacterTag != b.CharacterTag || a.CharacterIndex != b.CharacterIndex;
        }

        public override bool Equals( object obj )
        {
            if( obj is CharacterKey )
            {
                return this == ( CharacterKey ) obj;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return CharacterTag.GetHashCode() ^ CharacterIndex.GetHashCode();
        }
    }
}