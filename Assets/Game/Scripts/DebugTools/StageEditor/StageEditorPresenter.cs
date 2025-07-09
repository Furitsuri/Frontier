using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageEditorPresenter : MonoBehaviour
{
    public void Init()
    {
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// 
    /// </summary>
    public void ToggleView()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}
