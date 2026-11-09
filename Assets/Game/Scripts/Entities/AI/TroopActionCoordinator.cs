using System.Collections.Generic;
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
    ///   2. 攻撃出来るキャラクターのうち、攻撃を行える位置の数が最も少ないキャラクター
    ///      (立てる位置が限られるキャラクターに先に位置を取らせ、融通の利くキャラクターが後から合わせるため)
    ///   3. 攻撃出来るキャラクターがいない場合は、手番の順で最初のキャラクター(近づく等の行動はAIが決定する)
    ///   いずれも等しい場合は、手番の順(並び順)で先のキャラクターを優先します。
    /// </summary>
    public class TroopActionCoordinator
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        private AttackOptionCollector _collector = null;

        // 調整の対象(このフェーズで行動し得る、行動を終えていないキャラクター)と、その識別子
        private readonly List<Character> _candidates            = new List<Character>();
        private readonly HashSet<CharacterKey> _candidateKeys   = new HashSet<CharacterKey>();

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

            CollectCandidates( troop );

            _selectedActor          = SelectActorFromCandidates();
            _selectedContestedTiles = ( null != _selectedActor ) ? BuildContestedTiles( _selectedActor ) : null;

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
            if( !ai.IsDetermined() )
            {
                // SelectNextActorで選んだ直後であれば、その時点で求めたものを使う
                ai.SetContestedTiles( ( actor == _selectedActor ) ? _selectedContestedTiles : BuildContestedTiles( actor ) );
            }

            return enemyLogic.PrepareActionWithAI();
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
        /// 調整の対象の中から、次に行動するキャラクターを選びます
        /// </summary>
        private Character SelectActorFromCandidates()
        {
            if( _candidates.Count <= 0 ) { return null; }

            // 行動の途中(行動を決定済みで、まだ行動を終えていない)のキャラクターがいれば、その行動を続けさせる
            foreach( var chara in _candidates )
            {
                if( chara.BattleLogic.GetAi().IsDetermined() ) { return chara; }
            }

            // 攻撃出来るキャラクターのうち、攻撃を行える位置の数が最も少ないキャラクターを選ぶ
            Character bestAttacker  = null;
            int minStandTileNum     = int.MaxValue;
            foreach( var chara in _candidates )
            {
                int standTileNum = CountStandTiles( _collector.Collect( chara ) );
                if( 0 < standTileNum && standTileNum < minStandTileNum )
                {
                    minStandTileNum = standTileNum;
                    bestAttacker    = chara;
                }
            }

            // 攻撃出来るキャラクターがいない場合は、手番の順で最初のキャラクター
            return bestAttacker ?? _candidates[0];
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
            var weights     = new Dictionary<int, float>();
            var standTiles  = new HashSet<int>();

            foreach( var other in _candidates )
            {
                if( other == actor ) { continue; }

                standTiles.Clear();
                foreach( var option in _collector.Collect( other, _candidateKeys ) )
                {
                    standTiles.Add( option.StandTileIndex );
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

        /// <summary>
        /// 攻撃の候補から、攻撃を行う位置の数を求めます
        /// </summary>
        private static int CountStandTiles( List<AttackOption> options )
        {
            var standTiles = new HashSet<int>();
            foreach( var option in options )
            {
                standTiles.Add( option.StandTileIndex );
            }

            return standTiles.Count;
        }
    }
}
