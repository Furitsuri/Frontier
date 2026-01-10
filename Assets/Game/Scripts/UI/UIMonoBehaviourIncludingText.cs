using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

/// <summary>
/// 文字列を含むUIの基底クラスです。
/// ローカライズ対応のため、文字列を含むUIはこのクラスを必ず継承してください。
/// </summary>
public class UIMonoBehaviourIncludingText : UiMonoBehaviour, ILocalizedText
{
    [SerializeField] protected string _textKey = string.Empty;

    [Inject] protected ILocalizationService _localization = null;

    void OnEnable()
    {
        EnableRefreshText();
    }

    void OnDisable()
    {
        DisableRefreshText();
    }

    /// <summary>
    /// テキストのローカライズリフレッシュを有効にします。
    /// ※UI毎に仕様が異なる都合でOnEnable上には実装していません。
    /// </summary>
    protected void EnableRefreshText()
    {
        _localization.OnLanguageChanged += RefreshText;
        RefreshText();
    }

    /// <summary>
    /// テキストのローカライズリフレッシュを無効にします。
    /// ※UI毎に仕様が異なる都合でOnDisable上には実装していません。
    /// </summary>
    protected void DisableRefreshText()
    {
        _localization.OnLanguageChanged -= RefreshText;
    }

    virtual public void RefreshText() { }
}
