using Frontier.Registries;
using Frontier.Stage;
using System;
using System.Collections.Generic;
using Zenject;

namespace Frontier.Entities
{
    /// <summary>
    /// 攻撃可能範囲の取得、及びその表示・非表示を切り替えるハンドラです
    /// </summary>
    public class ActionableRangeRenderer
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld     = null;
        [Inject] private PrefabRegistry _prefabReg              = null;
        [Inject] private IStageDataProvider _stageDataProvider  = null;

        private bool _isShowingAttackableRange = false;
        private Character _owner               = null;
        private ReadOnlyReference<ActionableTileData> _readOnlyActionableTileData;

        // DrawRange で実際に描画したタイルインデックスを TileMapType 別に記録します。
        // ActionableTileData がリセットされても確実にクリアできるようにするために使用します。
        private readonly Dictionary<TileMapType, HashSet<int>> _drawnTileIndices = new Dictionary<TileMapType, HashSet<int>>();
        // 危険範囲として各タイルに登録したタイルインデックスを記録します。
        private readonly HashSet<int> _dangerTileIndices = new HashSet<int>();

        public bool IsShowingAttackableRange => _isShowingAttackableRange;

        public void Init( Character owner, ActionableTileData actionableTileMap )
        {
            _isShowingAttackableRange   = false;
            _owner                      = owner;
            _readOnlyActionableTileData = new ReadOnlyReference<ActionableTileData>( actionableTileMap );
            _drawnTileIndices.Clear();
            _dangerTileIndices.Clear();
        }

        public void Dispose()
        {
            _owner = null;
            _readOnlyActionableTileData = null;
        }

        /// <summary>
        /// 指定されたタイルを危険範囲(このキャラクターが攻撃可能な範囲)として表示します。
        /// 危険範囲は、移動・攻撃範囲等の表示(DrawRange)とは別の層として各タイルに登録されるため、互いの描画・消去に影響しません。
        /// </summary>
        public void ShowDangerRange( IEnumerable<int> tileIndices, in UnityEngine.Color color )
        {
            if( _isShowingAttackableRange ) { return; }

            _isShowingAttackableRange = true;

            CharacterKey ownerKey = _owner.GetCharacterKey();

            foreach( int tileIndex in tileIndices )
            {
                var tile = _stageDataProvider.CurrentData.GetTile( tileIndex );
                if( tile == null ) { continue; }

                tile.AddDangerRange( ownerKey, in color, CreateTileMesh );
                _dangerTileIndices.Add( tileIndex );
            }
        }

        /// <summary>
        /// 危険範囲の表示を消去します
        /// </summary>
        public void HideDangerRange()
        {
            if( !_isShowingAttackableRange ) { return; }

            _isShowingAttackableRange = false;

            CharacterKey ownerKey = _owner.GetCharacterKey();

            foreach( int tileIndex in _dangerTileIndices )
            {
                _stageDataProvider.CurrentData.GetTile( tileIndex )?.RemoveDangerRange( ownerKey );
            }
            _dangerTileIndices.Clear();
        }

        /// <summary>
        /// Characterの持つActionableTileDataのうち、指定されたTileMapTypeに該当する範囲を描画します
        /// </summary>
        public void DrawRange( TileMapType tileMapType, Func<TileDynamicData, (MeshType meshType, bool condition)[]> conditionBuilder )
        {
            if( !_drawnTileIndices.ContainsKey( tileMapType ) )
            {
                _drawnTileIndices[tileMapType] = new HashSet<int>();
            }

            foreach( var data in _readOnlyActionableTileData.Value.GetTileMap( tileMapType ) )
            {
                var meshTypeAndConditions = conditionBuilder( data.Value );

                for( int i = 0; i < meshTypeAndConditions.Length; ++i )
                {
                    if( meshTypeAndConditions[i].condition )
                    {
                        var tile = _stageDataProvider.CurrentData.GetTile( data.Key );
                        tile.DrawTileMesh( CreateTileMesh(), in TileColors.Colors[( int ) meshTypeAndConditions[i].meshType], _owner.GetCharacterKey(), tileMapType );
                        _drawnTileIndices[tileMapType].Add( data.Key );

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 指定した TileMapType（複数可）に該当するタイルの描画を消去します。
        /// ビットフラグで複数種を同時に指定できます。
        /// 移動・攻撃・ターゲット範囲のいずれかを消去する場合は、危険範囲の表示も併せて消去します。
        /// </summary>
        public void ClearTileMeshesByType( TileMapType types )
        {
            ClearTileMeshesByTypeKeepingDangerRange( types );

            if( ( types & ( TileMapType.MOVEABLE | TileMapType.ATTACKABLE | TileMapType.TARGETABLE ) ) != 0 )
            {
                HideDangerRange();
            }
        }

        /// <summary>
        /// 指定した TileMapType（複数可）に該当するタイルの描画を、危険範囲の表示を残したまま消去します。
        /// (カーソルを合わせている間のみ表示する範囲の消去など、危険範囲の表示状態に影響させたくない場合に使用します)
        /// </summary>
        public void ClearTileMeshesByTypeKeepingDangerRange( TileMapType types )
        {
            CharacterKey ownerKey = _owner.GetCharacterKey();

            foreach( TileMapType singleType in Enum.GetValues( typeof( TileMapType ) ) )
            {
                if( singleType == TileMapType.NONE ) { continue; }
                if( !types.HasFlag( singleType ) ) { continue; }

                // ActionableTileData がリセットされていても確実にクリアできるよう、
                // 実際に描画したタイルインデックスのレコードを参照してクリアします。
                if( _drawnTileIndices.TryGetValue( singleType, out var drawnIndices ) )
                {
                    foreach( int tileIndex in drawnIndices )
                    {
                        var tile = _stageDataProvider.CurrentData.GetTile( tileIndex );
                        tile?.ClearTileMesh( ownerKey, singleType );
                    }
                    drawnIndices.Clear();
                }

                if( singleType == TileMapType.QUEUED )
                {
                    _readOnlyActionableTileData.Value.ClearTileMap( TileMapType.QUEUED );
                }
            }
        }

        /// <summary>
        /// 予約済み攻撃範囲のタイルメッシュを点滅させます。
        /// DrawTargetableRangeAsQueued() で描画済みの状態で呼ぶことを想定しています。
        /// </summary>
        public void SetBlinkTargetableRange( bool isBlink )
        {
            foreach( var data in _readOnlyActionableTileData.Value.GetTileMap( TileMapType.QUEUED ) )
            {
                var tile = _stageDataProvider.CurrentData.GetTile( data.Key );
                if( tile == null ) { continue; }
                tile.GetTileMeshByOwnerKey( _owner.GetCharacterKey() )?.SetBlink( isBlink );
            }
        }

        /// <summary>
        /// 各タイルに登録したタイルメッシュの描画を全て消去します
        /// </summary>
        public void ClearTileMeshesAllType()
        {
            ClearTileMeshesByType( TileMapType.MOVEABLE | TileMapType.ATTACKABLE | TileMapType.TARGETABLE | TileMapType.QUEUED );
        }

        private TileMesh CreateTileMesh()
        {
            return _hierarchyBld.CreateComponentAndOrganize<TileMesh>( _prefabReg.TileMeshPrefab, true );
        }
    }
}
