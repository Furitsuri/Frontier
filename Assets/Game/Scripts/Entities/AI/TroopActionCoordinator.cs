using System.Collections.Generic;
using System.Diagnostics;
using Zenject;

namespace Frontier.Entities.Ai
{
    /// <summary>
    /// NPCの軍(敵・第三勢力)全体の行動を調整します。
    /// 各キャラクターの思考(攻撃の候補の列挙・評価)は個々のAIに任せたまま、
    /// 「次に誰が行動するか」と「味方の攻撃位置を塞がないための位置の評価」を、軍全体を見て決定します。
    /// 行動の度に現在の盤面で判断し直すため、攻撃の結果(パリィ・カウンター等)が予測と異なっても破綻しません。
    ///
    /// 次に行動するキャラクターの選び方:
    ///   1. 行動の途中のキャラクター(移動を終えて続けて攻撃する場合など)がいれば、そのキャラクター
    ///   2. 回復役以外の攻撃出来るキャラクターのうち、攻撃を行える位置の数が最も少ないキャラクター
    ///      (立てる位置が限られるキャラクターに先に位置を取らせ、融通の利くキャラクターが後から合わせるため)
    ///   3. 回復役(範囲内にHPが減っている味方がいて、回復を行えるキャラクター)
    ///      (攻撃役が先に位置を取った後に動くことで攻撃位置を塞ぎにくく、攻撃役が受けたダメージも含めて回復の対象を決められるため)
    ///   4. いずれもいない場合は、手番の順で最初のキャラクター(近づく等の行動はAIが決定する)
    ///   いずれも等しい場合は、手番の順(並び順)で先のキャラクターを優先します。
    /// </summary>
    public class TroopActionCoordinator
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private Frontier.Stage.StageController _stageCtrl = null;
#if UNITY_EDITOR
        [InjectOptional] private Frontier.DebugTools.DebugSettings _debugSettings = null;
#endif // UNITY_EDITOR

        private AttackOptionCollector _collector = null;

        // 調整の対象(このフェーズで行動し得る、行動を終えていないキャラクター)と、その識別子
        private readonly List<Character> _candidates            = new List<Character>();
        private readonly HashSet<CharacterKey> _candidateKeys   = new HashSet<CharacterKey>();

        // 各調整の対象が攻撃を行い得る位置(まだ行動していない味方が空けた場合を含む)。SelectNextActorの度に求め直す
        // MEMO : 回復の候補は含めない(回復は射程が広く多くの位置から行えるため、含めると攻撃役の位置取りの判断が薄まるため)
        private readonly Dictionary<Character, HashSet<int>> _potentialStandTiles = new Dictionary<Character, HashSet<int>>();
        // 調整の対象のうち、回復役(現時点で回復を行えるキャラクター)。SelectNextActorの度に求め直す
        private readonly HashSet<Character> _healers = new HashSet<Character>();

        // 直近のSelectNextActorで選んだキャラクターと、そのキャラクターに渡す「他の味方が攻撃位置として必要としているタイル」
        private Character _selectedActor                         = null;
        private Dictionary<int, float> _selectedContestedTiles  = null;

        /// <summary>
        /// 次に行動するキャラクターを選びます
        /// </summary>
        /// <param name="troop">軍のキャラクター(手番の順に並んだもの)</param>
        /// <returns>次に行動するキャラクター。行動し得るキャラクターがいない場合はnull</returns>
        public Character SelectNextActor( IEnumerable<Character> troop )
        {
            LazyInject.GetOrCreate( ref _collector, () => _hierarchyBld.InstantiateWithDiContainer<AttackOptionCollector>( false ) );

            var stopwatch = Stopwatch.StartNew();

            CollectCandidates( troop );
            _potentialStandTiles.Clear();
            _healers.Clear();
            _selectedContestedTiles = null;

            // 行動の途中(行動を決定済みで、まだ行動を終えていない)のキャラクターがいれば、その行動を続けさせる。
            // 行動は決定済みのため、攻撃を行い得る位置や必要タイルの計算は不要
            _selectedActor = FindActorInProgress();
            if( null != _selectedActor )
            {
                LogTiming( $"SelectNextActor {stopwatch.Elapsed.TotalMilliseconds:F2}ms (行動の途中のため計算なし) 対象{_candidates.Count}体 → {GetLabel( _selectedActor )}" );
                return _selectedActor;
            }

            // 各調整の対象について、攻撃を行い得る位置を1回ずつ求める(順番の決定と必要タイルの算出の双方で使い回す)
            foreach( var chara in _candidates )
            {
                _potentialStandTiles[chara] = CollectPotentialStandTiles( chara );
                if( _collector.HasAnyAllyTargetOption( chara ) ) { _healers.Add( chara ); }
            }
            double collectMs = stopwatch.Elapsed.TotalMilliseconds;

            _selectedActor          = SelectActorByStandTiles();
            _selectedContestedTiles = ( null != _selectedActor ) ? BuildContestedTiles( _selectedActor ) : null;
            double totalMs          = stopwatch.Elapsed.TotalMilliseconds;

            LogTiming( $"SelectNextActor {totalMs:F2}ms (攻撃・回復候補の列挙 {collectMs:F2}ms / 順番の決定・必要タイルの算出 {totalMs - collectMs:F2}ms) 対象{_candidates.Count}体(回復役{_healers.Count}体) → {GetLabel( _selectedActor )}" );

            return _selectedActor;
        }

        /// <summary>
        /// 指定のキャラクターの行動をAIに決定させ、その結果を返します。
        /// まだ決定していない場合は、他の味方が攻撃位置として必要としているタイルをAIに渡した上で決定させます
        /// </summary>
        /// <param name="actor">行動するキャラクター(SelectNextActorで選ばれたもの)</param>
        /// <returns>目的地・攻撃対象がそれぞれ有効か</returns>
        public (bool isValidDestination, bool isValidTarget) PrepareAction( Character actor )
        {
            if( actor.BattleLogic is not EnemyBattleLogic enemyLogic ) { return (false, false); }

            var ai = actor.BattleLogic.GetAi();
            if( ai.IsDetermined() ) { return enemyLogic.PrepareActionWithAI(); }  // 決定済みの結果を返すのみ

            var stopwatch = Stopwatch.StartNew();

            // SelectNextActorで選んだ直後であれば、その時点で求めたものを使う
            ai.SetContestedTiles( ( actor == _selectedActor ) ? _selectedContestedTiles : BuildContestedTiles( actor ) );
            var result = enemyLogic.PrepareActionWithAI();

            LogTiming( $"PrepareAction(AIによる行動の決定) {stopwatch.Elapsed.TotalMilliseconds:F2}ms {GetLabel( actor )} 移動先:{result.isValidDestination} 攻撃対象:{result.isValidTarget}" );

            return result;
        }

        /// <summary>
        /// 処理時間の計測結果をConsoleへ出力します(デバッグ設定で有効な場合のみ。エディタ以外では出力しません)
        /// </summary>
        [Conditional( "UNITY_EDITOR" )]
        private void LogTiming( string message )
        {
#if UNITY_EDITOR
            if( _debugSettings == null || !_debugSettings.LogTroopActionTiming ) { return; }

            UnityEngine.Debug.Log( $"[TroopAI] {message}" );
#endif // UNITY_EDITOR
        }

        /// <summary>
        /// ログに表示するキャラクターの識別名を取得します
        /// </summary>
        private static string GetLabel( Character chara )
        {
            if( null == chara ) { return "なし"; }

            var key = chara.GetCharacterKey();
            return $"{key.CharacterTag}#{key.CharacterIndex}";
        }

        /// <summary>
        /// 調整の対象となるキャラクターを集めます
        /// </summary>
        private void CollectCandidates( IEnumerable<Character> troop )
        {
            _candidates.Clear();
            _candidateKeys.Clear();

            foreach( var chara in troop )
            {
                if( null == chara || null == chara.BattleLogic ) { continue; }
                if( chara.BattleParams.TmpParam.IsEndAction() ) { continue; }

                var ai = chara.BattleLogic.GetAi();
                if( null == ai || !ai.CanTakeAction() ) { continue; }

                _candidates.Add( chara );
                _candidateKeys.Add( chara.GetCharacterKey() );
            }
        }

        /// <summary>
        /// 行動の途中(行動を決定済みで、まだ行動を終えていない)のキャラクターを探します
        /// </summary>
        /// <returns>行動の途中のキャラクター。いない場合はnull</returns>
        private Character FindActorInProgress()
        {
            foreach( var chara in _candidates )
            {
                if( chara.BattleLogic.GetAi().IsDetermined() ) { return chara; }
            }

            return null;
        }

        /// <summary>
        /// 調整の対象の中から、攻撃を行える位置の数と回復役であるかに基づいて次に行動するキャラクターを選びます。
        /// 回復役以外の攻撃出来るキャラクターのうち、攻撃を行える位置の数が最も少ないキャラクターを選び、
        /// いない場合は回復役を、それもいない場合は手番の順で最初のキャラクターを選びます(回復役同士、及びそれ以外は手番の順)
        /// </summary>
        private Character SelectActorByStandTiles()
        {
            if( _candidates.Count <= 0 ) { return null; }

            Character bestAttacker  = null;
            Character firstHealer   = null;
            int minStandTileNum     = int.MaxValue;
            foreach( var chara in _candidates )
            {
                // 回復役は攻撃役が行動を終えた後に動かす
                if( _healers.Contains( chara ) )
                {
                    firstHealer ??= chara;
                    continue;
                }

                int standTileNum = CountCurrentlyStandableTiles( chara, _potentialStandTiles[chara] );
                if( 0 < standTileNum && standTileNum < minStandTileNum )
                {
                    minStandTileNum = standTileNum;
                    bestAttacker    = chara;
                }
            }

            return bestAttacker ?? firstHealer ?? _candidates[0];
        }

        /// <summary>
        /// 指定のキャラクターが攻撃を行い得る位置(まだ行動していない味方が空けた場合を含む)を求めます
        /// </summary>
        private HashSet<int> CollectPotentialStandTiles( Character chara )
        {
            var standTiles = new HashSet<int>();
            foreach( var option in _collector.Collect( chara, _candidateKeys ) )
            {
                standTiles.Add( option.StandTileIndex );
            }

            return standTiles;
        }

        /// <summary>
        /// 攻撃を行い得る位置のうち、味方が空けるのを待たずに現時点で立つことが出来る位置の数を求めます。
        /// (味方が空けた場合を含めて列挙した結果から、現在他のキャラクターが立っている位置を除いたものであり、
        ///  味方が空けた場合を考慮せずに列挙した場合と同じ結果になります)
        /// </summary>
        private int CountCurrentlyStandableTiles( Character chara, HashSet<int> potentialStandTiles )
        {
            var key     = chara.GetCharacterKey();
            int count   = 0;
            foreach( int tileIndex in potentialStandTiles )
            {
                if( _stageCtrl.GetTileDynamicData( tileIndex ).IsStandableBy( key ) ) { ++count; }
            }

            return count;
        }

        /// <summary>
        /// 指定のキャラクター以外の調整の対象が、攻撃を行う位置として必要としているタイルと、その必要度を求めます。
        /// まだ行動していない味方(指定のキャラクター自身も含む)が立っているタイルは、移動して空くものとみなします。
        /// 必要度は、そのタイルを必要としている味方ごとに「1 / その味方が攻撃を行える位置の数」を合計したものです
        /// (攻撃を行える位置がそのタイルしかない味方ほど、大きく寄与します)
        /// </summary>
        /// <param name="actor">行動するキャラクター</param>
        /// <returns>タイルのインデックスと、そのタイルの必要度</returns>
        private Dictionary<int, float> BuildContestedTiles( Character actor )
        {
            var weights = new Dictionary<int, float>();

            foreach( var other in _candidates )
            {
                if( other == actor ) { continue; }

                // SelectNextActorで求め済みであれば使い回す
                if( !_potentialStandTiles.TryGetValue( other, out var standTiles ) )
                {
                    standTiles = CollectPotentialStandTiles( other );
                    _potentialStandTiles[other] = standTiles;
                }
                if( standTiles.Count <= 0 ) { continue; }

                float weight = 1f / standTiles.Count;
                foreach( int tileIndex in standTiles )
                {
                    weights.TryGetValue( tileIndex, out float current );
                    weights[tileIndex] = current + weight;
                }
            }

            return weights;
        }

    }
}
