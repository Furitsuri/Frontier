using System;
using Frontier.Entities;

public interface ICharacterStatusOpener
{
    event Action<Character> OnOpenCharacterStatus;
}
