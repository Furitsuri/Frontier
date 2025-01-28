using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageColorAdapter : IColorEditable
{
    private UnityEngine.UI.Image image;

    public ImageColorAdapter(UnityEngine.UI.Image image)
    {
        this.image = image;
    }

    public Color Color
    {
        get => image.color;
        set => image.color = value;
    }
}

public class RawImageColorAdapter : IColorEditable
{
    private RawImage rawImage;

    public RawImageColorAdapter(RawImage rawImage)
    {
        this.rawImage = rawImage;
    }

    public Color Color
    {
        get => rawImage.color;
        set => rawImage.color = value;
    }
}