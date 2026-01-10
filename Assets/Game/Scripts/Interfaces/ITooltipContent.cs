using UnityEngine;

public interface ITooltipContent
{
    public void SetTooltipText( string text )
    {
        // デフォルト実装は何もしない
    }

    public string GetTooltipText();

    public RectTransform GetRectTransform();
}