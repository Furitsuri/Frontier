using System;

public interface ILocalizationService
{
    Language CurrentLanguage { get; }

    string Get( LocKey key );
    void ChangeLanguage( Language lang );
    event Action OnLanguageChanged;
}
