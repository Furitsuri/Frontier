using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleUISystem : MonoBehaviour
{
    public static BattleUISystem Instance { get; private set; }

    [Header("SelectGrid")]
    public SelectGridUI SelectGridCursor;

    [Header("PlayerCommand")]
    public PlayerCommandUI PLCommandWindow;

    void Awake()
    {
        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void ToggleSelectGrid(bool isActive)
    {
        SelectGridCursor.gameObject.SetActive(isActive);
    }

    public void TogglePLCommand(bool isActive)
    {
        PLCommandWindow.gameObject.SetActive( isActive );
    }
}
