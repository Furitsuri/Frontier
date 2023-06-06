using System;
using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using Unity.VisualScripting;

public class Dijkstra
{
    public int N { get; }               // ���_�̐�
    private List<Edge>[] _graph;        // �O���t�̕ӂ̃f�[�^

    /// <summary>
    /// ������
    /// </summary>
    /// <param name="n">���_��</param>
    public Dijkstra(int n)
    {
        N = n;
        _graph = new List<Edge>[n];
        for (int i = 0; i < n; i++) _graph[i] = new List<Edge>(4);
    }

    /// <summary>
    /// �ӂ�ǉ�
    /// </summary>
    /// <param name="a">�ڑ����̒��_</param>
    /// <param name="b">�ڑ���̒��_</param>
    /// <param name="cost">�R�X�g</param>
    public void Add(int a, int b, int cost = 1)
            => _graph[a].Add(new Edge(a, b, cost));

    public List<int> GetMinRoute( int startIndex, int goalIndex )
    {
        var List = new List<int>(64);

        // �R�X�g���X�^�[�g���_�ȊO�𖳌����
        var cost = new (int, int)[N];   // item1 : �ړ��R�X�g item2 : item1�ɂ�����from�m�[�h�C���f�b�N�X
        for (int i = 0; i < N; i++) cost[i] = (int.MaxValue, -1);
        cost[startIndex].Item1 = 0;

        // ���m��̒��_���i�[����L���[
        var q = new Queue<Vertex>(256);
        q.Enqueue(new Vertex(startIndex, 0));

        while (q.Count > 0)
        {
            var v = q.Dequeue();

            // �L�^����Ă���R�X�g�ƈقȂ�(�R�X�g�����傫��)�ꍇ�͖���
            if (v.cost > cost[v.index].Item1) continue;

            // ����m�肵�����_����Ȃ��钸�_�ɑ΂��čX�V���s��
            foreach (var e in _graph[v.index])
            {
                if (cost[e.to].Item1 > v.cost + e.cost)
                {
                    // ���ɋL�^����Ă���R�X�g��菬������΃R�X�g���X�V
                    cost[e.to] = (v.cost + e.cost, e.from);
                    q.Enqueue(new Vertex(e.to, cost[e.to].Item1));
                }
            }
        }

        for( int i = goalIndex; i != startIndex; i = cost[i].Item2 )
        {
            List.Add(i);
        }
        List.Reverse();

        return List;
    }


    public struct Edge
    {
        public int from;                   // �ڑ����̒��_
        public int to;                     // �ڑ���̒��_
        public int cost;                   // �ӂ̃R�X�g

        public Edge(int from, int to, int cost)
        {
            this.from = from;
            this.to = to;
            this.cost = cost;
        }
    }

    public struct Vertex : IComparable<Vertex>
    {
        public int index;                  // ���_�̔ԍ�
        public int cost;                   // �L�^�����R�X�g

        public Vertex(int index, int cost)
        {
            this.index = index;
            this.cost = cost;
        }

        public int CompareTo(Vertex other)
            => cost.CompareTo(other.cost);
    }
}
