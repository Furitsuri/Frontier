using Frontier.Battle;
using Frontier.Entities;
using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Stage
{
    /// <summary>
    /// タイル情報を管理します
    /// </summary>
    public class TileDataHandler
    {
        [Inject] private IStageDataProvider _stageDataProvider  = null;
        [Inject] private BattleRoutineController _btlRtnCtrl    = null;
        [Inject] private GridCursorController _gridCursorCtrl   = null;

        private List<int> _attackableTileIndexs = new List<int>();
        

        #region PUBLIC_METHOD

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
                    var tileIndex       = chara.Params.TmpParam.GetCurrentGridIndex();
                    var tileData        = _stageDataProvider.CurrentData.GetTileDynamicData( tileIndex );
                    tileData.CharaKey   = new CharacterKey( chara.Params.CharacterParam.characterTag, chara.Params.CharacterParam.characterIndex );
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tileDDatas"></param>
        /// <param name="dprtIdx"></param>
        /// <param name="mvRng"></param>
        /// <param name="jmp"></param>
        /// <param name="atkRng"></param>
        /// <param name="height"></param>
        /// <param name="tileCosts"></param>
        /// <param name="charaKey"></param>
        /// <param name="actionableTileMap"></param>
        public void BeginRegisterMoveableTiles( TileDynamicData[] tileDDatas, int dprtIdx, int mvRng, int jmp, int atkRng, float height, in int[] tileCosts, in CharacterKey charaKey, ref ActionableTileMap actionableTileMap )
        {
            Debug.Assert( dprtIdx.IsInHalfOpenRange( 0, tileDDatas.Length ), "Irregular Index." );

            if( mvRng < 0 ) { return; }

            tileDDatas[dprtIdx].EstimatedMoveRange = mvRng;
            actionableTileMap.AddMoveableTile( dprtIdx, tileDDatas[dprtIdx] );
            RegisterMoveableTilesAllSides( tileDDatas, dprtIdx, mvRng, jmp, atkRng, height, in tileCosts, in charaKey, ref actionableTileMap );
        }

        /// <summary>
        /// アクション(移動、攻撃)が可能な範囲を抽出します。
        /// 攻撃の範囲を不要にしたい場合は、atkRngの値を0以下に指定してください。
        /// </summary>
        /// <param name="dprtIdx"></param>
        /// <param name="mvRng"></param>
        /// <param name="jmp"></param>
        /// <param name="atkRng"></param>
        /// <param name="dprtHeight"></param>
        /// <param name="tileCosts"></param>
        /// <param name="charaKey"></param>
        /// <returns></returns>
        public void ExtractActionableRangeData( int dprtIdx, int mvRng, int jmp,  int atkRng, float dprtHeight, in int[] tileCosts, in CharacterKey charaKey, ref ActionableTileMap actionableTileMap )
        {
            // 基データを直接変更しないようにクローンを作成してから処理を行う
            var cloneStageDynamicDatas = _stageDataProvider.CurrentData.DeepCloneStageDynamicData();

            BeginRegisterAttackableTiles( cloneStageDynamicDatas, dprtIdx, atkRng, charaKey.CharacterTag, false, ref actionableTileMap );
            BeginRegisterMoveableTiles( cloneStageDynamicDatas, dprtIdx, mvRng, jmp, atkRng, dprtHeight, in tileCosts, in charaKey, ref actionableTileMap );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dprtIdx"></param>
        /// <param name="mvRng"></param>
        /// <param name="jmp"></param>
        /// <param name="atkRng"></param>
        /// <param name="dprtHeight"></param>
        /// <param name="tileCosts"></param>
        /// <param name="charaKey"></param>
        /// <returns></returns>
        public void ExtractAttackableData( int dprtIdx, int atkRng, in CharacterKey charaKey, ref ActionableTileMap actionableTileMap )
        {
            // 基データを直接変更しないようにクローンを作成してから処理を行う
            var cloneStageDynamicDatas = _stageDataProvider.CurrentData.DeepCloneStageDynamicData();

            BeginRegisterAttackableTiles( cloneStageDynamicDatas, dprtIdx, atkRng, charaKey.CharacterTag, false, ref actionableTileMap );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="actionableTileMap"></param>
        /// <param name="setup"></param>
        /// <param name="condition"></param>
        /// <param name="args"></param>
        public void ExtractMoveableRangeDataFilterByCondition( ref ActionableTileMap actionableTileMap, Func<TileDynamicData[]> setup, Func<TileDynamicData, object[], bool> condition, params object[] args )
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
        /// タイルへの攻撃可能情報の登録を開始します
        /// </summary>
        /// <param name="dprtIndex">攻撃キャラクターが存在するグリッドのインデックス値</param>
        /// <param name="atkRange">攻撃可能範囲値</param>
        /// <param name="selfTag">攻撃を行うキャラクター自身のキャラクタータグ</param>
        public void BeginRegisterAttackableTiles( TileDynamicData[] tileDDatas, int dprtIdx, int atkRng, CHARACTER_TAG charaTag, bool isClearAttackableInfo, ref ActionableTileMap actionableTileMap )
        {
            Debug.Assert( dprtIdx.IsInHalfOpenRange( 0, _stageDataProvider.CurrentData.GetTileTotalNum() ), "StageController : Irregular Index." );

            if( isClearAttackableInfo ) { ClearAttackableInformation(); }   // 全てのタイルの攻撃可否情報を初期化

            // 攻撃可否情報を各タイルに登録
            int targetTileIndex = dprtIdx;    // 開始時点では出発タイルと同じ
            RegisterAttackableTilesAllSides( tileDDatas, dprtIdx, targetTileIndex, atkRng, charaTag, ref actionableTileMap );
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
        /// 攻撃可能タイルのうち、攻撃可能キャラクターが存在するタイルを専用のリストに追加していきます
        /// </summary>
        /// <param name="owner">攻撃を行うキャラクター</param>
        /// <param name="target">予め攻撃対象が決まっている際に指定</param>
        /// <returns>攻撃可能キャラクターが存在している</returns>
        public bool CorrectAttackableTileIndexs( Character owner, Character target = null )
        {
            _gridCursorCtrl.ClearAtkTargetInfo();
            _attackableTileIndexs.Clear();

            // 攻撃可能、かつ攻撃対象となるキャラクターが存在するタイルのインデックス値をリストに登録
            foreach( var tileDData in owner.ActionRangeCtrl.ActionableTileMap.AttackableTileMap )
            {
                if( Methods.CheckBitFlag( tileDData.Value.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST ) )
                {
                    _attackableTileIndexs.Add( tileDData.Key );
                }
            }

            // グリッドカーソルの位置を、攻撃可能キャラクターが存在するタイル位置に設定
            if( 0 < _attackableTileIndexs.Count )
            {
                _gridCursorCtrl.SetAtkTargetNum( _attackableTileIndexs.Count );

                // 攻撃対象引数targetが定められている場合はその対象を探す
                if( target != null && 1 < _attackableTileIndexs.Count )
                {
                    for( int i = 0; i < _attackableTileIndexs.Count; ++i )
                    {
                        var tileData = _stageDataProvider.CurrentData.GetTile( _attackableTileIndexs[i] ).DynamicData();
                        if( target.CharaKey == tileData.CharaKey )
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
        public ( TileStaticData, TileDynamicData ) GetCurrentTileDatas()
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

            return ( _stageDataProvider.CurrentData.GetTileStaticData( index ), _stageDataProvider.CurrentData.GetTileDynamicData( index ) );
        }

        #endregion PUBLIC_METHOD

        #region PRIVATE_METHOD

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

        private void RegisterMoveableTilesAllSides( TileDynamicData[] tileDDatas, int tileIdx, int mvRng, int jmp, int atkRng,  float height, in int[] tileCosts, in CharacterKey charaKey, ref ActionableTileMap actionableTileMap )
        {
            int colNum = _stageDataProvider.CurrentData.TileColNum;

            // 左端を除外
            if( tileIdx % colNum != 0 )
            {
                RegisterMoveableTiles( tileDDatas, tileIdx - 1, mvRng, jmp, atkRng, height, in tileCosts, in charaKey, ref actionableTileMap );   // tileIndexからX軸方向へ-1
            }
            // 右端を除外
            if( ( tileIdx + 1 ) % colNum != 0 )
            {
                RegisterMoveableTiles( tileDDatas, tileIdx + 1, mvRng, jmp, atkRng, height, in tileCosts, in charaKey, ref actionableTileMap );   // tileIndexからX軸方向へ+1
            }
            // Z軸方向への加算と減算はそのまま
            RegisterMoveableTiles( tileDDatas, tileIdx - colNum, mvRng, jmp, atkRng, height, in tileCosts, in charaKey, ref actionableTileMap );  // tileIndexからZ軸方向へ-1
            RegisterMoveableTiles( tileDDatas, tileIdx + colNum, mvRng, jmp, atkRng, height, in tileCosts, in charaKey, ref actionableTileMap );  // tileIndexからZ軸方向へ+1
        }

        private void RegisterAttackableTilesAllSides( TileDynamicData[] tileDDatas, int dprtIdx, int tgtTileIdx, int atkRng, CHARACTER_TAG charaTag, ref ActionableTileMap actionableTileMap )
        {
            int colNum = _stageDataProvider.CurrentData.TileColNum;

            // 左端を除外
            if( tgtTileIdx % colNum != 0 )
            {
                RegisterAttackableTiles( tileDDatas, dprtIdx, tgtTileIdx - 1, atkRng, charaTag, ref actionableTileMap );  // tgtTileIdxからX軸方向へ-1
            }
            // 右端を除外
            if( ( tgtTileIdx + 1 ) % colNum != 0 )
            {
                RegisterAttackableTiles( tileDDatas, dprtIdx, tgtTileIdx + 1, atkRng, charaTag, ref actionableTileMap );  // tgtTileIdxからX軸方向へ+1
            }
            // Z軸方向への加算と減算はそのまま
            RegisterAttackableTiles( tileDDatas, dprtIdx, tgtTileIdx - colNum, atkRng, charaTag, ref actionableTileMap ); // tgtTileIdxからZ軸方向へ-1
            RegisterAttackableTiles( tileDDatas, dprtIdx, tgtTileIdx + colNum, atkRng, charaTag, ref actionableTileMap ); // targetTileIndexからZ軸方向へ+1
        }

        private void RegisterMoveableTiles( TileDynamicData[] tileDDatas, int tileIdx, int mvRng, int jmp, int atkRng, float prevHeight, in int[] tileCosts, in CharacterKey charaKey, ref ActionableTileMap actionableTileMap )
        {
            int columnNum = _stageDataProvider.CurrentData.TileColNum;

            // 範囲外のタイルは考慮しない
            if( tileIdx < 0 || tileDDatas.Length <= tileIdx ) { return; }
            // 指定のタイル情報を取得
            var tileDData = tileDDatas[ tileIdx ];
            // 移動不可のグリッドに辿り着いた場合は終了
            if( Methods.CheckBitFlag( tileDData.Flag, TileBitFlag.CANNOT_MOVE ) ) { return; }
            // 既に計算済みのグリッドであれば終了
            if( mvRng <= tileDData.EstimatedMoveRange ) { return; }
            // 自身における敵対勢力キャラクターが存在すれば終了
            if( Character.IsOpponentFaction[Convert.ToInt32( charaKey.CharacterTag )]( tileDData.CharaKey.CharacterTag ) ) { return; }

            // 直前のタイルとの高さの差分を求め、ジャンプ値と比較して移動可能かを判定する
            var staticData  = _stageDataProvider.CurrentData.GetTileStaticData( tileIdx );
            float curHeight = staticData.Height;
            int heightCost  = CalcurateHeightCost( prevHeight, curHeight, jmp );

            // 現在グリッドの移動抵抗値を更新( 出発グリッドではmoveRangeの値をそのまま適応する )
            int tileTypeIndex           = Convert.ToInt32( staticData.TileType );
            int currentMoveRange        = mvRng - tileCosts[tileTypeIndex] - heightCost;
            tileDData.EstimatedMoveRange = currentMoveRange;

            // 負の値であれば終了
            if( currentMoveRange < 0 ) { return; }
            // 0以上(通行可能)である場合は登録
            else { actionableTileMap.AddMoveableTile( tileIdx, tileDDatas[tileIdx] ); }

            // 攻撃範囲についても登録する
            if( ( 0 < atkRng ) && ( !tileDData.CharaKey.IsValid() || tileDData.CharaKey == charaKey ) )
            {
                BeginRegisterAttackableTiles( tileDDatas, tileIdx, atkRng, charaKey.CharacterTag, false, ref actionableTileMap );
            }

            RegisterMoveableTilesAllSides( tileDDatas, tileIdx, currentMoveRange, jmp, atkRng, curHeight, in tileCosts, in charaKey, ref actionableTileMap );
        }

        /// <summary>
        /// 攻撃可能なタイルを登録します
        /// </summary>
        /// <param name="dprtIndex">出発タイルインデックス</param>
        /// <param name="targetTileIndex">対象のグリッドインデックス</param>
        /// <param name="atkRange">攻撃可能範囲値</param>
        /// <param name="ownerTag">自身のキャラクタータグ</param>
        private void RegisterAttackableTiles( TileDynamicData[] tileDDatas, int dprtIdx, int tgtTileIdx, int atkRng, CHARACTER_TAG charaTag, ref ActionableTileMap actionableTileMap )
        {
            // 範囲外のグリッドは考慮しない
            if( !tgtTileIdx.IsInHalfOpenRange( 0, tileDDatas.Length ) ) { return; }
            // 移動不可のグリッドには攻撃できない
            if( Methods.CheckBitFlag( tileDDatas[ tgtTileIdx ].Flag, TileBitFlag.CANNOT_MOVE ) ) { return; }
            // 高低差が攻撃範囲を超過している場合は攻撃できない
            var dprtTileData    = _stageDataProvider.CurrentData.GetTileStaticData( dprtIdx );
            var targetTileData  = _stageDataProvider.CurrentData.GetTileStaticData( tgtTileIdx );
            int diffHeight      = Convert.ToInt32( Mathf.Ceil( Mathf.Abs( targetTileData.Height - dprtTileData.Height ) ) );
            if( atkRng < diffHeight ) { return; }

            // 出発地点でなければ登録
            if( tgtTileIdx != dprtIdx )
            {
                var tgtTileData = tileDDatas[tgtTileIdx];
                Methods.SetBitFlag( ref tgtTileData.Flag, TileBitFlag.ATTACKABLE ); // 攻撃可能地点であることをフラグに記述

                // tgtTileに攻撃対象となるキャラクターがいれば、そのこともフラグに記述
                if( Character.IsOpponentFaction[Convert.ToInt32( charaTag )]( tgtTileData.CharaKey.CharacterTag ) )
                {
                    Methods.SetBitFlag( ref tileDDatas[dprtIdx].Flag, TileBitFlag.REACHABLE_ATTACK );   // dprtIdxであれば攻撃対象へ攻撃可能であることを記述
                    Methods.SetBitFlag( ref tgtTileData.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST );    // tgtTileに攻撃可能な攻撃対象がいることを記述
                }

                actionableTileMap.AddAttackableTile( tgtTileIdx, tgtTileData );     // 登録
            }

            if( --atkRng <= 0 ) { return; }   // 負の値であれば終了

            RegisterAttackableTilesAllSides( tileDDatas, dprtIdx, tgtTileIdx, atkRng, charaTag, ref actionableTileMap );  // 現在のtargetTileIndexの地点から更に四方に展開
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