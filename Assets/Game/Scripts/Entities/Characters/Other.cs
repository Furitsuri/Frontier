using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Entities
{
    // 第三勢力は形式上のものとして、中身は敵と同一にする
    // 仕様変更があれば処理を追加する
    public class Other : Enemy
    {
        public override void OnFieldEnter()
        {
            base.OnFieldEnter();
            LazyInject.GetOrCreate( ref _fieldLogic, () => _hierarchyBld.CreateComponentNestedParentWithDiContainer<OtherFieldLogic>( gameObject, true, false, "FieldLogic" ) );
            _fieldLogic.Setup();
            _fieldLogic.Regist( this );
            _fieldLogic.Init();
        }

        public override void OnBattleEnter( BattleCameraController btlCamCtrl )
        {
            LazyInject.GetOrCreate( ref _battleLogic, () => _hierarchyBld.CreateComponentNestedParentWithDiContainer<OtherBattleLogic>( gameObject, true, false, "BattleLogic" ) );
            _battleLogic.Setup();
            _battleLogic.Regist( this );
            _battleLogic.Init();

            _battleLogic.SetThinkType( ThinkingType );

            base.OnBattleEnter( btlCamCtrl );   // 基底クラスのOnBattleEnterは最後に呼ぶ
        }
    }
}
