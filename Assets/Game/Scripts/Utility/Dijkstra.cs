using System;
using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using Unity.VisualScripting;

public class Dijkstra
{
    public int N { get; }               // 頂点の数
    private List<Edge>[] _graph;        // グラフの辺のデータ

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="n">頂点数</param>
    public Dijkstra(int n)
    {
        N = n;
        _graph = new List<Edge>[n];
        for (int i = 0; i < n; i++) _graph[i] = new List<Edge>(Constants.NEIGHBORING_GRID_MAX_NUM);
    }

    /// <summary>
    /// 辺を追加
    /// </summary>
    /// <param name="a">接続元の頂点</param>
    /// <param name="b">接続先の頂点</param>
    /// <param name="cost">コスト</param>
    public void Add(int a, int b, int cost = 1)
            => _graph[a].Add(new Edge(a, b, cost));

    public List<WaypointInformation> GetMinRoute( int startIndex, int goalIndex, in List<int> pathList)
    {
        var List = new List<WaypointInformation>(Constants.DIJKSTRA_ROUTE_INDEXS_MAX_NUM);

        // コストをスタート頂点以外を無限大に
        var costInfo = new (int cost, int fromIndex)[N];   // item1 : 移動コスト item2 : item1におけるfromノードインデックス
        for (int i = 0; i < N; i++) costInfo[i] = (int.MaxValue, -1);
        costInfo[startIndex].cost = 0;

        // 未確定の頂点を格納するキュー
        var q = new Queue<Vertex>(256);
        q.Enqueue(new Vertex(startIndex, 0));

        while (q.Count > 0)
        {
            var v = q.Dequeue();

            // 記録されているコストと異なる(コストがより大きい)場合は無視
            if( v.cost > costInfo[v.index].cost ) { continue; }

            // 今回確定した頂点からつながる頂点に対して更新を行う
            foreach (var e in _graph[v.index])
            {
                if (costInfo[e.to].cost > v.cost + e.cost)
                {
                    // 既に記録されているコストより小さければコストを更新
                    costInfo[e.to] = (v.cost + e.cost, e.from);
                    q.Enqueue(new Vertex(e.to, costInfo[e.to].cost));
                }
            }
        }

        for( int i = goalIndex; i != startIndex; i = costInfo[i].fromIndex )
        {
            List.Add( new WaypointInformation( pathList[i], costInfo[i].cost ) );
        }
        List.Reverse();

        return List;
    }


    public struct Edge
    {
        public int from;                   // 接続元の頂点
        public int to;                     // 接続先の頂点
        public int cost;                   // 辺のコスト

        public Edge(int from, int to, int cost)
        {
            this.from   = from;
            this.to     = to;
            this.cost   = cost;
        }
    }

    public struct Vertex : IComparable<Vertex>
    {
        public int index;                  // 頂点の番号
        public int cost;                   // 記録したコスト

        public Vertex(int index, int cost)
        {
            this.index = index;
            this.cost = cost;
        }

        public int CompareTo(Vertex other)
            => cost.CompareTo(other.cost);
    }
}
