using Frontier.Entities;
using System.Collections.Generic;

namespace Frontier.CharacterEdit
{
    /// <summary>
    /// CharacterEditHandler及びその配下(LevelUpHandler/SkillEquipHandler等)で共有される、
    /// 「今どのキャラクターを編集対象としているか」を表す文脈。
    /// 参照型として各階層にそのまま渡すことで、どの階層でL1/R1によりキャラクターが
    /// 切り替わっても、全階層が同じ状態を参照できます。
    /// </summary>
    public class CharacterEditContext
    {
        public List<Character> Characters { get; }
        public int CurrentIndex { get; private set; }

        public Character CurrentCharacter => Characters[CurrentIndex];

        public CharacterEditContext( List<Character> characters, int startIndex )
        {
            Characters = characters;
            CurrentIndex = startIndex;
        }

        public void MoveNext()
        {
            CurrentIndex = ( CurrentIndex + 1 ) % Characters.Count;
        }

        public void MovePrevious()
        {
            CurrentIndex = ( CurrentIndex - 1 + Characters.Count ) % Characters.Count;
        }
    }
}
