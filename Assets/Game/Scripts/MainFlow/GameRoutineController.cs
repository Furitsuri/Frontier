using Frontier.Battle;
using Frontier.Entities;
using Frontier.FormTroop;
using UnityEngine;
using Zenject;
using static Frontier.BattleFileLoader;

namespace Frontier
{
    public class GameRoutineController : FocusRoutineBase
    {
        [Header( "スキルコントローラオブジェクト" )]
        [SerializeField] private GameObject _skillCtrlObject;

        [Inject] private HierarchyBuilderBase _hierarchyBld         = null;
        [Inject] private CharacterDictionary _characterDictionary   = null;
        [Inject] private CharacterFactory _characterFactory         = null;
        [Inject] private UserDomain _userDomain                     = null;

        private StartMode _startMode = StartMode.NEW_GAME;
        private SubRoutineController _current;

        public void StartBattle()
        {
            SwitchTo( _hierarchyBld.InstantiateWithDiContainer<BattleRoutineController>( true ) );
        }

        public void StartRecruit()
        {
            // TODO : 仮実装
            _userDomain.AddMoney( 1000 );

            SwitchTo( _hierarchyBld.InstantiateWithDiContainer<FormTroopRoutineController>( false ) );
        }

        private void SwitchTo( SubRoutineController routine )
        {
            _current?.Exit();
            _current = routine;
            _current.Setup();
            _current.Run();
        }

        public override void Init()
        {
            base.Init();

            if( _startMode == StartMode.NEW_GAME )
            {
                // StartNewGame();
            }

            StartRecruit();
            // StartBattle();
        }

        public override void UpdateRoutine()
        {
            _current?.Update();
        }
        public override void LateUpdateRoutine() 
        {
            _current?.LateUpdate();
        }
        public override void FixedUpdateRoutine()
        {
            _current?.FixedUpdate();
        }
        public override int GetPriority() { return ( int ) FocusRoutinePriority.MAIN_FLOW; }

        private void StartNewGame()
        {
            // TODO : 仮実装
            CharacterStatusData data = new CharacterStatusData()
            {
                Name = "Hero",
                CharacterTag    = ( int ) CHARACTER_TAG.PLAYER,
                CharacterIndex  = 0,
                MaxHP           = 30,
                Atk             = 20,
                Def             = 10,
                MoveRange       = 5,
                JumpForce       = 2,
                AtkRange        = 1,
                ActGaugeMax     = 5,
                ActRecovery     = 2,
                InitGridIndex   = 0,
                InitDir         = ( int ) Direction.FORWARD,
                Skills = new int[] { 0, -1, -1, -1 },
            };

            var hero = _characterFactory.CreateCharacter( (CHARACTER_TAG)data.CharacterTag, 0, data );

            _characterDictionary.Add( hero.CharaKey, hero );    // 生成した主人公を登録

            CharacterStatusData archerData = new CharacterStatusData()
            {
                Name = "Archer",
                CharacterTag = ( int ) CHARACTER_TAG.PLAYER,
                CharacterIndex = 1,
                MaxHP = 30,
                Atk = 20,
                Def = 10,
                MoveRange = 5,
                JumpForce = 2,
                AtkRange = 2,
                ActGaugeMax = 5,
                ActRecovery = 2,
                InitGridIndex = 0,
                InitDir = ( int ) Direction.FORWARD,
                Skills = new int[] { 1, -1, -1, -1 },
            };

            var archer = _characterFactory.CreateCharacter( ( CHARACTER_TAG ) archerData.CharacterTag, 1, archerData );

            _characterDictionary.Add( archer.CharaKey, archer );    // テスト用にもう一人登録
        }
    }
}