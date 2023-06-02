using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerCommandUI : MonoBehaviour
{
    private int m_SelectCommandIndex;
    private TextMeshProUGUI[] m_TMPs;
    private PLSelectCommandState m_PLSelectScript;

    void Awake()
    {
        // キャッシュとして保持
        m_TMPs = gameObject.GetComponentsInChildren<TextMeshProUGUI>();
    }

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        UpdateSelectBaseCommand();
    }

    void UpdateSelectBaseCommand()
    {
        m_SelectCommandIndex = m_PLSelectScript.SelectCommandIndex;

        // 一度全てを白色に設定
        foreach( TextMeshProUGUI tmp in m_TMPs )
        {
            tmp.color = Color.white;
        }

        // 選択項目を赤色に設定
        m_TMPs[m_SelectCommandIndex].color = Color.red;
    }

    // スクリプトの登録
    public void registPLCommandScript( PLSelectCommandState script )
    {
        m_PLSelectScript = script;
    }
}