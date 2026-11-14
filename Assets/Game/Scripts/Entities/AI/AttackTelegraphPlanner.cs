using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Zenject;

namespace Frontier.Entities.Ai
{
    /// <summary>
    /// NPCの予兆を伴うスキル(移動を伴う攻撃・一定範囲への攻撃。SkillsData.IsTelegraphedSkill)の使用を、自軍のフェーズの終了時に予約し、
    /// 次の自軍のフェーズでその予約を実行出来るかを判定します。
    ///
    /// 予約:
    ///   予兆を伴うスキルを装備しているNPCのみを対象に、現在の位置から(移動せずに)使用した場合に攻撃対象が1体以上存在する候補を求め、
    ///   AIの評価で最も高いものを予約します(AttackTelegraphRegistryへ登録し、予兆を表示します)。候補が無ければ予約しません。
    ///   アクションゲージは、次のフェーズの開始時の回復を見込んで判定します。
    ///
    /// 処理の負荷:
    ///   範囲の計算は現在の位置1か所のみから行うため、通常の行動の決定(移動先の全タイルから候補を列挙)と比べて軽量ですが、
    ///   対象のキャラクターが多い場合に1フレームへ集中しないよう、キャラクター1体ごとに区切り、1フレームあたりの処理時間の予算内で少しずつ進めます。
    ///   範囲の計算は盤面の共有データ(TileDataHandlerの作業用の層・メモ等)を書き換えながら行うため、別スレッドでの並列化は行いません。
    /// </summary>
    public class AttackTelegraphPlanner
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld         = null;
        [Inject] private AttackTelegraphRegistry _registry          = null;
#if UNITY_EDITOR
        [InjectOptional] private Frontier.DebugTools.DebugSettings _debugSettings = null;
#endif // UNITY_EDITOR

        private AttackOptionCollector _collector    = null;
        private IEnumerator _planRoutine            = null;
        private double _totalPlanMs                 = 0.0;  // 計測用: 予約の判断に要した処理時間の合計
        private int _planFrameCount                 = 0;    // 計測用: 予約の判断に要したフレーム数

        /// <summary>
        /// 予約の判断を進めている途中であるか
        /// </summary>
        public bool IsPlanning => _planRoutine != null;

        /// <summary>
        /// 指定の軍の予約の判断を開始します。
        /// 以前の予約(実行・取り消しされずに残っているもの)は、判断の際に取り消されます。
        /// 判断はStepを呼び出す度に進みます
        /// </summary>
        /// <param name="troop">予約を行う軍のキャラクター</param>
        // MEMO : BattleRoutineControllerはこのクラスを注入する側であり、DIコンテナへのバインドもこのクラスの生成より後になるため、
        //        軍のキャラクターは注入ではなく引数で受け取る
        public void Begin( IEnumerable<Character> troop )
        {
            LazyInject.GetOrCreate( ref _collector, () => _hierarchyBld.InstantiateWithDiContainer<AttackOptionCollector>( false ) );

            // 判断の途中で撃破等によって並びが変わっても影響を受けないよう、開始時点の一覧を複製しておく
            _planRoutine    = PlanRoutine( new List<Character>( troop ) );
            _totalPlanMs    = 0.0;
            _planFrameCount = 0;
        }

        /// <summary>
        /// 予約の判断を、指定の処理時間の予算を超えるまで進めます
        /// </summary>
        /// <param name="budgetMs">1回の呼び出しあたりの処理時間の予算(ミリ秒)。キャラクター1体分の判断の途中では打ち切りません</param>
        /// <returns>全ての判断を終えたか否か</returns>
        public bool Step( double budgetMs )
        {
            if( _planRoutine == null ) { return true; }

            var stopwatch = Stopwatch.StartNew();
            ++_planFrameCount;

            while( _planRoutine.MoveNext() )
            {
                if( budgetMs <= stopwatch.Elapsed.TotalMilliseconds )
                {
                    _totalPlanMs += stopwatch.Elapsed.TotalMilliseconds;
                    LogTiming( $"予約の判断 {stopwatch.Elapsed.TotalMilliseconds:F2}ms (予算超過のため次フレームへ持ち越し)" );
                    return false;
                }
            }

            _totalPlanMs += stopwatch.Elapsed.TotalMilliseconds;
            LogTiming( $"予約の判断 完了 合計{_totalPlanMs:F2}ms / {_planFrameCount}フレーム 予約{_registry.Count}体" );
            _planRoutine = null;

            return true;
        }

        /// <summary>
        /// 予約していた攻撃を、現在の盤面で実行出来るかを判定します。
        /// 実行出来る場合は、攻撃対象等を求め直した上で、このフェーズの行動としてAIに決定済みにさせます。
        /// 実行出来ない場合は、予約を取り消した上で一回休み(行動終了とし、モデルをグレーにする)とします。
        /// 予約していないキャラクターの場合は何もしません
        /// </summary>
        /// <param name="chara">判定するキャラクター</param>
        /// <returns>予約を実行するか否か(予約していない、または一回休みとなった場合はfalse)</returns>
        public bool TryPrepareReservedAction( Character chara )
        {
            if( !_registry.TryGet( chara, out var reserved ) ) { return false; }

            LazyInject.GetOrCreate( ref _collector, () => _hierarchyBld.InstantiateWithDiContainer<AttackOptionCollector>( false ) );

            if( chara.BattleLogic is EnemyBattleLogic enemyLogic && _collector.TryRebuildTelegraphOption( chara, in reserved, out var rebuilt ) )
            {
                enemyLogic.ApplyReservedAction( in rebuilt );
                return true;
            }

            // 予約した攻撃が成立しない場合は一回休みとする
            // TODO : 成立しない場合の扱い(条件ごとに別の行動を取らせる等)は、仕様を詰めた上で見直す
            _registry.Remove( chara );
            chara.BattleLogic.BeImpossibleAction();
            LogTiming( $"予約した攻撃が成立しないため一回休み {chara.GetCharacterKey().CharacterTag}#{chara.GetCharacterKey().CharacterIndex}" );

            return false;
        }

        /// <summary>
        /// 攻撃を予約しているかを取得します
        /// </summary>
        public bool HasReservation( Character chara )
        {
            return _registry.Has( chara );
        }

        /// <summary>
        /// 予約していた攻撃の実行を開始したことを通知します(予約を取り除き、予兆の表示を消去します)
        /// </summary>
        public void NotifyReservedActionStarted( Character chara )
        {
            _registry.Remove( chara );
        }

        /// <summary>
        /// キャラクター1体ごとに区切って予約の判断を行います
        /// </summary>
        private IEnumerator PlanRoutine( List<Character> troop )
        {
            foreach( var chara in troop )
            {
                _registry.Remove( chara );

                if( TryPlan( chara, out var selected ) )
                {
                    _registry.Reserve( chara, in selected );
                }

                yield return null;
            }
        }

        /// <summary>
        /// 指定のキャラクターが、次のフェーズで使用する予兆を伴うスキルによる攻撃を選択します
        /// </summary>
        private bool TryPlan( Character chara, out AttackOption selected )
        {
            selected = default;

            if( chara == null || chara.BattleLogic == null ) { return false; }
            if( !AttackOptionCollector.HasTelegraphSkill( chara ) ) { return false; }

            var ai = chara.BattleLogic.GetAi();
            if( ai == null || !ai.CanTakeAction() ) { return false; }    // 待ち構えていてまだ行動を開始していない場合等は予約しない

            var status          = chara.GetStatusRef;
            int expectedGauge   = System.Math.Min( status.CurActionGauge + status.recoveryActionGauge, status.maxActionGauge );
            var options         = _collector.CollectTelegraphOptions( chara, expectedGauge );

            return ai.TrySelectTelegraphOption( options, out selected );
        }

        /// <summary>
        /// 処理時間の計測結果等をConsoleへ出力します(デバッグ設定の「AI TIMING LOG」が有効な場合のみ。エディタ以外では出力しません)
        /// </summary>
        [Conditional( "UNITY_EDITOR" )]
        private void LogTiming( string message )
        {
#if UNITY_EDITOR
            if( _debugSettings == null || !_debugSettings.LogTroopActionTiming ) { return; }

            UnityEngine.Debug.Log( $"[Telegraph] {message}" );
#endif // UNITY_EDITOR
        }
    }
}
