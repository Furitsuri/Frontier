using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleUISystem : MonoBehaviour
{
    public static BattleUISystem Instance { get; private set; }

    [Header("SelectGrid")]
    public SelectGridUI SelectGridCursor;         // グリッド選択に用いるカーソル
    public SelectGridUI AttackTargetCursor;       // 攻撃対象グリッド選択に用いるカーソル

    [Header("PlayerParam")]
    public CharacterParameterUI PlayerParameter;  // 選択グリッド上に存在するキャラクターのパラメータ表示UI
    public CharacterParameterUI EnemyParameter;   // 攻撃対象キャラクターのパラメータ表示UI

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
