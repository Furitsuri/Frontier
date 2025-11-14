using UnityEngine;
using UnityEngine.UI;

[RequireComponent( typeof( Image ) )]
public class ImageBlinker : MonoBehaviour
{
    [SerializeField] private float speed = 2f;
    private Image _image;
    private Color baseColor;

    void Start()
    {
        _image = GetComponent<Image>();
        baseColor = _image.color;
    }

    void Update()
    {
        float alpha = Mathf.PingPong( Time.time * speed, 1f );
        _image.color = new Color( baseColor.r, baseColor.g, baseColor.b, alpha );
    }
}