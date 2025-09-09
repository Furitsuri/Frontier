using Frontier.Entities;
using Frontier.Stage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEditor.SceneManagement;
using UnityEngine;
using Zenject;

public class MovePathHandler
{
    private StageController _stageCtrl  = null;

    private int _nextTileIndex                                                              = 0;
    private Character _owner                                                                = null;
    private List<int> _candidateRouteIndexs                                                 = null;
    private List<(int routeIndex, int routeCost, Vector3 tilePosition)> _proposedMoveRoute  = new List<(int routeIndex, int routeCost, Vector3 tilePosition)>();

    public int NextTileIndex => _nextTileIndex;
    public List<(int routeIndex, int routeCost, Vector3 tilePosition)> ProposedMoveRoute => _proposedMoveRoute;

    [Inject]
    private void Construct( StageController stageCtrl )
    {
        _stageCtrl = stageCtrl;
    }

    /// <summary>
    /// 初期化します
    /// </summary>
    public void Init( Character Owner )
    {
        _nextTileIndex = 0;
        _owner = Owner;

        if ( _candidateRouteIndexs == null )
        {
            _candidateRouteIndexs = new List<int>( _stageCtrl.GetTotalTileNum() );
        }
    }

    /// <summary>
    /// 目標とするタイルを更新するため、インデックスをインクリメントします
    /// </summary>
    public void IncrementNextTileIndex() { ++_nextTileIndex; }

    /// <summary>
    /// 移動候補となるタイル設定を行います
    /// </summary>
    /// <param name="condition">
    /// 移動候補とする条件式。intは下記のfor文で使用するiに対応します。
    /// object[]に対し、呼び出し側で任意のパラメータを指定してください。
    /// </param>
    /// <param name="args">任意パラメータ。Characterの持つTmpParameterなどを指定して条件文を構成出来ます</param>
    public void SetUpCandidateRouteIndexs( bool isReset,  Func<int, object[], bool> condition, params object[] args )
    {
        if ( isReset ) { _candidateRouteIndexs.Clear(); }  // 一度クリア

        // 進行可能なタイルをルート候補に挿入
        for ( int i = 0; i < _stageCtrl.GetTotalTileNum(); ++i )
        {
            if ( condition( i, args ) )  // 条件を呼び出し側で設定
            {
                _candidateRouteIndexs.Add( i );
            }
        }
    }

    public int GetNextRouteIndex()
    {
        return _proposedMoveRoute[_nextTileIndex].routeIndex;
    }

    /// <summary>
    /// 出発地点と目標地点を指定して、最短移動ルートを取得します
    /// </summary>
    /// <param name="departingTileIndex">出発地点となるタイルのインデックス値</param>
    /// <param name="destinationlTileIndex">目標地点となるタイルのインデックス値</param>
    public bool FindMoveRoute( int departingTileIndex, int destinationlTileIndex )
    {
        _proposedMoveRoute.Clear();

        var route = _stageCtrl.ExtractShortestRoute( departingTileIndex, destinationlTileIndex, _candidateRouteIndexs );
        if( route == null ) { return false; }
        _proposedMoveRoute = route;

        if ( 0 < _proposedMoveRoute.Count )
        {
            _nextTileIndex = 0;

            return true;
        }

        return false;
    }

    /// <summary>
    /// 探索済みのルートをリセットせず、移動可能範囲を考慮した上で、出発地点と目標地点を指定して最短移動ルートを取得します
    /// 移動操作中に移動ルートを探索する際に使用します
    /// </summary>
    /// <param name="departingTileIndex">出発地点のタイルインデックス</param>
    /// <param name="destinationlTileIndex">目標地点のタイルのインデックス</param>
    /// <returns>ルート取得の是非</returns>
    public bool FindActuallyMoveRoute( int departingTileIndex, int destinationlTileIndex )
    {
        // 指定のインデックス位置にキャラクターが留まれない場合は失敗
        if ( !CanStandOnTile( destinationlTileIndex ) ) { return false; }

        var route = _stageCtrl.ExtractShortestRoute( departingTileIndex, destinationlTileIndex, _candidateRouteIndexs );
        if ( route == null ) { return false; }
        _proposedMoveRoute = route;

        if ( 0 < _proposedMoveRoute.Count )
        {
            _nextTileIndex = 0;

            return true;
        }

        return false;
    }

    /// <summary>
    /// 目標地点のタイルに対し、移動出来るタイルのうち、最も近い位置のタイルへのパスを取得します
    /// </summary>
    /// <param name="departingTileIndex">出発地点のタイルインデックス</param>
    /// <param name="destinationlTileIndex">目標地点のタイルのインデックス</param>
    /// <param name="moveRange">移動可能レンジ</param>
    /// <param name="outReachableTileIndex">目標地点に対し、移動可能な範囲の中で最も近い位置のタイルのインデックス値</param>
    /// <returns>ルート取得の是非</returns>
    public bool FindNearestReachableTileRoute( int departingTileIndex, int destinationlTileIndex, int moveRange )
    {
        if ( !FindMoveRoute( departingTileIndex, destinationlTileIndex ) ) {  return false; }

        int prevCost            = 0;   // 下記routeCostは各インデックスまでの合計値コストなので、差分を得る必要がある
        int reachableTileIndex  = 0;

        // 得られたルートのうち、移動可能なインデックス値を辿る
        foreach ( (int routeIndex, int routeCost, Vector3 t) r in _proposedMoveRoute )
        {
            moveRange -= ( r.routeCost - prevCost );
            prevCost = r.routeCost;

            // 移動レンジを超えれば終了
            if ( moveRange < 0 ) { break; }
            // グリッド上にキャラクターが存在しないことを確認して更新
            if ( !_stageCtrl.GetGridInfo( r.routeIndex ).IsExistCharacter() ) { reachableTileIndex = r.routeIndex; }
        }

        // 目的地となるタイルのインデックス値より、後方のインデックス値のタイル情報をリストから削除
        int removeBaseIndex = _proposedMoveRoute.FindIndex(item => item.routeIndex == reachableTileIndex) + 1;
        int removeCount     = _proposedMoveRoute.Count - removeBaseIndex;
        _proposedMoveRoute.RemoveRange( removeBaseIndex, removeCount );

        return true;
    }

    /// <summary>
    /// 指定のタイルの位置に留まることが出来るかを判定します
    /// </summary>
    /// <param name="tileIdx">指定のタイル位置のインデックス</param>
    /// <returns>留まることの可否</returns>
    public bool CanStandOnTile( int tileIdx )
    {
        var tileInfo    = _stageCtrl.GetGridInfo( tileIdx );
        bool ownerExist = ( tileInfo.charaTag == _owner.Params.CharacterParam.characterTag ) && ( tileInfo.charaIndex == _owner.Params.CharacterParam.characterIndex );

        return ( 0 <= tileInfo.estimatedMoveRange && ( ownerExist || !Methods.CheckBitFlag( tileInfo.flag, ImpassableFlag() ) ) );
    }

    /// <summary>
    /// 次の目標座標を取得します
    /// </summary>
    /// <returns>目標座標</returns>
    public Vector3 GetNextTilePosition()
    {
        return _proposedMoveRoute[_nextTileIndex].tilePosition;
    }

    /// <summary>
    /// 通行不可となるタイルフラグ情報
    /// </summary>
    /// <returns></returns>
    static public StageController.BitFlag ImpassableFlag()
    {
        return StageController.BitFlag.CANNOT_MOVE | StageController.BitFlag.ALLY_EXIST | StageController.BitFlag.ENEMY_EXIST | StageController.BitFlag.OTHER_EXIST;
    }
}