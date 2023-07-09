using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerCommandUI : MonoBehaviour
{
    private TextMeshProUGUI[] m_TMPs;
    private PLSelectCommandState m_PLSelectScript;
    private bool[] m_Enables;

    void Awake()
    {
        // キャッシュとして保持
        m_TMPs = gameObject.GetComponentsInChildren<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateSelectBaseCommand();
    }

    void UpdateSelectBaseCommand()
    {
        // 一度全てを白色に設定
        foreach( TextMeshProUGUI tmp in m_TMPs )
        {
             tmp.color = Color.white;
        }
        // 使用出来ないコマンドを灰色に
        for( int i = 0; i < m_Enables.Length; ++i )
        {
            if (m_Enables[i])
            {
                m_TMPs[i].color = Color.gray;
            }
        }

        // 選択項目を赤色に設定
        m_TMPs[m_PLSelectScript.SelectCommandIndex].color = Color.red;
    }

    // スクリプトの登録
    public void registPLCommandScript( PLSelectCommandState script )
    {
        m_PLSelectScript = script;
    }

    public void RegistUnenableCommandIndexs( ref bool[] enables )
    {
        m_Enables = enables;
    }
}