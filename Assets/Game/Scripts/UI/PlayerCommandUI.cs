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
        // �L���b�V���Ƃ��ĕێ�
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

        // ��x�S�Ă𔒐F�ɐݒ�
        foreach( TextMeshProUGUI tmp in m_TMPs )
        {
            tmp.color = Color.white;
        }

        // �I�����ڂ�ԐF�ɐݒ�
        m_TMPs[m_SelectCommandIndex].color = Color.red;
    }

    // �X�N���v�g�̓o�^
    public void registPLCommandScript( PLSelectCommandState script )
    {
        m_PLSelectScript = script;
    }
}