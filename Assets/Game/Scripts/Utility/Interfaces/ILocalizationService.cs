using System;

public interface ILocalizationService
{
    string Get( string key );
    event Action OnLanguageChanged;
}