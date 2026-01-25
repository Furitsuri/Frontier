namespace Frontier.Entities
{
    public class Enemy : Npc
    {
        /// <summary>
        /// 初期化します
        /// </summary>
        public override void Init()
        {
            base.Init();
        }

        public override void OnFieldEnter()
        {
            base.OnFieldEnter();
            LazyInject.GetOrCreate( ref _fieldLogic, () => _hierarchyBld.CreateComponentNestedParentWithDiContainer<EnemyFieldLogic>( gameObject, true, false, "FieldLogic" ) );
            _fieldLogic.Setup();
            _fieldLogic.Regist( this );
            _fieldLogic.Init();
        }

        public override void OnBattleEnter( BattleCameraController btlCamCtrl )
        {
            LazyInject.GetOrCreate( ref _battleLogic, () => _hierarchyBld.CreateComponentNestedParentWithDiContainer<EnemyBattleLogic>( gameObject, true, false, "BattleLogic" ) );
            _battleLogic.Setup();
            _battleLogic.Regist( this );
            _battleLogic.Init();

            _battleLogic.SetThinkType( ThinkingType );

            base.OnBattleEnter( btlCamCtrl );   // 基底クラスのOnBattleEnterは最後に呼ぶ
        }
    }
}