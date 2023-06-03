using System;
using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;

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
        for (int i = 0; i < n; i++) _graph[i] = new List<Edge>(8);
    }

    /// <summary>
    /// 辺を追加
    /// </summary>
    /// <param name="a">接続元の頂点</param>
    /// <param name="b">接続先の頂点</param>
    /// <param name="cost">コスト</param>
    public void Add(int a, int b, int cost = 1)
            => _graph[a].Add(new Edge(b, cost));

    /// <summary>
    /// 最短経路のコストを取得
    /// </summary>
    /// <param name="start">開始頂点</param>
    public int[] GetMinCost(int start)
    {
        // コストをスタート頂点以外を無限大に
        var cost = new int[N];
        for (int i = 0; i < N; i++) cost[i] = int.MaxValue;
        cost[start] = 0;

        // 未確定の頂点を格納する優先度付きキュー(コストが小さいほど優先度が高い)
        var q = new Queue<Vertex>(256);   //= new PriorityQueue<Vertex>(N * 10, Comparer<Vertex>.Create((a, b) => b.CompareTo(a)));
        q.Enqueue(new Vertex(start, 0));

        while (q.Count > 0)
        {
            var v = q.Dequeue();

            // 記録されているコストと異なる(コストがより大きい)場合は無視
            if (v.cost != cost[v.index]) continue;

            // 今回確定した頂点からつながる頂点に対して更新を行う
            foreach (var e in _graph[v.index])
            {
                if (cost[e.to] > v.cost + e.cost)
                {
                    // 既に記録されているコストより小さければコストを更新
                    cost[e.to] = v.cost + e.cost;
                    q.Enqueue(new Vertex(e.to, cost[e.to]));
                }
            }
        }

        // 確定したコストを返す
        return cost;
    }

    public struct Edge
    {
        public int to;                      // 接続先の頂点
        public int cost;                   // 辺のコスト

        public Edge(int to, int cost)
        {
            this.to = to;
            this.cost = cost;
        }
    }

    public struct Vertex : IComparable<Vertex>
    {
        public int index;                   // 頂点の番号
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
