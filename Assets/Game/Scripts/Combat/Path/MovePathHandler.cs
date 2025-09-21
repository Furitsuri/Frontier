using Frontier.Entities;
using Frontier.Stage;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

/// <summary>
/// タイル間を移動する際に求めるパスの作成・管理を行うハンドラです。
/// 複数キャラクター同時移動を行った際や突撃スキル使用時には各キャラクターでパスを計算、保持する必要があるため、
/// キャラクター1人1人が持つ設計となっています。
/// </summary>
public class MovePathHandler
{
    private StageController _stageCtrl  = null;

    // _proposedMovePathをトレースする際に用いるインデックス値
    private int _focusedWaypointIndex                   = 0;
    // このクラスの持ち主となるキャラクター
    private Character _owner                            = null;
    // pathの候補となるタイルインデックス値のリスト。逆に言えば、このリストに挿入されていないインデックスのタイルは通行しない
    private List<int> _candidateRouteIndexs             = null;
    // 計算から得られる移動パス
    private List<WaypointInformation> _proposedMovePath = new List<WaypointInformation>();

    public int FocusedWaypointIndex => _focusedWaypointIndex;
    public List<WaypointInformation> ProposedMovePath => _proposedMovePath;

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
        _focusedWaypointIndex = 0;
        _owner = Owner;

        if ( _candidateRouteIndexs == null )
        {
            _candidateRouteIndexs = new List<int>( _stageCtrl.GetTotalTileNum() );
        }
    }

    /// <summary>
    /// 目標とするタイルを更新するため、インデックスをインクリメントします
    /// </summary>
    public void IncrementFocusedWaypointIndex() { ++_focusedWaypointIndex; }

    /// <summary>
    /// 移動候補となるタイル設定を行います
    /// </summary>
    /// <param name="condition">
    /// 移動候補とする条件式。intは下記のfor文で使用するiに対応します。
    /// object[]に対し、呼び出し側で任意のパラメータを指定してください。
    /// </param>
    /// <param name="args">任意パラメータ。Characterの持つTmpParameterなどを指定して条件文を構成出来ます</param>
    public void SetUpCandidatePathIndexs( bool isReset,  Func<int, object[], bool> condition, params object[] args )
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

    /// <summary>
    /// 取得したパス上から、インデックスから参照されるタイルのインデックス値を取得します
    /// </summary>
    /// <returns>参照されるタイルのインデックス値</returns>
    public int GetFocusedWaypointIndex()
    {
        return _proposedMovePath[_focusedWaypointIndex].TileIndex;
    }

    /// <summary>
    /// pathのトレースが終了しているかを取得します
    /// </summary>
    /// <returns>トレースが終了したか否か</returns>
    public bool IsEndPathTrace()
    {
        return _proposedMovePath.Count <= _focusedWaypointIndex;
    }

    /// <summary>
    /// 出発地点と目標地点を指定して、最短移動ルートを取得します
    /// </summary>
    /// <param name="departingTileIndex">出発地点となるタイルのインデックス値</param>
    /// <param name="destinationlTileIndex">目標地点となるタイルのインデックス値</param>
    public bool FindMovePath( int departingTileIndex, int destinationlTileIndex, int jumpForce )
    {
        if ( _candidateRouteIndexs.Count <= 0 )
        {
            Debug.LogError( "_candidateRouteIndexs is not set up. Please check." );
            return false;
        }

        _proposedMovePath.Clear();

        var route = _stageCtrl.ExtractShortestPath( departingTileIndex, destinationlTileIndex, jumpForce, _candidateRouteIndexs );
        if( route == null ) { return false; }
        _proposedMovePath = route;

        if ( 0 < _proposedMovePath.Count )
        {
            _focusedWaypointIndex = 0;

            return true;
        }

        return false;
    }

    /// <summary>
    /// _ownerが移動可能、及び位置することが出来るタイルを考慮した上で、出発地点と目標地点を指定して最短移動ルートを取得します。
    /// 移動操作をしてる最中に移動ルートを探索するといった、動的な状況で使用します。
    /// </summary>
    /// <param name="departingTileIndex">出発地点のタイルインデックス</param>
    /// <param name="destinationlTileIndex">目標地点のタイルのインデックス</param>
    /// <returns>ルート取得の可否</returns>
    public bool FindActuallyMovePath( int departingTileIndex, int destinationlTileIndex, int jumpForce, bool isEndPathTrace )
    {
        if ( _candidateRouteIndexs.Count <= 0 )
        {
            Debug.LogError( "_candidateRouteIndexs is not set up. Please check." );
            return false;
        }

        // 指定のインデックス位置にキャラクターが留まれない場合は失敗
        if ( !CanStandOnTile( destinationlTileIndex ) ) { return false; }

        var route = _stageCtrl.ExtractShortestPath( departingTileIndex, destinationlTileIndex, jumpForce, _candidateRouteIndexs );
        if ( route == null ) { return false; }
        // 現在のパストレースが終了していない場合は、直近のwaypointを移動対象として先頭に登録
        if ( !isEndPathTrace )
        {
            _proposedMovePath.Insert( 0, _proposedMovePath[_focusedWaypointIndex]);
            // 先頭要素以外は削除
            if ( 1 < _proposedMovePath.Count ) { _proposedMovePath.RemoveRange( 1, _proposedMovePath.Count - 1 ); }
        }
        else { _proposedMovePath.Clear(); }
        _proposedMovePath.AddRange( route );    // 連結させることで、パストレースの終了の有無に関わらず新しいパスを作成

        if ( 0 < _proposedMovePath.Count )
        {
            _focusedWaypointIndex = 0;

            return true;
        }

        return false;
    }

    /// <summary>
    /// 渡されたパスの長さを指定の長さに調整し、保持します
    /// </summary>
    /// <param name="range">移動可能レンジ</param>
    /// <param name="adjustTargetPath">調整対象のパス</param>
    public void AdjustPathToRangeAndSet(  int range, in List<WaypointInformation> adjustTargetPath )
    {
        int prevCost            = 0;   // 下記routeCostは各インデックスまでの合計値コストなので、差分を得る必要がある
        int reachableTileIndex  = 0;

        // 得られたルートのうち、移動可能なインデックス値を辿る
        foreach ( WaypointInformation p in adjustTargetPath )
        {
            range -= ( p.MoveCost - prevCost );
            prevCost = p.MoveCost;

            // 移動レンジを超えれば終了
            if ( range < 0 ) { break; }
            // グリッド上にキャラクターが存在しないことを確認して更新
            if ( !_stageCtrl.GetTileInfo( p.TileIndex ).IsExistCharacter() ) { reachableTileIndex = p.TileIndex; }
        }

        // 目的地となるタイルのインデックス値より、後方のインデックス値のタイル情報をリストから削除
        int removeBaseIndex = adjustTargetPath.FindIndex(item => item.TileIndex == reachableTileIndex) + 1;
        int removeCount     = adjustTargetPath.Count - removeBaseIndex;
        adjustTargetPath.RemoveRange( removeBaseIndex, removeCount );

        _proposedMovePath = adjustTargetPath;
    }

    /// <summary>
    /// 指定のタイルの位置に留まることが出来るかを判定します
    /// </summary>
    /// <param name="tileIdx">指定のタイル位置のインデックス</param>
    /// <returns>留まることの可否</returns>
    public bool CanStandOnTile( int tileIdx )
    {
        var tileInfo    = _stageCtrl.GetTileInfo( tileIdx );
        bool ownerExist = ( tileInfo.charaTag == _owner.Params.CharacterParam.characterTag ) && ( tileInfo.charaIndex == _owner.Params.CharacterParam.characterIndex );

        return ( 0 <= tileInfo.estimatedMoveRange && ( ownerExist || !Methods.CheckBitFlag( tileInfo.flag, ImpassableFlag() ) ) );
    }

    /// <summary>
    /// 次の目標座標を取得します
    /// </summary>
    /// <returns>目標座標</returns>
    public TileInformation GetFocusedTileInformation()
    {
        return _stageCtrl.GetTileInfo( _proposedMovePath[_focusedWaypointIndex].TileIndex );
    }

    /// <summary>
    /// 通行不可となるタイルフラグ情報
    /// </summary>
    /// <returns></returns>
    static public TileBitFlag ImpassableFlag()
    {
        return TileBitFlag.CANNOT_MOVE | TileBitFlag.ALLY_EXIST | TileBitFlag.ENEMY_EXIST | TileBitFlag.OTHER_EXIST;
    }
}