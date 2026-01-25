using Frontier.Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class CharacterRegistry
    {
        List<Character> _characters = new List<Character>();

        public IReadOnlyList<Character> Characters => _characters;
        public void Add( Character c ) { _characters.Add( c ); }
        public Character Get( int id ) { return null; }
    }
}