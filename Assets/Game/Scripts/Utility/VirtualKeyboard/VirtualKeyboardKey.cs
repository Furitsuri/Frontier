using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VirtualKeyboardKey : MonoBehaviour
{
    [SerializeField] private string keyValue;
    [SerializeField] private TMP_Text displayText;

    public void OnKeyPressed()
    {
        VirtualKeyboardInput.Instance.AppendCharacter(keyValue);
    }

    private void Start()
    {
        if (displayText != null)
            displayText.text = keyValue;
    }
}