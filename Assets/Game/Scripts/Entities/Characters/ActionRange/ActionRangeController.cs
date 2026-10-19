using Frontier.Battle;
using Frontier.Combat;
using Frontier.Stage;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier.Entities
{
    public class ActionRangeController
    {
        private BattleRoutineController  _btlRtnCtrl              = null;
        private StageController          _stageCtrl               = null;
        private Character                _owner                   = null;
        private MovePathHandler          _movePathHandler         = null;
        private ActionableTileData       _actionableTileData      = null;
        private ActionableRangeRenderer  _actionableRangeRdr     = null;
        private MoveDirectionArrowPlacer _moveDirectionArrowPlacer = null;
        private SkillRangeRules          _skillRangeRules         = SkillRangeRules.Default;
        private int                      _skillMaxRange           = 0;
        private Action<TargetingRangeContext, bool, bool, int, int, ActionableTileData>[] RefreshTargetableRangeCallbacks;

        /// <summary>
        /// 直近にSetupAttackableRangeData(int,SkillID)で設定したスキルの最大レンジ
        /// (移動を伴うスキルでは着地可能な最大距離。攻撃対象として含める先のタイル数は含まない)
        /// </summary>
        public int SkillMaxRange => _skillMaxRange;
        public MovePathHandler MovePathHdlr => _movePathHandler;
        public ActionableTileData ActionableTileData => _actionableTileData;
        public ActionableRangeRenderer ActionableRangeRdr => _actionableRangeRdr;

        [Inject] public ActionRangeController( HierarchyBuilderBase hierarchyBld, BattleRoutineController btlRtnCtrl, StageController stageCtrl )
        {
            _btlRtnCtrl = btlRtnCtrl;
            _stageCtrl  = stageCtrl;

            LazyInject.GetOrCreate( ref _actionableTileData,       () => hierarchyBld.InstantiateWithDiContainer<ActionableTileData>( false ) );
            LazyInject.GetOrCreate( ref _movePathHandler,          () => hierarchyBld.InstantiateWithDiContainer<MovePathHandler>( false ) );
            LazyInject.GetOrCreate( ref _actionableRangeRdr,       () => hierarchyBld.InstantiateWithDiContainer<ActionableRangeRenderer>( false ) );
            LazyInject.GetOrCreate( ref _moveDirectionArrowPlacer, () => hierarchyBld.InstantiateWithDiContainer<MoveDirectionArrowPlacer>( false ) );

            RefreshTargetableRangeCallbacks = new Action<TargetingRangeContext, bool, bool, int, int, ActionableTileData>[( int ) TargetingMode.NUM]
            {
                NormalAttackTargetingRange.RefreshTargetableRange,    // TargetingMode.NORMAL_ATTACK
                PartOfRangeTargetingRange.RefreshTargetableRange,     // TargetingMode.PART_OF_RANGE
                DirectionalTargetingRange.RefreshTargetableRange,     // TargetingMode.DIRECTIONAL
                AllTargetingRange.RefreshTargetableRange,             // TargetingMode.ALL
            };
        }

        public void Init( Character owner )
        {
            _owner = owner;

            _actionableTileData.Init();
            _movePathHandler.Init( owner );
            _actionableRangeRdr.Init( owner, _actionableTileData );
            _moveDirectionArrowPlacer.Init( owner.GetCharacterKey() );
        }

        public void Dispose()
        {
            _actionableRangeRdr.Dispose();
            _actionableTileData.Dispose();
            _movePathHandler.Dispose();
            _moveDirectionArrowPlacer.ClearArrows();

            _owner                    = null;
            _actionableRangeRdr       = null;
            _actionableTileData       = null;
            _movePathHandler          = null;
            _moveDirectionArrowPlacer = null;
        }
        public void SetupActionableRangeData( int dprtTileIdx, float dprtTileHeight )
        {
            ExtractActionableRangeData( dprtTileIdx, dprtTileHeight, ref _actionableTileData );
        }

        /// <summary>
        /// 移動可能範囲、及びそこから攻撃可能な範囲を、渡されたActionableTileDataへ計算します
        /// (SetupActionableRangeDataと危険範囲表示の共通処理)
        /// </summary>
        private void ExtractActionableRangeData( int dprtTileIdx, float dprtTileHeight, ref ActionableTileData targetTileData )
        {
            targetTileData.Init();

            var charaParam  = _owner.GetStatusRef;
            var mvRng       = charaParam.moveRange;
            var jmp         = charaParam.jumpForce;
            var atkRng      = !_owner.BattleParams.TmpParam.IsEndCommand[( int ) COMMAND_TAG.ATTACK] ? _owner.GetStatusRef.attackRange : 0;

            _stageCtrl.TileDataHdlr().ExtractActionableRangeData( dprtTileIdx, mvRng, jmp, atkRng, dprtTileHeight, _owner.BattleLogic.TileCostTable, _owner.GetCharacterKey(), ref targetTileData );
        }

        /// <summary>
        /// 通常攻撃用に攻撃可能な範囲を設定します
        /// </summary>
        /// <param name="dprtTileIdx"></param>
        public void SetupAttackableRangeData( int dprtTileIdx )
        {
            _actionableTileData.Init();
            _skillRangeRules = SkillRangeRules.Default;

            var atkRng = !_owner.BattleParams.TmpParam.IsEndCommand[( int ) COMMAND_TAG.ATTACK] ? _owner.GetStatusRef.attackRange : 0;

            _stageCtrl.TileDataHdlr().ExtractAttackableData( dprtTileIdx, atkRng, RangeShape.FROM_MYSELF, _owner.GetCharacterKey(), ref _actionableTileData );
        }

        /// <summary>
        /// スキルによる攻撃用に攻撃可能な範囲を設定します
        /// </summary>
        /// <param name="dprtTileIdx"></param>
        /// <param name="useSkillID"></param>
        public void SetupAttackableRangeData( int dprtTileIdx, SkillID useSkillID )
        {
            if( !SkillsData.IsTransitionSkillActionType( SkillsData.data[( int ) useSkillID].ActionType ) )
            {
                Debug.LogError( "攻撃用のスキル以外のスキルが指定されています。" );

                return;
            }

            _skillRangeRules = SkillRangeRules.Get( useSkillID, _stageCtrl, _owner );
            _skillMaxRange   = ResolveSkillMaxRange( dprtTileIdx, useSkillID, _skillRangeRules );

            ExtractAttackableRangeDataForSkill( dprtTileIdx, useSkillID, _skillRangeRules, ref _actionableTileData );
        }

        /// <summary>
        /// 表示用のActionableTileData(及びゴースト表示関連のフィールド)には影響を与えず、
        /// 指定スキルが実行可能かどうかのみを判定します。
        /// 移動を伴うスキルは、攻撃対象の有無によらず着地可能なタイルが範囲内に存在すれば実行可能とします。
        /// それ以外のスキルは、攻撃範囲内に攻撃対象が存在する場合のみ実行可能とします。
        /// スキル選択画面などで、範囲プレビューを崩さずに使用可否を判定したい場合に使用します。
        /// </summary>
        public bool IsSkillExecutableAt( int dprtTileIdx, SkillID useSkillID )
        {
            var skillData = SkillsData.data[( int ) useSkillID];
            if( !SkillsData.IsTransitionSkillActionType( skillData.ActionType ) ) { return false; }

            var rules        = SkillRangeRules.Get( useSkillID, _stageCtrl, _owner );
            var tempTileData = new ActionableTileData();
            ExtractAttackableRangeDataForSkill( dprtTileIdx, useSkillID, rules, ref tempTileData );

            if( skillData.IsMovingSkill )
            {
                // 対象選択時と同じ着地先の決定処理を用いて、いずれかの方向に着地先が存在するかを判定する
                return DirectionalTargetingRange.HasLandingTileInAnyDirection( dprtTileIdx, tempTileData, _stageCtrl, _owner.GetCharacterKey(), rules.GhostResolver );
            }

            foreach( var data in tempTileData.AttackableTileMap )
            {
                if( Methods.HasAnyFlag( data.Value.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST ) ) { return true; }
            }

            return false;
        }

        /// <summary>
        /// 指定スキルの攻撃可能範囲を、渡されたActionableTileDataへ計算します
        /// (SetupAttackableRangeData(int,SkillID)とIsSkillExecutableAtの共通処理)
        /// </summary>
        private void ExtractAttackableRangeDataForSkill( int dprtTileIdx, SkillID useSkillID, SkillRangeRules rules, ref ActionableTileData targetTileData )
        {
            SkillsData.Data skillData = SkillsData.data[( int ) useSkillID];

            // 着地タイルの先の相手を攻撃するスキル等のため、最大レンジに攻撃対象として含める分のタイル数を加える
            var atkRng = ResolveSkillMaxRange( dprtTileIdx, useSkillID, rules ) + rules.TargetableRangeMargin;

            targetTileData.Init();
            _stageCtrl.TileDataHdlr().ExtractAttackableData( dprtTileIdx, atkRng, skillData.RangeShape, _owner.GetCharacterKey(), ref targetTileData, rules.PostProcessor, rules.Reachability );
        }

        /// <summary>
        /// 指定スキルの最大レンジを求めます。
        /// スキル固有の求め方が無い場合、スキルの効果範囲の値を用います(負の値の場合はキャラクターの攻撃レンジ)
        /// </summary>
        private int ResolveSkillMaxRange( int dprtTileIdx, SkillID useSkillID, SkillRangeRules rules )
        {
            if( rules.MaxRangeResolver != null ) { return rules.MaxRangeResolver( dprtTileIdx ); }

            SkillsData.Data skillData = SkillsData.data[( int ) useSkillID];
            return ( skillData.RangeValue < 0 ) ? _owner.GetStatusRef.attackRange : skillData.RangeValue;
        }

        public void ClearActionableRangeData()
        {
            _actionableTileData.Init();
        }

        /// <summary>
        /// 範囲データと、その描画を消去します
        /// </summary>
        /// <param name="isKeepDangerRange">危険範囲の表示を残すかどうか</param>
        public void ClearActionableRangeDataWithRender( bool isKeepDangerRange = false )
        {
            if( isKeepDangerRange )
            {
                _actionableRangeRdr.ClearTileMeshesByTypeKeepingDangerRange( TileMapType.MOVEABLE | TileMapType.ATTACKABLE | TileMapType.TARGETABLE | TileMapType.QUEUED );
            }
            else
            {
                _actionableRangeRdr.ClearTileMeshesAllType();
            }

            ClearActionableRangeData();
        }

        public void RefreshTargetableRange( TargetingMode targetingMode, bool isFirstRefresh, bool isWithMove, int tileIndex, int currentRange )
        {
            if( _actionableTileData.IsEmpty() )
            {
                Debug.LogError("アクション可能範囲が設定されていない状態で対象範囲指定の処理が呼び出されています。");
                return;
            }

            // 描画されている攻撃範囲のメッシュを一度全てクリアする
            _actionableRangeRdr.ClearTileMeshesAllType();
            // ターゲット対象タイルのインデックスを一度クリアする
            _actionableTileData.ClearAttackTargetTileIndicies();
            // ターゲット可能タイルのインデックスを一度クリアする
            _actionableTileData.ClearTargetableTile();
            // ターゲットモードに合ったコールバックを呼び出す
            var context = new TargetingRangeContext { BtlRtnCtrl = _btlRtnCtrl, Owner = _owner, StageCtrl = _stageCtrl, GhostResolver = _skillRangeRules.GhostResolver, RangeAdjustmentFilter = _skillRangeRules.RangeAdjustmentFilter, AttackTargetFilter = _skillRangeRules.AttackTargetFilter, TargetableRangeMargin = _skillRangeRules.TargetableRangeMargin };
            RefreshTargetableRangeCallbacks[( int ) targetingMode]( context, isFirstRefresh, isWithMove, tileIndex, currentRange, _actionableTileData );
            // 攻撃範囲再描画も併せて行う
            DrawAttackableRange();
        }

        /// <summary>
        /// 条件を指定して移動可能範囲を設定します
        /// </summary>
        /// <param name="setup"></param>
        /// <param name="condition"></param>
        /// <param name="args"></param>
        public void SetupMoveableRangeDataFilterByCondition( Func<TileDynamicData[]> setup, Func<TileDynamicData, object[], bool> condition, params object[] args )
        {
            _actionableTileData.Init();

            _stageCtrl.TileDataHdlr().ExtractMoveableRangeDataFilterByCondition( ref _actionableTileData, setup, condition, args );
        }

        public void ToggleDisplayDangerRange( in Color color )
        {
            SetDisplayDangerRange( !_actionableRangeRdr.IsShowingAttackableRange, color );
        }

        public void SetDisplayDangerRange( bool isShow, in Color color )
        {
            if( !isShow )
            {
                _actionableRangeRdr.HideDangerRange();
                return;
            }

            if( _actionableRangeRdr.IsShowingAttackableRange ) { return; }

            _actionableRangeRdr.ShowDangerRange( ExtractDangerTileIndices(), color );
        }

        /// <summary>
        /// 危険範囲(現在の位置から移動した上で攻撃出来るタイル)のタイル番号を求めます。
        /// カーソルを合わせている間の範囲表示等で用いる_actionableTileDataを書き換えないよう、一時的なデータへ計算します
        /// </summary>
        public ICollection<int> ExtractDangerTileIndices()
        {
            int dprtTileIndex   = _owner.BattleParams.TmpParam.CurrentTileIndex;
            var data            = _stageCtrl.GetTileStaticData( dprtTileIndex );
            var dangerTileData  = new ActionableTileData();
            ExtractActionableRangeData( dprtTileIndex, data.Height, ref dangerTileData );

            return dangerTileData.AttackableTileMap.Keys;
        }

        /// <summary>
        /// 危険範囲(現在の位置から移動した上で攻撃出来るタイル)に、攻撃対象が存在するかどうかを判定します。
        /// ExtractDangerTileIndicesと同様、_actionableTileDataは書き換えません
        /// </summary>
        public bool HasAttackTargetInDangerRange()
        {
            int dprtTileIndex   = _owner.BattleParams.TmpParam.CurrentTileIndex;
            var data            = _stageCtrl.GetTileStaticData( dprtTileIndex );
            var dangerTileData  = new ActionableTileData();
            ExtractActionableRangeData( dprtTileIndex, data.Height, ref dangerTileData );

            foreach( var tileData in dangerTileData.AttackableTileMap )
            {
                if( Methods.HasAnyFlag( tileData.Value.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST ) ) { return true; }
            }

            return false;
        }

        public void DrawMoveableRange()
        {
            _actionableRangeRdr.DrawRange( TileMapType.MOVEABLE, tileData => new (MeshType, bool)[]
            {
                ( MeshType.REACHABLE_ATTACK,        Methods.HasAnyFlag(tileData.Flag, TileBitFlag.REACHABLE_ATTACK) ),
                ( MeshType.MOVE,                    0 <= tileData.EstimatedMoveRange ),
            } );
        }

        /// <summary>
        /// 移動可能なタイルのみを描画します。DrawMoveableRangeと異なり、攻撃が到達可能な立ち位置(REACHABLE_ATTACK)も
        /// 移動可能タイルとして描画するため、攻撃に関する色が混ざりません(複数キャラクターの移動範囲を同時に示す場合など)
        /// </summary>
        public void DrawMoveOnlyRange()
        {
            _actionableRangeRdr.DrawRange( TileMapType.MOVEABLE, tileData => new (MeshType, bool)[]
            {
                ( MeshType.MOVE,                    0 <= tileData.EstimatedMoveRange ),
            } );
        }

        public void DrawAttackableRange()
        {
            // メッシュタイプとそれに対応する描画条件( MEMO : 描画優先度の高い順に並べること )
            _actionableRangeRdr.DrawRange( TileMapType.ATTACKABLE, tileData => new (MeshType, bool)[]
            {
                ( MeshType.ATTACKABLE_TARGET_EXIST, Methods.HasAnyFlag(tileData.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST) && tileData.EstimatedMoveRange < 0 ),
                ( MeshType.ATTACKABLE,              Methods.HasAnyFlag(tileData.Flag, TileBitFlag.ATTACKABLE) && tileData.EstimatedMoveRange < 0 )
            } );

            _actionableRangeRdr.DrawRange( TileMapType.TARGETABLE, _ => new (MeshType, bool)[]
            {
                ( MeshType.TARGETABLE, true ),
            } );
        }

        public void ReDrawAttackableRange()
        {
            _actionableRangeRdr.ClearTileMeshesAllType();
            DrawAttackableRange();
        }

        /// <summary>
        /// スキルキュー時の QUEUED 範囲を描画し、直線移動スキルの場合は進行方向矢印を配置します。
        /// skillDestinationTileIndex に -1 を渡すと矢印はクリアされます（移動なしスキル）。
        /// </summary>
        public void DrawTargetableRangeAsQueued( int skillDestinationTileIndex = -1 )
        {
            _actionableTileData.ClearTileMap( TileMapType.QUEUED );
            _actionableTileData.AddQueuedTile( _owner.BattleParams.TmpParam.CurrentTileIndex );

            foreach( var data in _actionableTileData.GetTileMap( TileMapType.TARGETABLE ) )
            {
                _actionableTileData.AddQueuedTile( data.Key );
            }

            _actionableRangeRdr.DrawRange( TileMapType.QUEUED, _ => new (MeshType, bool)[]
            {
                ( MeshType.QUEUED, true ),
            } );

            PlaceMoveDirectionArrows( _owner.BattleParams.TmpParam.CurrentTileIndex, skillDestinationTileIndex );
        }

        /// <summary>
        /// 直線移動スキル用。出発タイルから目的タイルまでの直線経路に進行方向矢印を配置します。
        /// destinationTileIndex が負の場合は矢印をクリアします。
        /// </summary>
        public void PlaceMoveDirectionArrows( int departureTileIndex, int destinationTileIndex )
        {
            if ( destinationTileIndex < 0 || destinationTileIndex == departureTileIndex )
            {
                _moveDirectionArrowPlacer.ClearArrows();
                return;
            }

            int colNum = _stageCtrl.GetGridNumsXZ().Item2;
            int delta  = destinationTileIndex - departureTileIndex;

            int step, dx, dz;
            if ( delta % colNum == 0 )
            {
                int sign = Math.Sign( delta / colNum );
                step = colNum * sign;
                dx = 0; dz = sign;
            }
            else
            {
                int sign = Math.Sign( delta );
                step = sign;
                dx = sign; dz = 0;
            }

            int tileCount = Math.Abs( delta / step );
            var rotation  = Quaternion.LookRotation( new Vector3( dx, 0f, dz ) );
            var entries   = new List<MoveDirectionArrowPlacer.Entry>( tileCount );

            for ( int i = 1; i < tileCount; ++i )
            {
                int  tileIndex = departureTileIndex + step * i;
                bool isLast    = ( i == tileCount - 1 );
                var  dirType   = isLast ? MoveDirectionType.ARROW_STRAIGHT : MoveDirectionType.ARROW_BODY;
                entries.Add( new MoveDirectionArrowPlacer.Entry( tileIndex, dirType, rotation ) );
            }

            _moveDirectionArrowPlacer.PlaceArrows( entries );
        }

        /// <summary>
        /// NPC移動用。ProposedMovePath に沿って各タイルに進行方向矢印を配置します。
        /// </summary>
        public void PlaceMoveDirectionArrows( int currentTileIndex, List<WaypointInformation> proposedMovePath )
        {
            if ( proposedMovePath == null || proposedMovePath.Count == 0 )
            {
                _moveDirectionArrowPlacer.ClearArrows();
                return;
            }

            _moveDirectionArrowPlacer.PlaceArrows( BuildPathArrowEntries( currentTileIndex, proposedMovePath, _stageCtrl.GetGridNumsXZ().Item2 ) );
        }

        /// <summary>
        /// 出発タイルと経路から、経路に沿った各タイルの進行方向矢印の配置情報を作成します。
        /// 出発タイル・最終タイル(目的地)には矢印を配置しません。
        /// </summary>
        /// <param name="currentTileIndex">出発タイル</param>
        /// <param name="path">出発タイルを含まない、目的地までの経路</param>
        /// <param name="colNum">グリッドの列数</param>
        public static List<MoveDirectionArrowPlacer.Entry> BuildPathArrowEntries( int currentTileIndex, IReadOnlyList<WaypointInformation> path, int colNum )
        {
            int pathCount = path.Count;
            var entries   = new List<MoveDirectionArrowPlacer.Entry>( pathCount );

            // 移動目的地となる最終タイルには矢印を配置しないため、pathCount - 1 までを走査する
            for ( int i = 0; i < pathCount - 1; ++i )
            {
                int prevTileIndex = ( i == 0 ) ? currentTileIndex : path[i - 1].TileIndex;
                int thisTileIndex = path[i].TileIndex;
                int nextTileIndex = path[i + 1].TileIndex;

                ( int inDx, int inDz )   = IndexDeltaToXZ( thisTileIndex - prevTileIndex, colNum );
                ( int outDx, int outDz ) = IndexDeltaToXZ( nextTileIndex - thisTileIndex, colNum );
                int cross                = inDx * outDz - inDz * outDx;

                // 目的地直前のタイル(次で目的地に到達するタイル)は先端に矢印がある種別、
                // それより前のタイルは胴体の種別を用いる
                bool isTileBeforeDestination = ( i == pathCount - 2 );

                MoveDirectionType dirType;
                Quaternion        rotation = Quaternion.LookRotation( new Vector3( outDx, 0f, outDz ) );

                if ( cross == 0 )
                {
                    dirType = isTileBeforeDestination ? MoveDirectionType.ARROW_STRAIGHT : MoveDirectionType.ARROW_BODY;
                }
                else if ( cross > 0 )
                {
                    dirType = isTileBeforeDestination ? MoveDirectionType.ARROW_TURN_LEFT : MoveDirectionType.ARROW_BODY_TURN_LEFT;
                }
                else
                {
                    dirType = isTileBeforeDestination ? MoveDirectionType.ARROW_TURN_RIGHT : MoveDirectionType.ARROW_BODY_TURN_RIGHT;
                }

                entries.Add( new MoveDirectionArrowPlacer.Entry( thisTileIndex, dirType, rotation ) );
            }

            return entries;
        }

        /// <summary>
        /// このキャラクターの移動方向矢印をすべて破棄します。
        /// </summary>
        public void ClearMoveDirectionArrows()
        {
            _moveDirectionArrowPlacer.ClearArrows();
        }

        /// <summary>
        /// ターゲット可能範囲(黄色)のみをクリアし、攻撃可能範囲(赤色)のみを再描画します。
        /// DIRECTIONALスキルのキャンセル時など、攻撃範囲を維持したままターゲット表示だけ消したい場合に使います。
        /// </summary>
        public void ClearTargetableAndReDrawAttackableRange()
        {
            _actionableTileData.ClearTargetableTile();
            _actionableTileData.ClearAttackTargetTileIndicies();
            DrawAttackableRange();
        }

        public void DrawActionableRange()
        {
            DrawMoveableRange();
            DrawAttackableRange();
        }

        public bool FindMovePath( int dprtTileIndex, int destTileIndex, int ownerJumpForce, in int[] ownerTileCosts )
        {
            Debug.Assert( null != _actionableTileData, "_actionableTileDataのセットアップが完了しているか確認してください。" );

            return _movePathHandler.FindMovePath( dprtTileIndex, destTileIndex, ownerJumpForce, ownerTileCosts, _actionableTileData.MoveableTileMap );
        }

        public bool FindActuallyMovePath( int departingTileIndex, int destinationTileIndex, int ownerJumpForce, int[] ownerTileCosts, bool isEndPathTrace )
        {
            Debug.Assert( null != _actionableTileData, "_actionableTileDataのセットアップが完了しているか確認してください。" );

            return _movePathHandler.FindActuallyMovePath( departingTileIndex, destinationTileIndex, ownerJumpForce, ownerTileCosts, isEndPathTrace, _actionableTileData );
        }

        public List<CharacterKey> GetAttackTargetCharacterKeys()
        {
            List<CharacterKey> targetKeys = new List<CharacterKey>( _actionableTileData.RefAttackTargetTileIndicies.Count );
            for( int i = 0; i < _actionableTileData.RefAttackTargetTileIndicies.Count; ++i )
            {
                var tileData = _stageCtrl.GetTileDynamicData( _actionableTileData.RefAttackTargetTileIndicies[i] );
                targetKeys.Add( tileData.CharaKey );
            }
            return targetKeys;
        }

        private static ( int dx, int dz ) IndexDeltaToXZ( int delta, int colNum )
        {
            if ( delta ==  colNum ) return ( 0,  1 );
            if ( delta == -colNum ) return ( 0, -1 );
            if ( delta ==  1      ) return ( 1,  0 );
            if ( delta == -1      ) return (-1,  0 );
            return ( 0, 0 );
        }

    }
}