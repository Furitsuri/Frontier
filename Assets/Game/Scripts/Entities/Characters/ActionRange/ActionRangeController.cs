using Frontier.Entities;
using Frontier.Combat;
using Frontier.Stage;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Unity.Burst.Intrinsics.X86.Avx;
using Cysharp.Threading.Tasks;

namespace Frontier.Entities
{
    public class ActionRangeController
    {
        private StageController _stageCtrl                  = null;
        private Character _owner                            = null;
        private MovePathHandler _movePathHandler            = null;
        private ActionableTileData _actionableTileData      = null;
        private ActionableRangeRenderer _actionableRangeRdr = null;

        private Action<int, int, Character, ActionableTileData>[] RefreshTargetingRangeCallbacks;

        public MovePathHandler MovePathHdlr => _movePathHandler;
        public ActionableTileData ActionableTileData => _actionableTileData;
        public ActionableRangeRenderer ActionableRangeRdr => _actionableRangeRdr;

        [Inject] public ActionRangeController( HierarchyBuilderBase hierarchyBld, StageController stageCtrl )
        {
            _stageCtrl      = stageCtrl;

            LazyInject.GetOrCreate( ref _actionableTileData, () => hierarchyBld.InstantiateWithDiContainer<ActionableTileData>( false ) );
            LazyInject.GetOrCreate( ref _movePathHandler, () => hierarchyBld.InstantiateWithDiContainer<MovePathHandler>( false ) );
            LazyInject.GetOrCreate( ref _actionableRangeRdr, () => hierarchyBld.InstantiateWithDiContainer<ActionableRangeRenderer>( false ) );

            RefreshTargetingRangeCallbacks = new Action<int, int, Character, ActionableTileData>[( int ) TargetingMode.NUM]
            {
                RefreshTargetingWithNormalAttack,   // TargetingMode.NORMAL_ATTACK
                RefreshTargetingWithPartOfRange,    // TargetingMode.PART_OF_RANGE
                RefreshTargetingWithDirectional,    // TargetingMode.DIRECTIONAL
                RefreshTargetingWithAll             // TargetingMode.ALL
            };
        }

        public void Init( Character owner )
        {
            _owner = owner;

            _actionableTileData.Init();
            _movePathHandler.Init( owner );
            _actionableRangeRdr.Init( owner, _actionableTileData );
        }

        public void Dispose()
        {
            _actionableRangeRdr.Dispose();
            _actionableTileData.Dispose();
            _movePathHandler.Dispose();

            _owner                      = null;
            _actionableRangeRdr         = null;
            _actionableTileData         = null;
            _movePathHandler            = null;
        }
        public void SetupActionableRangeData( int dprtTileIdx, float dprtTileHeight )
        {
            _actionableTileData.Init();

            var charaParam  = _owner.GetStatusRef;
            var mvRng       = charaParam.moveRange;
            var jmp         = charaParam.jumpForce;
            var atkRng      = !_owner.BattleParams.TmpParam.IsEndCommand[( int ) COMMAND_TAG.ATTACK] ? _owner.GetStatusRef.attackRange : 0;

            _stageCtrl.TileDataHdlr().ExtractActionableRangeData( dprtTileIdx, mvRng, jmp, atkRng, dprtTileHeight, _owner.BattleLogic.TileCostTable, _owner.GetCharacterKey(), ref _actionableTileData );
        }

        /// <summary>
        /// 通常攻撃用に攻撃可能な範囲を設定します
        /// </summary>
        /// <param name="dprtTileIdx"></param>
        public void SetupAttackableRangeData( int dprtTileIdx )
        {
            _actionableTileData.Init();

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
            SkillsData.Data skillData = SkillsData.data[( int ) useSkillID];
            if( !SkillsData.IsTransitionSkillActionType( skillData.ActionType ) )
            {
                Debug.LogError( "攻撃用のスキル以外のスキルが指定されています。" );

                return;
            }

            // スキルの効果範囲に負の値が設定されている場合は、キャラクターの攻撃レンジをそのまま用いる
            var atkRng = ( skillData.RangeValue < 0 ) ? _owner.GetStatusRef.attackRange : skillData.RangeValue;

            _actionableTileData.Init();
            _stageCtrl.TileDataHdlr().ExtractAttackableData( dprtTileIdx, atkRng, skillData.RangeShape, _owner.GetCharacterKey(), ref _actionableTileData );
        }

        public void ClearActionableRangeData()
        {
            _actionableTileData.Init();
        }

        public void ClearActionableRangeDataWithRender()
        {
            _actionableRangeRdr.ClearTileMeshes();

            ClearActionableRangeData();
        }

        public void RefreshTargetingRange( TargetingMode targetingMode, int tileIndex, int targetingValue )
        {
            if( _actionableTileData.IsEmpty() )
            {
                Debug.LogError("アクション可能範囲が設定されていない状態で対象範囲指定の処理が呼び出されています。");
                return;
            }

            // 描画されている攻撃範囲のメッシュを一度全てクリアする
            _actionableRangeRdr.ClearTileMeshes();
            // ターゲット対象タイルのインデックスを一度クリアする
            _actionableTileData.ClearAttackTargetTileIndicies();
            // ターゲット可能タイルのインデックスを一度クリアする
            _actionableTileData.ClearTargetableTile();
            // ターゲットモードに合ったコールバックを呼び出す
            RefreshTargetingRangeCallbacks[( int ) targetingMode]( tileIndex, targetingValue, _owner, _actionableTileData );
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
            // ActionableTileDataが空の場合はこのタイミングでセットアップを行う
            if( _actionableTileData.IsEmpty() )
            {
                int dprtTileIndex   = _owner.BattleParams.TmpParam.CurrentTileIndex;
                var data            =_stageCtrl.GetTileStaticData( dprtTileIndex );
                SetupActionableRangeData( dprtTileIndex, data.Height );
            }

            _actionableRangeRdr.SetDisplayDangerRange( !_actionableRangeRdr.IsShowingAttackableRange, color );
        }

        public void SetDisplayDangerRange( bool isShow, in Color color )
        {
            // ActionableTileDataが空の場合はこのタイミングでセットアップを行う
            if( _actionableTileData.IsEmpty() )
            {
                int dprtTileIndex   = _owner.BattleParams.TmpParam.CurrentTileIndex;
                var data            =_stageCtrl.GetTileStaticData( dprtTileIndex );
                SetupActionableRangeData( dprtTileIndex, data.Height );
            }

            _actionableRangeRdr.SetDisplayDangerRange( isShow, color );
        }

        public void DrawMoveableRange()
        {
            _actionableRangeRdr.DrawMoveableRange( tileData => new (MeshType, bool)[]
            {
                ( MeshType.REACHABLE_ATTACK,        Methods.HasAnyFlag(tileData.Flag, TileBitFlag.REACHABLE_ATTACK) ),
                ( MeshType.MOVE,                    0 <= tileData.EstimatedMoveRange ),
            } );
        }

        public void DrawAttackableRange()
        {
            // メッシュタイプとそれに対応する描画条件( MEMO : 描画優先度の高い順に並べること )
            _actionableRangeRdr.DrawAttackableRange( tileData => new (MeshType, bool)[]
            {
                ( MeshType.TARGETABLE,              Methods.HasAnyFlag(tileData.Flag, TileBitFlag.TARGETABLE) ),
                ( MeshType.ATTACKABLE_TARGET_EXIST, Methods.HasAnyFlag(tileData.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST) && tileData.EstimatedMoveRange < 0 ),
                ( MeshType.ATTACKABLE,              Methods.HasAnyFlag(tileData.Flag, TileBitFlag.ATTACKABLE) && tileData.EstimatedMoveRange < 0 ),
            } );
        }

        public void ReDrawAttackableRange()
        {
            _actionableRangeRdr.ClearTileMeshes();
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
                var tileData = _actionableTileData.GetAttackableTile( _actionableTileData.RefAttackTargetTileIndicies[i] );
                if( tileData != null && tileData.CharaKey.IsValid() )
                {
                    targetKeys.Add( tileData.CharaKey );
                }
            }
            return targetKeys;
        }

        private void RefreshTargetingWithNormalAttack( int tileIndex, int targetingValue, Character owner, ActionableTileData actionableTileMap )
        {
            Direction ownerDir = owner.GetTransformHandler.GetDirection();

            foreach( var attackableMap in actionableTileMap.AttackableTileMap )
            {
                if( Methods.HasAnyFlag( attackableMap.Value.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST ) )
                {
                    actionableTileMap.AddAttackTargetTileIndex( attackableMap.Key );
                }
            }
        }

        private void RefreshTargetingWithPartOfRange( int tileIndex, int targetingValue, Character owner, ActionableTileData actionableTileMap )
        {
            // ターゲット指定の値に応じた範囲のターゲット可能タイルを、ターゲット可能タイルとして設定する
            _stageCtrl.TileDataHdlr().BeginExpandTargetableTilesWithPartOfRange( tileIndex, targetingValue, owner.GetCharacterTag(), actionableTileMap );
        }

        /// <summary>
        /// 指定のキャラクターの向きに沿った位置の攻撃可能タイルを、ターゲット可能タイルとして設定します
        /// </summary>
        /// <param name="tileIndex"></param>
        /// <param name="TargetingValue"></param>
        /// <param name="owner"></param>
        /// <param name="actionableTileData"></param>
        private void RefreshTargetingWithDirectional( int tileIndex, int targetingValue, Character owner, ActionableTileData actionableTileData )
        {
            Vector3 basePos     = _stageCtrl.GetTileStaticData( owner.BattleParams.TmpParam.CurrentTileIndex ).CharaStandPos;
            Vector3 baseForward = owner.GetTransformHandler.GetOrderedForward();
            baseForward.y       = 0f;
            baseForward         = baseForward.normalized;

            foreach( var attackableMap in actionableTileData.AttackableTileMap )
            {
                // 一度全てのTARGETABLEフラグのビットを降ろす
                Methods.UnsetBitFlag( ref attackableMap.Value.Flag, TileBitFlag.TARGETABLE );

                var targetTilePos = _stageCtrl.GetTileStaticData( attackableMap.Key ).CharaStandPos;

                if( Methods.IsMatchForward( baseForward, basePos, targetTilePos ) )
                {
                    // 攻撃可能タイルの中で、キャラクターの向きに沿った位置にあるタイルをターゲット可能タイルとして登録する
                    actionableTileData.AddTargetableTile( attackableMap.Key, attackableMap.Value );
                    // さらに、そのタイルに攻撃対象のキャラクターが存在する場合は、攻撃対象タイルインデックスリストに追加する
                    if( IsExistOpponent( attackableMap.Value, owner.GetCharacterTag() ) )
                    {
                        actionableTileData.AddAttackTargetTileIndex( attackableMap.Key );
                    }
                }
            }
        }

        /// <summary>
        /// 現在の攻撃可能タイルをすべてターゲット可能タイルとして設定します
        /// </summary>
        /// <param name="tileIndex"></param>
        /// <param name="TargetingValue"></param>
        /// <param name="owner"></param>
        /// <param name="actionableTileMap"></param>
        private void RefreshTargetingWithAll( int tileIndex, int targetingValue, Character owner, ActionableTileData actionableTileMap )
        {
            foreach( var attackableMap in actionableTileMap.AttackableTileMap )
            {
                Methods.SetBitFlag( ref attackableMap.Value.Flag, TileBitFlag.TARGETABLE );

                if( IsExistOpponent( attackableMap.Value, owner.GetCharacterTag() ) )
                {
                }
            }
        }

        private bool IsExistOpponent( TileDynamicData tileDynamicData, CHARACTER_TAG ownerTag )
        {
            return tileDynamicData.CharaKey.IsValid() && BattleLogicBase.IsOpponentFaction[( int ) ownerTag]( tileDynamicData.CharaKey.CharacterTag );
        }
    }
}