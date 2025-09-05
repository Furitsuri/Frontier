using Frontier.Entities;
using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using Zenject;

public class MovePathHandler
{
    private StageController _stageCtrl  = null;

    private int _nextTileIndex                                          = 0;
    private Character _owner                                            = null;
    private List<int> _candidateRouteIndexs                             = null;
    private List<(int routeIndex, int routeCost)> _proposedMoveRoute    = null;
    private List<Vector3> _moveRoutePositions                           = new List<Vector3>();

    public int NextTileIndex => _nextTileIndex;
    public List<Vector3> MoveRoutePositions => _moveRoutePositions;

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
    public void SetUpCandidateRouteIndexs()
    {
        _candidateRouteIndexs.Clear();  // 一度クリア

        // 通行不可フラグ
        var impassableFlag = StageController.BitFlag.CANNOT_MOVE | StageController.BitFlag.ALLY_EXIST | StageController.BitFlag.ENEMY_EXIST | StageController.BitFlag.OTHER_EXIST;

        // 進行可能なタイルをルート候補に挿入
        for ( int i = 0; i < _stageCtrl.GetTotalTileNum(); ++i )
        {
            var tileInfo    = _stageCtrl.GetGridInfo( i );
            // 味方が存在していても自身は除く
            bool ownerExist = ( tileInfo.charaTag == _owner.Params.CharacterParam.characterTag ) && ( tileInfo.charaIndex == _owner.Params.CharacterParam.characterIndex );

            if ( 0 <= tileInfo.estimatedMoveRange && ( ownerExist || !Methods.CheckBitFlag( tileInfo.flag, impassableFlag ) ) )
            {
                _candidateRouteIndexs.Add( i );
            }
        }
    }

    /// <summary>
    /// 移動する各タイル上の座標情報を設定します
    /// </summary>
    public void SetUpRoutePositions()
    {
        // 各インスタンスを初期化
        _nextTileIndex = 0;
        _moveRoutePositions.Clear();

        // パスのインデックスからグリッド座標を得る
        for ( int i = 0; i < _proposedMoveRoute.Count; ++i )
        {
            _moveRoutePositions.Add( _stageCtrl.GetGridInfo( _proposedMoveRoute[i].routeIndex ).charaStandPos );
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
    public bool CalcurateMovePathRoute( int departingTileIndex, int destinationlTileIndex )
    {
        if ( departingTileIndex == destinationlTileIndex )
        {
            _proposedMoveRoute = null;
            return false;
        }

        _proposedMoveRoute = _stageCtrl.ExtractShortestRouteIndexs( departingTileIndex, destinationlTileIndex, _candidateRouteIndexs );

        return ( _proposedMoveRoute != null && 0 < _proposedMoveRoute.Count );
    }

    /// <summary>
    /// 指定の
    /// </summary>
    /// <param name="tileIdx"></param>
    /// <returns></returns>
    public bool IsPassableTile( int tileIdx )
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
        return _moveRoutePositions[_nextTileIndex];
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