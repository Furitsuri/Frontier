using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VirtualKeyboardInput : MonoBehaviour
{
    static public VirtualKeyboardInput Instance { get; private set; }

    [SerializeField] private TMP_Text inputDisplay;

    private string currentInput = "";

    private void Awake()
    {
        Instance = this;
    }

    public void AppendCharacter(string c)
    {
        currentInput += c;
        UpdateDisplay();
    }

    public void Backspace()
    {
        if (currentInput.Length > 0)
        {
            currentInput = currentInput.Substring(0, currentInput.Length - 1);
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        inputDisplay.text = currentInput;
    }

    public string GetCurrentInput() => currentInput;
}