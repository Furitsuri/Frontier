using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleUISystem : MonoBehaviour
{
    public static BattleUISystem Instance { get; private set; }

    [Header("SelectGrid")]
    public SelectGridUI SelectGridCursor;         // �O���b�h�I���ɗp����J�[�\��
    public SelectGridUI AttackTargetCursor;       // �U���ΏۃO���b�h�I���ɗp����J�[�\��

    [Header("PlayerParam")]
    public CharacterParameterUI PlayerParameter;  // �I���O���b�h��ɑ��݂���L�����N�^�[�̃p�����[�^�\��UI
    public CharacterParameterUI EnemyParameter;   // �U���ΏۃL�����N�^�[�̃p�����[�^�\��UI

    [Header("PlayerCommand")]
    public PlayerCommandUI PLCommandWindow;

    void Awake()
    {
        Instance = this;
    }

    public void ToggleSelectGrid( bool isActive )
    {
        SelectGridCursor.gameObject.SetActive( isActive );
    }

    public void ToggleAttackTargetGrid(bool isActive)
    {
        AttackTargetCursor.gameObject.SetActive(isActive);
    }

    public void TogglePlayerParameter( bool isActive )
    {
        PlayerParameter.gameObject.SetActive( isActive );
    }

    public void ToggleEnemyParameter(bool isActive)
    {
        EnemyParameter.gameObject.SetActive(isActive);
    }

    public void TogglePLCommand( bool isActive )
    {
        PLCommandWindow.gameObject.SetActive( isActive );
    }
}
