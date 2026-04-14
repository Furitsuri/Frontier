using Frontier.Battle;
using Frontier.Combat;
using Frontier.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;
using static UnityEngine.GraphicsBuffer;

namespace Frontier.Stage
{
    /// <summary>
    /// タイル情報を管理します
    /// </summary>
    public class TileDataHandler
    {
        [Inject] private IStageDataProvider _stageDataProvider  = null;
        [Inject] private BattleRoutineController _btlRtnCtrl    = null;

        private delegate void RegisterAttackableTileCallback( TileDynamicData[] tiles, int l, int m, int n, CHARACTER_TAG tag, ActionableTileData map );

        private int[] _directionOffsets;
        private RegisterAttackableTileCallback[] _registerAttackableTileCallbacks;

        #region PUBLIC_METHOD

        [Inject]
        public TileDataHandler()
        {
            _registerAttackableTileCallbacks = new RegisterAttackableTileCallback[( int )RangeShape.NUM]
            {
                RegisterAttackableTilesAllSides,
                RegisterAttackableTilesAllSidesLinearly,
            };
        }

        public void Init()
        {
            // コンストラクタ時点ではCurrentDataがnullのため、Initで初期化
            _directionOffsets = new int[( int ) Direction.NUM]
            {
                _stageDataProvider.CurrentData.TileColNum,  // Direction.FORWARD
                1,                                          // Direction.RIGHT
                -_stageDataProvider.CurrentData.TileColNum, // Direction.BACK
                -1                                          // Direction.LEFT
            };
        }

        /// <summary>
        /// タイル情報を更新します
        /// </summary>
        public void UpdateTileDynamicDatas()
        {
            ResetTileDynamicDatas();    // 一度全てのタイル情報を元に戻す

            for( int i = 0; i < ( int ) CHARACTER_TAG.NUM; ++i )
            {
                foreach( var chara in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( ( CHARACTER_TAG ) i ) )
                {
                    var tileIndex       = chara.BattleParams.TmpParam.CurrentTileIndex;
                    var tileData        = _stageDataProvider.CurrentData.GetTileDynamicData( tileIndex );
                    tileData.CharaKey   = new CharacterKey( chara.GetStatusRef.characterTag, chara.GetStatusRef.characterIndex );
                }
            }
        }

        public void BeginExpandTargetableTilesWithPartOfRange( int baseTileIndex, int expandRange, CHARACTER_TAG ownerTag, ActionableTileData actionableTileData )
        {
            // 基データを直接変更しないようにクローンを作成してから処理を行う
            var cloneStageDynamicDatas = _stageDataProvider.CurrentData.DeepCloneStageDynamicData();

            ExpandTargetableTilesWithPartOfRange( cloneStageDynamicDatas, baseTileIndex, expandRange, ownerTag, actionableTileData );
        }

        /// <summary>
        /// 移動可能なタイルを登録します
        /// </summary>
        /// <param name="tileDynamicDatas"></param>
        /// <param name="dprtIdx"></param>
        /// <param name="mvRng"></param>
        /// <param name="jmp"></param>
        /// <param name="atkRng"></param>
        /// <param name="height"></param>
        /// <param name="tileCosts"></param>
        /// <param name="charaKey"></param>
        /// <param name="actionableTileMap"></param>
        public void BeginRegisterMoveableTiles( TileDynamicData[] tileDynamicDatas, int dprtIdx, int mvRng, int jmp, int atkRng, float height, in int[] tileCosts, in CharacterKey charaKey, ref ActionableTileData actionableTileMap )
        {
            Debug.Assert( dprtIdx.IsInHalfOpenRange( 0, tileDynamicDatas.Length ), "Irregular Index." );

            if( mvRng < 0 ) { return; }

            tileDynamicDatas[dprtIdx].EstimatedMoveRange = mvRng;
            actionableTileMap.AddMoveableTile( dprtIdx, tileDynamicDatas[dprtIdx] );
            RegisterMoveableTilesAllSides( tileDynamicDatas, dprtIdx, mvRng, jmp, atkRng, height, in tileCosts, in charaKey, ref actionableTileMap );
        }

        /// <summary>
        /// タイルへの攻撃可能情報の登録を開始します
        /// </summary>
        /// <param name="tileDynamicDatas"></param>
        /// <param name="dprtIdx"></param>
        /// <param name="atkRng"></param>
        /// <param name="charaTag"></param>
        /// <param name="isClearAttackableInfo"></param>
        /// <param name="actionableTileMap"></param>
        public void BeginRegisterAttackableTiles( TileDynamicData[] tileDynamicDatas, int dprtIdx, int atkRng, RangeShape rangeType, CHARACTER_TAG charaTag, bool isClearAttackableInfo, ref ActionableTileData actionableTileMap )
        {
            Debug.Assert( dprtIdx.IsInHalfOpenRange( 0, _stageDataProvider.CurrentData.GetTileTotalNum() ), "StageController : Irregular Index." );

            if( isClearAttackableInfo ) { ClearAttackableInformation(); }   // 全てのタイルの攻撃可否情報を初期化

            // 攻撃可否情報を各タイルに登録
            int targetTileIndex = dprtIdx;    // 開始時点では出発タイルと同じ
            _registerAttackableTileCallbacks[( int )rangeType]( tileDynamicDatas, dprtIdx, targetTileIndex, atkRng, charaTag, actionableTileMap );
        }

        /// <summary>
        /// タイルの配置不可色を消去します
        /// </summary>
        public void ClearUndeployableColorOfTiles()
        {
            for( int i = 0; i < _stageDataProvider.CurrentData.GetTileTotalNum(); ++i )
            {
                _stageDataProvider.CurrentData.Tiles[i].ClearUndeployableColor();
            }
        }

        /// <summary>
        /// 攻撃可能情報を消去します
        /// </summary>
        public void ClearAttackableInformation()
        {
            UnsetAllTilesBitFlag( TileBitFlag.REACHABLE_ATTACK | TileBitFlag.ATTACKABLE | TileBitFlag.ATTACKABLE_TARGET_EXIST );
        }

        /// <summary>
        /// アクション(移動、攻撃)が可能な範囲を抽出します
        /// 攻撃の範囲を不要にしたい場合は、atkRngの値を0以下に指定してください
        /// </summary>
        /// <param name="dprtIdx"></param>
        /// <param name="mvRng"></param>
        /// <param name="jmp"></param>
        /// <param name="atkRng"></param>
        /// <param name="dprtHeight"></param>
        /// <param name="tileCosts"></param>
        /// <param name="charaKey"></param>
        /// <returns></returns>
        public void ExtractActionableRangeData( int dprtIdx, int mvRng, int jmp, int atkRng, float dprtHeight, in int[] tileCosts, in CharacterKey charaKey, ref ActionableTileData actionableTileMap )
        {
            // 基データを直接変更しないようにクローンを作成してから処理を行う
            var cloneStageDynamicDatas = _stageDataProvider.CurrentData.DeepCloneStageDynamicData();

            BeginRegisterAttackableTiles( cloneStageDynamicDatas, dprtIdx, atkRng, RangeShape.FROM_MYSELF, charaKey.CharacterTag, false, ref actionableTileMap );
            BeginRegisterMoveableTiles( cloneStageDynamicDatas, dprtIdx, mvRng, jmp, atkRng, dprtHeight, in tileCosts, in charaKey, ref actionableTileMap );
        }

        /// <summary>
        /// 指定のパラメータに対応する攻撃範囲をactionableTileMapに抽出します
        /// </summary>
        /// <param name="dprtIdx"></param>
        /// <param name="mvRng"></param>
        /// <param name="jmp"></param>
        /// <param name="atkRng"></param>
        /// <param name="dprtHeight"></param>
        /// <param name="tileCosts"></param>
        /// <param name="charaKey"></param>
        /// <returns></returns>
        public void ExtractAttackableData( int dprtIdx, int atkRng, RangeShape rangeType, in CharacterKey charaKey, ref ActionableTileData actionableTileMap )
        {
            // 基データを直接変更しないようにクローンを作成してから処理を行う
            var cloneStageDynamicDatas = _stageDataProvider.CurrentData.DeepCloneStageDynamicData();

            BeginRegisterAttackableTiles( cloneStageDynamicDatas, dprtIdx, atkRng, rangeType, charaKey.CharacterTag, false, ref actionableTileMap );
        }

        /// <summary>
        /// 指定する条件に合う移動可能タイルを抽出します
        /// </summary>
        /// <param name="actionableTileMap"></param>
        /// <param name="setup"></param>
        /// <param name="condition"></param>
        /// <param name="args"></param>
        public void ExtractMoveableRangeDataFilterByCondition( ref ActionableTileData actionableTileMap, Func<TileDynamicData[]> setup, Func<TileDynamicData, object[], bool> condition, params object[] args )
        {
            // 呼び出し側で設定されたセットアップ
            var moveableTileMap = setup();

            for( int i = 0; i < moveableTileMap.Length; ++i )
            {
                // 条件は呼び出し側で設定
                if( condition( moveableTileMap[i], args ) )
                {
                    actionableTileMap.AddMoveableTile( i, moveableTileMap[i] );
                }
            }
        }

        /// <summary>
        /// 2つの指定のインデックスが隣り合う座標に存在しているかを判定します
        /// </summary>
        /// <param name="fstIndex">指定インデックスその1</param>
        /// <param name="scdIndex">指定インデックスその2</param>
        /// <returns>隣り合うか否か</returns>
        public bool IsTileNextToEacheOther( int fstIndex, int scdIndex )
        {
            var colNum = _stageDataProvider.CurrentData.TileColNum;

            bool updown = ( Math.Abs( fstIndex - scdIndex ) == colNum );

            int fstQuotient     = fstIndex / colNum;
            int scdQuotient     = scdIndex / colNum;
            var fstRemainder    = fstIndex % colNum;
            var scdRemainder    = scdIndex % colNum;
            bool leftright      = ( fstQuotient == scdQuotient ) && ( Math.Abs( fstRemainder - scdRemainder ) == 1 );

            return updown || leftright;
        }

        /// <summary>
        /// 隣接タイル間の移動コストを計算し、コストとタイル間を移動可能か否かを返します
        /// </summary>
        /// <param name="dprtIndex">基点となるタイルインデックス</param>
        /// <param name="destIndex">移動目標となるタイルインデックス</param>
        /// <param name="jumpForce">ジャンプ力</param>
        /// <param name="ownerTileCosts">移動を行うキャラクターのタイルコスト</param>
        /// <returns>タイル間のコスト及び移動可否</returns>
        public ( int cost, bool passable ) CalcurateTileCost( int dprtIndex, int destIndex, int jumpForce, in int[] ownerTileCosts )
        {
            // 隣接していないタイル間の移動は不可
            if( !IsTileNextToEacheOther( dprtIndex, destIndex ) ) { return ( short.MaxValue, false ); }

            // 目的地のタイルのタイプから移動コストを取得
            var destTileData        = _stageDataProvider.CurrentData.GetTile( destIndex ).StaticData();
            TileType destTileType   = destTileData.TileType;
            int tileCost            = ownerTileCosts[( int ) destTileType];

            // 高低差コストを取得
            int heightCost = CalcurateHeightCost( dprtIndex, destIndex, jumpForce );

            return ( tileCost + heightCost, heightCost <= jumpForce );
        }

        /// <summary>
        /// 現在選択しているタイルの動的データを取得します
        /// 攻撃対象選択状態では選択している攻撃対象が存在するタイル情報を取得します
        /// </summary>
        /// /// <returns>タイル間のコスト及び移動可否</returns>
        public ( TileStaticData, TileDynamicData ) GetTileDatas( int index )
        {
            return ( _stageDataProvider.CurrentData.GetTileStaticData( index ), _stageDataProvider.CurrentData.GetTileDynamicData( index ) );
        }

        #endregion PUBLIC_METHOD

        #region PRIVATE_METHOD

        private void ExpandTargetableTilesWithPartOfRange( TileDynamicData[] tileDynamicDatas, int tileIndex, int expandRange, CHARACTER_TAG ownerTag, ActionableTileData actionableTileData )
        {
            // 範囲外のタイルは考慮しない
            if( tileIndex < 0 || tileDynamicDatas.Length <= tileIndex ) { return; }
            // 指定のタイル情報を取得
            var tileDynamicData = tileDynamicDatas[tileIndex];
            // 移動不可のグリッドに辿り着いた場合は終了
            if( Methods.HasAnyFlag( tileDynamicData.Flag, TileBitFlag.CANNOT_MOVE ) ) { return; }
            // 既にターゲット可能なグリッドであれば終了
            if( Methods.HasAnyFlag( tileDynamicData.Flag, TileBitFlag.TARGETABLE ) ) { return; }

            // ターゲット可能タイルとして登録
            actionableTileData.AddTargetableTile( tileIndex, tileDynamicData );

            // 敵対勢力のキャラクターが存在するタイルであれば、攻撃対象タイルとして登録
            if( BattleLogicBase.IsOpponentFaction[( int ) ownerTag]( tileDynamicData.CharaKey.CharacterTag ) )
            {
                actionableTileData.AddAttackTargetTileIndex( tileIndex );
            }

            // 次に展開する際の展開範囲が0以下であれば終了
            if( --expandRange <= 0 ) { return; }

            var tileColNum = _stageDataProvider.CurrentData.TileColNum;

            // 現在のタイルから更に四方に展開
            ExpandTargetableTilesWithPartOfRange( tileDynamicDatas, tileIndex - 1, expandRange, ownerTag, actionableTileData );            // tileIndexからX軸方向へ-1
            ExpandTargetableTilesWithPartOfRange( tileDynamicDatas, tileIndex + 1, expandRange, ownerTag, actionableTileData );            // tileIndexからX軸方向へ+1
            ExpandTargetableTilesWithPartOfRange( tileDynamicDatas, tileIndex - tileColNum, expandRange, ownerTag, actionableTileData );   // tileIndexからZ軸方向へ-1
            ExpandTargetableTilesWithPartOfRange( tileDynamicDatas, tileIndex + tileColNum, expandRange, ownerTag, actionableTileData );   // tileIndexからZ軸方向へ+1
        }

        /// <summary>
        /// タイル情報を基の状態に戻します
        /// </summary>
        private void ResetTileDynamicDatas()
        {
            for( int i = 0; i < _stageDataProvider.CurrentData.Tiles.Length; ++i )
            {
                _stageDataProvider.CurrentData.Tiles[i].ApplyBaseTileDynamicData();
            }
        }

        /// <summary>
        /// 全てのタイルにおいて、指定のビットフラグの設定を解除します
        /// </summary>
        private void UnsetAllTilesBitFlag( TileBitFlag value )
        {
            // 全てのグリッドの移動・攻撃可否情報を初期化
            for( int i = 0; i < _stageDataProvider.CurrentData.GetTileTotalNum(); ++i )
            {
                Methods.UnsetBitFlag( ref _stageDataProvider.CurrentData.GetTileDynamicData( i ).Flag, value );
            }
        }

        private void RegisterMoveableTilesAllSides( TileDynamicData[] tileDynamicDatas, int tileIdx, int mvRng, int jmp, int atkRng,  float height, in int[] tileCosts, in CharacterKey charaKey, ref ActionableTileData actionableTileMap )
        {
            int colNum = _stageDataProvider.CurrentData.TileColNum;

            // 左端を除外
            if( tileIdx % colNum != 0 )
            {
                RegisterMoveableTiles( tileDynamicDatas, tileIdx - 1, mvRng, jmp, atkRng, height, in tileCosts, in charaKey, ref actionableTileMap );   // tileIndexからX軸方向へ-1
            }
            // 右端を除外
            if( ( tileIdx + 1 ) % colNum != 0 )
            {
                RegisterMoveableTiles( tileDynamicDatas, tileIdx + 1, mvRng, jmp, atkRng, height, in tileCosts, in charaKey, ref actionableTileMap );   // tileIndexからX軸方向へ+1
            }
            // Z軸方向への加算と減算はそのまま
            RegisterMoveableTiles( tileDynamicDatas, tileIdx - colNum, mvRng, jmp, atkRng, height, in tileCosts, in charaKey, ref actionableTileMap );  // tileIndexからZ軸方向へ-1
            RegisterMoveableTiles( tileDynamicDatas, tileIdx + colNum, mvRng, jmp, atkRng, height, in tileCosts, in charaKey, ref actionableTileMap );  // tileIndexからZ軸方向へ+1
        }

        private void RegisterAttackableTilesAllSides( TileDynamicData[] tileDynamicDatas, int dprtIdx, int tgtTileIdx, int atkRng, CHARACTER_TAG charaTag, ActionableTileData actionableTileMap )
        {
            int colNum = _stageDataProvider.CurrentData.TileColNum;

            // 左端を除外
            if( tgtTileIdx % colNum != 0 )
            {
                RegisterAttackableTiles( tileDynamicDatas, dprtIdx, tgtTileIdx - 1, atkRng, charaTag, actionableTileMap );  // tgtTileIdxからX軸方向へ-1
            }
            // 右端を除外
            if( ( tgtTileIdx + 1 ) % colNum != 0 )
            {
                RegisterAttackableTiles( tileDynamicDatas, dprtIdx, tgtTileIdx + 1, atkRng, charaTag, actionableTileMap );  // tgtTileIdxからX軸方向へ+1
            }
            // Z軸方向への加算と減算はそのまま
            RegisterAttackableTiles( tileDynamicDatas, dprtIdx, tgtTileIdx - colNum, atkRng, charaTag, actionableTileMap ); // tgtTileIdxからZ軸方向へ-1
            RegisterAttackableTiles( tileDynamicDatas, dprtIdx, tgtTileIdx + colNum, atkRng, charaTag, actionableTileMap ); // targetTileIndexからZ軸方向へ+1
        }

        private void RegisterAttackableTilesAllSidesLinearly( TileDynamicData[] tileDynamicDatas, int dprtIdx, int tgtTileIdx, int atkRng, CHARACTER_TAG charaTag, ActionableTileData actionableTileMap )
        {
            int colNum = _stageDataProvider.CurrentData.TileColNum;

            // 左端を除外
            if( tgtTileIdx % colNum != 0 )
            {
                RegisterAttackableTilesLinearly( tileDynamicDatas, dprtIdx, tgtTileIdx - 1, atkRng, Direction.LEFT, charaTag, actionableTileMap );  // tgtTileIdxからX軸方向へ-1
            }
            // 右端を除外
            if( ( tgtTileIdx + 1 ) % colNum != 0 )
            {
                RegisterAttackableTilesLinearly( tileDynamicDatas, dprtIdx, tgtTileIdx + 1, atkRng, Direction.RIGHT, charaTag, actionableTileMap );  // tgtTileIdxからX軸方向へ+1
            }
            // Z軸方向への加算と減算はそのまま
            RegisterAttackableTilesLinearly( tileDynamicDatas, dprtIdx, tgtTileIdx - colNum, atkRng, Direction.BACK,charaTag, actionableTileMap ); // tgtTileIdxからZ軸方向へ-1
            RegisterAttackableTilesLinearly( tileDynamicDatas, dprtIdx, tgtTileIdx + colNum, atkRng, Direction.FORWARD, charaTag, actionableTileMap ); // targetTileIndexからZ軸方向へ+1
        }

        private void RegisterMoveableTiles( TileDynamicData[] tileDynamicDatas, int tileIdx, int mvRng, int jmp, int atkRng, float prevHeight, in int[] tileCosts, in CharacterKey charaKey, ref ActionableTileData actionableTileMap )
        {
            // 範囲外のタイルは考慮しない
            if( tileIdx < 0 || tileDynamicDatas.Length <= tileIdx ) { return; }
            // 指定のタイル情報を取得
            var tileDynamicData = tileDynamicDatas[ tileIdx ];
            // 移動不可のグリッドに辿り着いた場合は終了
            if( Methods.HasAnyFlag( tileDynamicData.Flag, TileBitFlag.CANNOT_MOVE ) ) { return; }
            // 既に計算済みのグリッドであれば終了
            if( mvRng <= tileDynamicData.EstimatedMoveRange ) { return; }
            // 自身における敵対勢力キャラクターが存在すれば終了
            if( BattleLogicBase.IsOpponentFaction[Convert.ToInt32( charaKey.CharacterTag )]( tileDynamicData.CharaKey.CharacterTag ) ) { return; }

            // 直前のタイルとの高さの差分を求め、ジャンプ値と比較して移動可能かを判定する
            var staticData  = _stageDataProvider.CurrentData.GetTileStaticData( tileIdx );
            float curHeight = staticData.Height;
            int heightCost  = CalcurateHeightCost( prevHeight, curHeight, jmp );

            // 現在グリッドの移動抵抗値を更新( 出発グリッドではmoveRangeの値をそのまま適応する )
            int tileTypeIndex           = Convert.ToInt32( staticData.TileType );
            int currentMoveRange        = mvRng - tileCosts[tileTypeIndex] - heightCost;
            tileDynamicData.EstimatedMoveRange = currentMoveRange;

            // 負の値であれば終了
            if( currentMoveRange < 0 ) { return; }
            // 0以上(通行可能)である場合は登録
            else { actionableTileMap.AddMoveableTile( tileIdx, tileDynamicDatas[tileIdx] ); }

            // 攻撃範囲についても登録する
            if( ( 0 < atkRng ) && ( !tileDynamicData.CharaKey.IsValid() || tileDynamicData.CharaKey == charaKey ) )
            {
                BeginRegisterAttackableTiles( tileDynamicDatas, tileIdx, atkRng, RangeShape.FROM_MYSELF, charaKey.CharacterTag, false, ref actionableTileMap );
            }

            RegisterMoveableTilesAllSides( tileDynamicDatas, tileIdx, currentMoveRange, jmp, atkRng, curHeight, in tileCosts, in charaKey, ref actionableTileMap );
        }

        private bool RegisterAttackableTile( TileDynamicData[] tileDynamicDatas, int dprtIdx, int tgtTileIdx, ref int atkRng, CHARACTER_TAG charaTag, ActionableTileData actionableTileMap )
        {
            // 範囲外のグリッドは考慮しない
            if( !tgtTileIdx.IsInHalfOpenRange( 0, tileDynamicDatas.Length ) ) { return false; }
            // 移動不可のグリッドには攻撃できない
            if( Methods.HasAnyFlag( tileDynamicDatas[tgtTileIdx].Flag, TileBitFlag.CANNOT_MOVE ) ) { return false; }
            // 高低差が攻撃範囲を超過している場合は攻撃できない
            var dprtTileData    = _stageDataProvider.CurrentData.GetTileStaticData( dprtIdx );
            var targetTileData  = _stageDataProvider.CurrentData.GetTileStaticData( tgtTileIdx );
            int diffHeight      = Convert.ToInt32( Mathf.Ceil( Mathf.Abs( targetTileData.Height - dprtTileData.Height ) ) );
            if( atkRng < diffHeight ) { return false; }

            // 出発地点でなければ登録
            if( tgtTileIdx != dprtIdx )
            {
                var tgtTileData = tileDynamicDatas[tgtTileIdx];
                Methods.SetBitFlag( ref tgtTileData.Flag, TileBitFlag.ATTACKABLE ); // 攻撃可能地点であることをフラグに記述

                // tgtTileに攻撃対象となるキャラクターがいれば、そのこともフラグに記述
                if( BattleLogicBase.IsOpponentFaction[( int ) charaTag]( tgtTileData.CharaKey.CharacterTag ) )
                {
                    Methods.SetBitFlag( ref tileDynamicDatas[dprtIdx].Flag, TileBitFlag.REACHABLE_ATTACK );   // dprtIdxであれば攻撃対象へ攻撃可能であることを記述
                    Methods.SetBitFlag( ref tgtTileData.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST );    // tgtTileに攻撃可能な攻撃対象がいることを記述
                }

                actionableTileMap.AddAttackableTile( tgtTileIdx, tgtTileData ); // 登録
            }

            if( --atkRng <= 0 ) { return false; }   // 負の値であれば終了

            return true;
        }

        /// <summary>
        /// 攻撃可能なタイルを登録します
        /// </summary>
        /// <param name="dprtIndex">出発タイルインデックス</param>
        /// <param name="targetTileIndex">対象のグリッドインデックス</param>
        /// <param name="atkRange">攻撃可能範囲値</param>
        /// <param name="ownerTag">自身のキャラクタータグ</param>
        private void RegisterAttackableTiles( TileDynamicData[] tileDynamicDatas, int dprtIdx, int tgtTileIdx, int atkRng, CHARACTER_TAG charaTag, ActionableTileData actionableTileMap )
        {
            if( !RegisterAttackableTile( tileDynamicDatas, dprtIdx, tgtTileIdx, ref atkRng, charaTag, actionableTileMap ) ) { return; }

            RegisterAttackableTilesAllSides( tileDynamicDatas, dprtIdx, tgtTileIdx, atkRng, charaTag, actionableTileMap );  // 現在のtargetTileIndexの地点から更に四方に展開
        }

        private void RegisterAttackableTilesLinearly( TileDynamicData[] tileDynamicDatas, int dprtIdx, int tgtTileIdx, int atkRng, Direction dir, CHARACTER_TAG charaTag, ActionableTileData actionableTileMap )
        {
            if( !RegisterAttackableTile( tileDynamicDatas, dprtIdx, tgtTileIdx, ref atkRng, charaTag, actionableTileMap ) ) { return; }

            tgtTileIdx += _directionOffsets[( int ) dir]; // 次のタイルインデックスを設定

            RegisterAttackableTilesLinearly( tileDynamicDatas, dprtIdx, tgtTileIdx, atkRng, dir, charaTag, actionableTileMap );  // 現在のtargetTileIndexの地点から更に四方に展開
        }

        /// <summary>
        /// 隣接タイル間の高低差コストを計算します
        /// </summary>
        /// <param name="dprtIndex"></param>
        /// <param name="destIndex"></param>
        /// <param name="jumpForce"></param>
        /// <returns></returns>
        private int CalcurateHeightCost( int dprtIndex, int destIndex, int jumpForce )
        {
            Debug.Assert( IsTileNextToEacheOther( dprtIndex, destIndex ), "" );

            float dprtHeight = _stageDataProvider.CurrentData.GetTile( dprtIndex ).StaticData().Height;
            float destHeight = _stageDataProvider.CurrentData.GetTile( destIndex ).StaticData().Height;

            return CalcurateHeightCost( dprtHeight, destHeight, jumpForce );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dprtHeight"></param>
        /// <param name="destHeight"></param>
        /// <param name="jumpForce"></param>
        /// <returns></returns>
        private int CalcurateHeightCost( float dprtHeight, float destHeight, int jumpForce )
        {
            int retCost             = 0;
            float diffHeightCeil    = Mathf.Ceil( destHeight - dprtHeight );

            if( jumpForce < diffHeightCeil ||                           // ジャンプ力を超過している場合は、移動不可
                diffHeightCeil <= -1 * ( jumpForce + DESCENT_MARGIN ) ) // 移動先のタイルが低くてても、高低差がジャンプ力+定数を超過している場合は、移動不可
            {
                retCost += short.MaxValue;  // int.MaxValueを入れるとオーバーフローしてしまうため、short.MaxValueに留める
            }
            // ジャンプ力以内の高さであれば、その分をコストに加算する
            else if( 0 < diffHeightCeil )
            {
                retCost += Convert.ToInt32( diffHeightCeil );
            }

            return retCost;
        }

        #endregion PRIVATE_METHOD
    }
}