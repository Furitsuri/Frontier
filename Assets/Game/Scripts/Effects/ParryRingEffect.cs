using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParryRingEffect : MonoBehaviour
{

    [SerializeField]
    private Material _ringMaterial;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (_ringMaterial != null)
        {
            Graphics.Blit(source, destination, _ringMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}
