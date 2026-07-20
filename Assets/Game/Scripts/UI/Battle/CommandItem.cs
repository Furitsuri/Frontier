using TMPro;
using UnityEngine;

public class CommandItem : UIMonoBehaviourIncludingText
{
    [SerializeField] private TextMeshProUGUI _commandName;

    /// <summary>
    /// OnEnableでEnableRefreshText()を呼び出さないため、空実装にしています。
    /// </summary>
    void OnEnable() { }

    public void SetTextKey( string key )
    {
        _textKey = key;
        EnableRefreshText();
    }

    public void SetColor( Color color )
    {
        _commandName.color = color;
    }

    public float GetFontSize()
    {
        return _commandName.fontSize;
    }

    /// <summary>
    /// 現在設定されているテキストを1行で表示するために必要な幅を返します
    /// </summary>
    public float GetPreferredWidth()
    {
        return _commandName.GetPreferredValues().x;
    }

    public override void Setup()
    {
        _commandName = this.GetComponent<TextMeshProUGUI>();
    }

    #region ILocalizedText implementation

    public override void RefreshText()
    {
        _commandName.text = _localization.Get( _textKey );
    }

#endregion  // ILocalizedText implementation
}
