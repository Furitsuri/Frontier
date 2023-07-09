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
        // �L���b�V���Ƃ��ĕێ�
        m_TMPs = gameObject.GetComponentsInChildren<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateSelectBaseCommand();
    }

    void UpdateSelectBaseCommand()
    {
        // ��x�S�Ă𔒐F�ɐݒ�
        foreach( TextMeshProUGUI tmp in m_TMPs )
        {
             tmp.color = Color.white;
        }
        // �g�p�o���Ȃ��R�}���h���D�F��
        for( int i = 0; i < m_Enables.Length; ++i )
        {
            if (m_Enables[i])
            {
                m_TMPs[i].color = Color.gray;
            }
        }

        // �I�����ڂ�ԐF�ɐݒ�
        m_TMPs[m_PLSelectScript.SelectCommandIndex].color = Color.red;
    }

    // �X�N���v�g�̓o�^
    public void registPLCommandScript( PLSelectCommandState script )
    {
        m_PLSelectScript = script;
    }

    public void RegistUnenableCommandIndexs( ref bool[] enables )
    {
        m_Enables = enables;
    }
}