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
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private StageController _stageCtrl         = null;

        private Character _owner                            = null;
        private ActionableTileMap _actionableTileMap        = null;
        private MovePathHandler _movePathHandler            = null;
        private ActionableRangeRenderer _actionableRangeRdr = null;

        private Func<int, int, Character, ActionableTileMap, List<CharacterKey>>[] RefreshTargetingRangeCallbacks;

        public ActionableTileMap ActionableTileMap => _actionableTileMap;
        public MovePathHandler MovePathHdlr => _movePathHandler;
        public ActionableRangeRenderer ActionableRangeRdr => _actionableRangeRdr;

        public void Init( Character owner )
        {
            _owner = owner;

            LazyInject.GetOrCreate( ref _actionableTileMap, () => _hierarchyBld.InstantiateWithDiContainer<ActionableTileMap>( false ) );
            LazyInject.GetOrCreate( ref _movePathHandler, () => _hierarchyBld.InstantiateWithDiContainer<MovePathHandler>( false ) );
            LazyInject.GetOrCreate( ref _actionableRangeRdr, () => _hierarchyBld.InstantiateWithDiContainer<ActionableRangeRenderer>( false ) );

            RefreshTargetingRangeCallbacks = new Func<int, int, Character, ActionableTileMap, List<CharacterKey>>[( int ) TargetingMode.NUM]
            {
                RefreshTargetingWithCenter,
                RefreshTargetingWithDirectional,
                RefreshTargetingWithAll
            };

            _actionableTileMap.Init();
            _movePathHandler.Init( owner );
            _actionableRangeRdr.Init( owner, _actionableTileMap );
        }

        public void Dispose()
        {
            _owner = null;
            _actionableTileMap.Dispose();
            _actionableTileMap = null;
            _movePathHandler.Dispose();
            _movePathHandler = null;
            _actionableRangeRdr.Dispose();
            _actionableRangeRdr = null;
        }
        public void SetupActionableRangeData( int dprtTileIdx, float dprtTileHeight )
        {
            _actionableTileMap.Init();

            var charaParam  = _owner.GetStatusRef;
            var mvRng       = charaParam.moveRange;
            var jmp         = charaParam.jumpForce;
            var atkRng      = !_owner.BattleParams.TmpParam.IsEndCommand[( int ) COMMAND_TAG.ATTACK] ? _owner.GetStatusRef.attackRange : 0;

            _stageCtrl.TileDataHdlr().ExtractActionableRangeData( dprtTileIdx, mvRng, jmp, atkRng, dprtTileHeight, _owner.BattleLogic.TileCostTable, _owner.GetCharacterKey(), ref _actionableTileMap );
        }

        /// <summary>
        /// 通常攻撃用に攻撃可能な範囲を設定します
        /// </summary>
        /// <param name="dprtTileIdx"></param>
        public void SetupAttackableRangeData( int dprtTileIdx )
        {
            _actionableTileMap.Init();

            var atkRng = !_owner.BattleParams.TmpParam.IsEndCommand[( int ) COMMAND_TAG.ATTACK] ? _owner.GetStatusRef.attackRange : 0;

            _stageCtrl.TileDataHdlr().ExtractAttackableData( dprtTileIdx, atkRng, RangeShape.FROM_MYSELF, _owner.GetCharacterKey(), ref _actionableTileMap );
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

            _actionableTileMap.Init();
            _stageCtrl.TileDataHdlr().ExtractAttackableData( dprtTileIdx, atkRng, skillData.RangeShape, _owner.GetCharacterKey(), ref _actionableTileMap );
        }

        public void ClearActionableRangeData()
        {
            _actionableTileMap.Init();
        }

        public void ClearActionableRangeDataWithRender()
        {
            _actionableRangeRdr.ClearTileMeshes();

            ClearActionableRangeData();
        }

        public List<CharacterKey> RefreshTargetingRange( TargetingMode targetingMode, int tileIndex, int targetingValue )
        {
            if( _actionableTileMap.IsEmpty() )
            {
                Debug.LogError("アクション可能範囲が設定されていない状態で対象範囲指定の処理が呼び出されています。");
                return null;
            }

            // 一度全てのTARGETABLEフラグのビットを降ろす
            foreach( var attackableMap in _actionableTileMap.AttackableTileMap )
            {
                Methods.UnsetBitFlag( ref attackableMap.Value.Flag, TileBitFlag.TARGETABLE );
            }

            return RefreshTargetingRangeCallbacks[( int ) targetingMode]( tileIndex, targetingValue, _owner, _actionableTileMap );
        }

        /// <summary>
        /// 条件を指定して移動可能範囲を設定します
        /// </summary>
        /// <param name="setup"></param>
        /// <param name="condition"></param>
        /// <param name="args"></param>
        public void SetupMoveableRangeDataFilterByCondition( Func<TileDynamicData[]> setup, Func<TileDynamicData, object[], bool> condition, params object[] args )
        {
            _actionableTileMap.Init();

            _stageCtrl.TileDataHdlr().ExtractMoveableRangeDataFilterByCondition( ref _actionableTileMap, setup, condition, args );
        }

        public void ToggleDisplayDangerRange( in Color color )
        {
            // ActionableTileMapが空の場合はこのタイミングでセットアップを行う
            if( _actionableTileMap.IsEmpty() )
            {
                int dprtTileIndex   = _owner.BattleParams.TmpParam.CurrentTileIndex;
                var data            =_stageCtrl.GetTileStaticData( dprtTileIndex );
                SetupActionableRangeData( dprtTileIndex, data.Height );
            }

            _actionableRangeRdr.SetDisplayDangerRange( !_actionableRangeRdr.IsShowingAttackableRange, color );
        }

        public void SetDisplayDangerRange( bool isShow, in Color color )
        {
            // ActionableTileMapが空の場合はこのタイミングでセットアップを行う
            if( _actionableTileMap.IsEmpty() )
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
            Debug.Assert( null != _actionableTileMap, "_actionableTileMapのセットアップが完了しているか確認してください。" );

            return _movePathHandler.FindMovePath( dprtTileIndex, destTileIndex, ownerJumpForce, ownerTileCosts, _actionableTileMap.MoveableTileMap );
        }

        public bool FindActuallyMovePath( int departingTileIndex, int destinationTileIndex, int ownerJumpForce, int[] ownerTileCosts, bool isEndPathTrace )
        {
            Debug.Assert( null != _actionableTileMap, "_actionableTileMapのセットアップが完了しているか確認してください。" );

            return _movePathHandler.FindActuallyMovePath( departingTileIndex, destinationTileIndex, ownerJumpForce, ownerTileCosts, isEndPathTrace, _actionableTileMap );
        }

        private List<CharacterKey> RefreshTargetingWithCenter( int tileIndex, int TargetingValue, Character owner, ActionableTileMap actionableTileMap )
        {
            List<CharacterKey> retTargetingCharaKeys = new List<CharacterKey>();

            return retTargetingCharaKeys;
        }

        /// <summary>
        /// 指定のキャラクターの向きに沿った位置の攻撃可能タイルを、ターゲット可能タイルとして設定します
        /// </summary>
        /// <param name="tileIndex"></param>
        /// <param name="TargetingValue"></param>
        /// <param name="owner"></param>
        /// <param name="actionableTileMap"></param>
        private List<CharacterKey> RefreshTargetingWithDirectional( int tileIndex, int TargetingValue, Character owner, ActionableTileMap actionableTileMap )
        {
            List<CharacterKey> retTargetingCharaKeys = new List<CharacterKey>();

            Direction ownerDir = owner.GetTransformHandler.GetDirection();

            foreach( var attackableMap in actionableTileMap.AttackableTileMap )
            {
                if( ownerDir == _stageCtrl.TileDataHdlr().GetDirectionBetweenTiles( owner.BattleParams.TmpParam.CurrentTileIndex, attackableMap.Key ) )
                {
                    Methods.SetBitFlag( ref attackableMap.Value.Flag, TileBitFlag.TARGETABLE );

                    if( IsOpponentExist( attackableMap.Value, owner.GetCharacterTag() ) )
                    {
                        retTargetingCharaKeys.Add( attackableMap.Value.CharaKey );
                    }
                }
            }

            return retTargetingCharaKeys;
        }

        /// <summary>
        /// 現在の攻撃可能タイルをすべてターゲット可能タイルとして設定します
        /// </summary>
        /// <param name="tileIndex"></param>
        /// <param name="TargetingValue"></param>
        /// <param name="owner"></param>
        /// <param name="actionableTileMap"></param>
        private List<CharacterKey> RefreshTargetingWithAll( int tileIndex, int TargetingValue, Character owner, ActionableTileMap actionableTileMap )
        {
            List<CharacterKey> retTargetingCharaKeys = new List<CharacterKey>();

            foreach( var attackableMap in actionableTileMap.AttackableTileMap )
            {
                Methods.SetBitFlag( ref attackableMap.Value.Flag, TileBitFlag.TARGETABLE );

                if( IsOpponentExist( attackableMap.Value, owner.GetCharacterTag() ) )
                {
                    retTargetingCharaKeys.Add( attackableMap.Value.CharaKey );
                }
            }

            return retTargetingCharaKeys;
        }

        private bool IsOpponentExist( TileDynamicData tileDData, CHARACTER_TAG ownerTag )
        {
            return tileDData.CharaKey.IsValid() && BattleLogicBase.IsOpponentFaction[( int ) ownerTag]( tileDData.CharaKey.CharacterTag );
        }
    }
}