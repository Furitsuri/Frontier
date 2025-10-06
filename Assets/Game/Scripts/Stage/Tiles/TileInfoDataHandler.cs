using Frontier.Battle;
using Frontier.Entities;
using Frontier.Stage;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Stage
{
    /// <summary>
    /// タイル情報を管理します
    /// </summary>
    public class TileInfoDataHandler
    {
        [Inject] private IStageDataProvider _stageDataProvider  = null;
        [Inject] private BattleRoutineController _btlRtnCtrl    = null;
        [Inject] private GridCursorController _gridCursorCtrl   = null;

        private List<int> _attackableTileIndexs = new List<int>();

        #region PUBLIC_METHOD

        /// <summary>
        /// タイル情報を更新します
        /// </summary>
        public void UpdateTileInfo()
        {
            ResetTileInfo();    // 一度全てのタイル情報を元に戻す

            // キャラクターが存在するタイルの情報を更新
            TileBitFlag[] flags =
            {
                TileBitFlag.ALLY_EXIST,
                TileBitFlag.ENEMY_EXIST,
                TileBitFlag.OTHER_EXIST
            };
            for( int i = 0; i < ( int ) CHARACTER_TAG.NUM; ++i )
            {
                foreach( var chara in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( ( CHARACTER_TAG ) i ) )
                {
                    var gridIndex       = chara.Params.TmpParam.GetCurrentGridIndex();
                    ref var tileInfo    = ref _stageDataProvider.CurrentData.GetTileInfo( gridIndex );
                    tileInfo.charaTag   = chara.Params.CharacterParam.characterTag;
                    tileInfo.charaIndex = chara.Params.CharacterParam.characterIndex;
                    Methods.SetBitFlag( ref tileInfo.flag, flags[i] );
                }
            }
        }

        /// <summary>
        /// タイルへの移動可能情報の登録を開始します
        /// </summary>
        /// <param name="dprtIndex"></param>
        /// <param name="moveRange"></param>
        /// <param name="atkRange"></param>
        /// <param name="jumpForce"></param>
        /// <param name="ownerIndex"></param>
        /// <param name="prevHeight"></param>
        /// <param name="ownerTileCosts"></param>
        /// <param name="selfTag"></param>
        /// <param name="isAttackable"></param>
        public void BeginRegisterMoveableTiles( int dprtIndex, int moveRange, int atkRange, int jumpForce, int ownerIndex, float dprtHeight, in int[] ownerTileCosts, CHARACTER_TAG selfTag, bool isAttackable )
        {
            Debug.Assert( dprtIndex.IsInHalfOpenRange( 0, _stageDataProvider.CurrentData.GetTileTotalNum() ), "StageController : Irregular Index." );

            var tileInfo = _stageDataProvider.CurrentData.GetTileInfo( dprtIndex );
            if( tileInfo == null ) { return; }
            tileInfo.estimatedMoveRange = moveRange;

            RegisterMoveableTilesAllSides( dprtIndex, moveRange, atkRange, jumpForce, ownerIndex, dprtHeight, in ownerTileCosts, selfTag, isAttackable );
        }

        /// <summary>
        /// タイルへの攻撃可能情報の登録を開始します
        /// </summary>
        /// <param name="dprtIndex">攻撃キャラクターが存在するグリッドのインデックス値</param>
        /// <param name="atkRange">攻撃可能範囲値</param>
        /// <param name="selfTag">攻撃を行うキャラクター自身のキャラクタータグ</param>
        public void BeginRegisterAttackableTiles( int dprtIndex, int atkRange, CHARACTER_TAG ownerTag, bool isClearAttackableInfo )
        {
            Debug.Assert( dprtIndex.IsInHalfOpenRange( 0, _stageDataProvider.CurrentData.GetTileTotalNum() ), "StageController : Irregular Index." );

            if( isClearAttackableInfo ) { ClearAttackableInformation(); }   // 全てのタイルの攻撃可否情報を初期化

            // 攻撃可否情報を各タイルに登録
            int targetTileIndex = dprtIndex;    // 開始時点では出発タイルと同じ
            RegisterAttackableTilesAllSides( dprtIndex, targetTileIndex, atkRange, ownerTag );
        }

        /// <summary>
        /// 現在選択しているタイルの情報を取得します
        /// 攻撃対象選択状態では選択している攻撃対象が存在するタイル情報を取得します
        /// </summary>
        /// <param name="tileInfo">該当するタイルの情報</param>
        public void FetchCurrentTileInfo( out TileInformation tileInfo )
        {
            int index = 0;

            if( _gridCursorCtrl.GridState == GridCursorState.ATTACK )
            {
                index = _attackableTileIndexs[_gridCursorCtrl.GetAtkTargetIndex()];
            }
            else
            {
                index = _gridCursorCtrl.Index;
            }

            tileInfo = _stageDataProvider.CurrentData.GetTileInfo( index );
        }

        /// <summary>
        /// 攻撃可能情報を消去します
        /// </summary>
        public void ClearAttackableInformation()
        {
            _attackableTileIndexs.Clear();
            UnsetAllTilesBitFlag( TileBitFlag.REACHABLE_ATTACK | TileBitFlag.ATTACKABLE | TileBitFlag.ATTACKABLE_TARGET_EXIST );
        }

        /// <summary>
        /// 2つの指定のインデックスが隣り合う座標に存在しているかを判定します
        /// </summary>
        /// <param name="fstIndex">指定インデックスその1</param>
        /// <param name="scdIndex">指定インデックスその2</param>
        /// <returns>隣り合うか否か</returns>
        public bool IsTileNextToEacheOther( int fstIndex, int scdIndex )
        {
            var colNum = _stageDataProvider.CurrentData.GridColumnNum;

            bool updown = ( Math.Abs( fstIndex - scdIndex ) == colNum );

            int fstQuotient     = fstIndex / colNum;
            int scdQuotient     = scdIndex / colNum;
            var fstRemainder    = fstIndex % colNum;
            var scdRemainder    = scdIndex % colNum;
            bool leftright      = ( fstQuotient == scdQuotient ) && ( Math.Abs( fstRemainder - scdRemainder ) == 1 );

            return updown || leftright;
        }

        /// <summary>
        /// 攻撃可能タイルのうち、攻撃可能キャラクターが存在するタイルを専用のリストに追加していきます
        /// </summary>
        /// <param name="selfTag">攻撃を行うキャラクターのキャラクタータグ</param>
        /// <param name="target">予め攻撃対象が決まっている際に指定</param>
        /// <returns>攻撃可能キャラクターが存在している</returns>
        public bool CorrectAttackableTileIndexs( CHARACTER_TAG selfTag, Character target = null )
        {
            Character character = null;

            _gridCursorCtrl.ClearAtkTargetInfo();
            _attackableTileIndexs.Clear();

            // 攻撃可能、かつ攻撃対象となるキャラクターが存在するタイルのインデックス値をリストに登録
            for( int i = 0; i < _stageDataProvider.CurrentData.GetTileTotalNum(); ++i )
            {
                var info = _stageDataProvider.CurrentData.GetTileInfo( i );
                if( Methods.CheckBitFlag( info.flag, TileBitFlag.ATTACKABLE_TARGET_EXIST ) )
                {
                    character = _btlRtnCtrl.BtlCharaCdr.GetCharacterFromHashtable( info.charaTag, info.charaIndex );
                    if( character != null && character.Params.CharacterParam.characterTag != selfTag )
                    {
                        _attackableTileIndexs.Add( i );
                    }
                }
            }

            // グリッドカーソルの位置を、攻撃可能キャラクターが存在するタイル位置に設定
            if( 0 < _attackableTileIndexs.Count )
            {
                _gridCursorCtrl.SetAtkTargetNum( _attackableTileIndexs.Count );

                // 攻撃対象が定められている場合はその対象を探す
                if( target != null && 1 < _attackableTileIndexs.Count )
                {
                    for( int i = 0; i < _attackableTileIndexs.Count; ++i )
                    {
                        var info = _stageDataProvider.CurrentData.GetTileData( _attackableTileIndexs[i] ).GetTileInfo();
                        if( target == _btlRtnCtrl.BtlCharaCdr.GetCharacterFromHashtable( info.charaTag, info.charaIndex ) )
                        {
                            _gridCursorCtrl.SetAtkTargetIndex( i );
                            break;
                        }
                    }
                }
                // 定められていない場合は先頭を指定する
                else { _gridCursorCtrl.SetAtkTargetIndex( 0 ); }
            }

            return ( 0 < _attackableTileIndexs.Count );
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
            var destTileData        = _stageDataProvider.CurrentData.GetTileData( destIndex );
            TileType destTileType   = destTileData.Type;
            int tileCost            = ownerTileCosts[( int ) destTileType];

            // 高低差コストを取得
            int heightCost = CalcurateHeightCost( dprtIndex, destIndex, jumpForce );

            return ( tileCost + heightCost, heightCost <= jumpForce );
        }

        #endregion PUBLIC_METHOD

        #region PRIVATE_METHOD

        /// <summary>
        /// タイル情報を基の状態に戻します
        /// </summary>
        private void ResetTileInfo()
        {
            for( int i = 0; i < _stageDataProvider.CurrentData.GetTileTotalNum(); ++i )
            {
                _stageDataProvider.CurrentData.TileDatas[i].ApplyBaseTileInfo();
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
                Methods.UnsetBitFlag( ref _stageDataProvider.CurrentData.GetTileInfo( i ).flag, value );
            }
        }

        /// <summary>
        /// 指定のタイルから四方に向けて、移動可能なタイルを登録する処理を展開します
        /// </summary>
        /// <param name="tileIndex"></param>
        /// <param name="moveRange"></param>
        /// <param name="atkRange"></param>
        /// <param name="jumpForce"></param>
        /// <param name="ownerIndex"></param>
        /// <param name="height"></param>
        /// <param name="ownerTileCosts"></param>
        /// <param name="selfTag"></param>
        /// <param name="isAttackable"></param>
        private void RegisterMoveableTilesAllSides( int tileIndex, int moveRange, int atkRange, int jumpForce, int ownerIndex, float height, in int[] ownerTileCosts, CHARACTER_TAG selfTag, bool isAttackable )
        {
            int colNum = _stageDataProvider.CurrentData.GridColumnNum;

            // 左端を除外
            if( tileIndex % colNum != 0 )
            {
                RegisterMoveableTiles( tileIndex - 1, moveRange, atkRange, jumpForce, ownerIndex, height, in ownerTileCosts, selfTag, isAttackable );   // tileIndexからX軸方向へ-1
            }
            // 右端を除外
            if( ( tileIndex + 1 ) % colNum != 0 )
            {
                RegisterMoveableTiles( tileIndex + 1, moveRange, atkRange, jumpForce, ownerIndex, height, in ownerTileCosts, selfTag, isAttackable );   // tileIndexからX軸方向へ+1
            }
            // Z軸方向への加算と減算はそのまま
            RegisterMoveableTiles( tileIndex - colNum, moveRange, atkRange, jumpForce, ownerIndex, height, in ownerTileCosts, selfTag, isAttackable );  // tileIndexからZ軸方向へ-1
            RegisterMoveableTiles( tileIndex + colNum, moveRange, atkRange, jumpForce, ownerIndex, height, in ownerTileCosts, selfTag, isAttackable );  // tileIndexからZ軸方向へ+1
        }

        /// <summary>
        /// 指定のタイルから四方に向けて、攻撃可能なタイルを登録する処理を展開します
        /// </summary>
        /// <param name="dprtIndex">出発タイルインデックス</param>
        /// <param name="atkRange">攻撃可能範囲値</param>
        /// <param name="ownerTag">自身のキャラクタータグ</param>
        private void RegisterAttackableTilesAllSides( int dprtIndex, int targetTileIndex, int atkRange, CHARACTER_TAG ownerTag )
        {
            int colNum = _stageDataProvider.CurrentData.GridColumnNum;

            // 左端を除外
            if( targetTileIndex % colNum != 0 )
            {
                RegisterAttackableTiles( dprtIndex, targetTileIndex - 1, atkRange, ownerTag );  // targetTileIndexからX軸方向へ-1
            }
            // 右端を除外
            if( ( targetTileIndex + 1 ) % colNum != 0 )
            {
                RegisterAttackableTiles( dprtIndex, targetTileIndex + 1, atkRange, ownerTag );  // targetTileIndexからX軸方向へ+1
            }
            // Z軸方向への加算と減算はそのまま
            RegisterAttackableTiles( dprtIndex, targetTileIndex - colNum, atkRange, ownerTag ); // targetTileIndexからZ軸方向へ-1
            RegisterAttackableTiles( dprtIndex, targetTileIndex + colNum, atkRange, ownerTag ); // targetTileIndexからZ軸方向へ+1
        }

        /// <summary>
        /// 移動可能なグリッドを登録します
        /// </summary>
        /// <param name="tileIndex">登録対象のグリッドインデックス</param>
        /// <param name="moveRange">移動可能範囲値</param>
        /// <param name="atkRange">攻撃可能範囲値</param>
        /// <param name="jumpForce">ジャンプ値</param>
        /// <param name="ownerIndex">移動キャラクターのキャラクターインデックス</param>
        /// <param name="prevHeight">移動前のタイルの高さ</param>
        /// <param name="ownerTileCosts">各タイルの移動コスト(ステータス異常によって変化するためキャラ毎に個別)</param>
        /// <param name="selfTag">呼び出し元キャラクターのキャラクタータグ</param>
        /// <param name="isAttackable">呼び出し元のキャラクターが攻撃可能か否か</param>
        /// <param name="isDeparture">出発グリッドから呼び出されたか否か</param>
        private void RegisterMoveableTiles( int tileIndex, int moveRange, int atkRange, int jumpForce, int ownerIndex, float prevHeight, in int[] ownerTileCosts, CHARACTER_TAG selfTag, bool isAttackable )
        {
            var stageData = _stageDataProvider.CurrentData;
            int columnNum = stageData.GridColumnNum;

            // 範囲外のタイルは考慮しない
            if( tileIndex < 0 || stageData.GetTileTotalNum() <= tileIndex ) { return; }
            // 指定のタイル情報を取得
            var tileInfo = stageData.GetTileInfo( tileIndex );
            if( tileInfo == null ) { return; }
            // 移動不可のグリッドに辿り着いた場合は終了
            if( Methods.CheckBitFlag( tileInfo.flag, TileBitFlag.CANNOT_MOVE ) ) { return; }
            // 既に計算済みのグリッドであれば終了
            if( moveRange <= tileInfo.estimatedMoveRange ) { return; }
            // 自身に対する敵対勢力キャラクターが存在すれば終了
            TileBitFlag[] opponentTag = new TileBitFlag[( int ) CHARACTER_TAG.NUM]
            {
                TileBitFlag.ENEMY_EXIST | TileBitFlag.OTHER_EXIST,   // PLAYERにおける敵対勢力
                TileBitFlag.ALLY_EXIST  | TileBitFlag.OTHER_EXIST,    // ENEMYにおける敵対勢力
                TileBitFlag.ALLY_EXIST  | TileBitFlag.ENEMY_EXIST     // OTHERにおける敵対勢力
            };
            if( Methods.CheckBitFlag( tileInfo.flag, opponentTag[( int ) selfTag] ) ) { return; }

            // 直前のタイルとの高さの差分を求め、ジャンプ値と比較して移動可能かを判定する
            float curHeight = stageData.TileDatas[tileIndex].Height;
            int heightCost  = CalcurateHeightCost( prevHeight, curHeight, jumpForce );

            // 現在グリッドの移動抵抗値を更新( 出発グリッドではmoveRangeの値をそのまま適応する )
            int tileTypeIndex           = Convert.ToInt32( stageData.TileDatas[tileIndex].Type );
            int currentMoveRange        = moveRange - ownerTileCosts[tileTypeIndex] - heightCost;
            tileInfo.estimatedMoveRange = currentMoveRange;

            // 負の値であれば終了
            if( currentMoveRange < 0 ) { return; }
            // 攻撃範囲についても登録する
            if( isAttackable && ( tileInfo.charaTag == CHARACTER_TAG.NONE || tileInfo.charaIndex == ownerIndex ) )
            {
                BeginRegisterAttackableTiles( tileIndex, atkRange, selfTag, false );
            }

            RegisterMoveableTilesAllSides( tileIndex, currentMoveRange, atkRange, jumpForce, ownerIndex, curHeight, in ownerTileCosts, selfTag, isAttackable );
        }

        /// <summary>
        /// 攻撃可能なタイルを登録します
        /// </summary>
        /// <param name="dprtIndex">出発タイルインデックス</param>
        /// <param name="targetTileIndex">対象のグリッドインデックス</param>
        /// <param name="atkRange">攻撃可能範囲値</param>
        /// <param name="ownerTag">自身のキャラクタータグ</param>
        private void RegisterAttackableTiles( int dprtIndex, int targetTileIndex, int atkRange, CHARACTER_TAG ownerTag )
        {
            // 範囲外のグリッドは考慮しない
            var stageData = _stageDataProvider.CurrentData;
            if( !targetTileIndex.IsInHalfOpenRange( 0, stageData.GetTileTotalNum() ) ) { return; }
            // 移動不可のグリッドには攻撃できない
            if( Methods.CheckBitFlag( stageData.GetTileInfo( targetTileIndex ).flag, TileBitFlag.CANNOT_MOVE ) ) { return; }
            // 高低差が攻撃範囲を超過している場合は攻撃できない
            var dprtTileData    = stageData.GetTileData( dprtIndex );
            var targetTileData  = stageData.GetTileData( targetTileIndex );
            int diffHeight      = Convert.ToInt32( Mathf.Ceil( Mathf.Abs( targetTileData.Height - dprtTileData.Height ) ) );
            if( atkRange < diffHeight ) { return; }

            // 出発地点でなければ登録
            if( targetTileIndex != dprtIndex )
            {
                Methods.SetBitFlag( ref stageData.GetTileInfo( targetTileIndex ).flag, TileBitFlag.ATTACKABLE );
                var tileInfo = stageData.GetTileInfo( targetTileIndex );

                bool[] isMatch =
                {
                    (tileInfo.charaTag == CHARACTER_TAG.ENEMY || tileInfo.charaTag == CHARACTER_TAG.OTHER),     // PLAYER
                    (tileInfo.charaTag == CHARACTER_TAG.PLAYER || tileInfo.charaTag == CHARACTER_TAG.OTHER),    // ENEMY
                    (tileInfo.charaTag == CHARACTER_TAG.PLAYER || tileInfo.charaTag == CHARACTER_TAG.ENEMY)     // OTHER
                };

                if( isMatch[( int ) ownerTag] )
                {
                    Methods.SetBitFlag( ref stageData.GetTileInfo( dprtIndex ).flag, TileBitFlag.REACHABLE_ATTACK );
                    Methods.SetBitFlag( ref stageData.GetTileInfo( targetTileIndex ).flag, TileBitFlag.ATTACKABLE_TARGET_EXIST );
                }
            }

            if( --atkRange <= 0 ) { return; }   // 負の値であれば終了

            RegisterAttackableTilesAllSides( dprtIndex, targetTileIndex, atkRange, ownerTag );  // 現在のtargetTileIndexの地点から更に四方に展開
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

            float dprtHeight = _stageDataProvider.CurrentData.GetTileData( dprtIndex ).Height;
            float destHeight = _stageDataProvider.CurrentData.GetTileData( destIndex ).Height;

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