using Frontier.Stage;
using Frontier.Battle;
using Frontier.Entities;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.UI
{
    public class BattleParameterUI : UiMonoBehaviour
    {
        [Header( "LeftWindowParam" )]
        [SerializeField] private CharacterParameterUI _playerParameter;        // 左側表示のパラメータUIウィンドウ

        [Header( "RightWindowParam" )]
        [SerializeField] private CharacterParameterUI _enemyParameter;         // 右側表示のパラメータUIウィンドウ

        [Header( "Parameter_attackDirection" )]
        [SerializeField] private ParameterAttackDirectionUI _attackDirection;  // パラメータUI間上の攻撃(回復)元から対象への表示

        public CharacterParameterUI PlayerParameter => _playerParameter;
        public CharacterParameterUI EnemyParameter => _enemyParameter;
        public ParameterAttackDirectionUI AttackDirection => _attackDirection;

        public override void Setup()
        {
            _playerParameter.Setup();
            _playerParameter.SetupCamera();
            _enemyParameter.Setup();
            _enemyParameter.SetupCamera();
            _attackDirection.Setup();

            _playerParameter.Init();
            _enemyParameter.Init();

            _playerParameter.gameObject.SetActive( false );
            _enemyParameter.gameObject.SetActive( false );
        }
    }
}