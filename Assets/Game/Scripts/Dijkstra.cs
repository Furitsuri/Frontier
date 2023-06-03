using System;
using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;

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
        for (int i = 0; i < n; i++) _graph[i] = new List<Edge>(8);
    }

    /// <summary>
    /// �ӂ�ǉ�
    /// </summary>
    /// <param name="a">�ڑ����̒��_</param>
    /// <param name="b">�ڑ���̒��_</param>
    /// <param name="cost">�R�X�g</param>
    public void Add(int a, int b, int cost = 1)
            => _graph[a].Add(new Edge(b, cost));

    /// <summary>
    /// �ŒZ�o�H�̃R�X�g���擾
    /// </summary>
    /// <param name="start">�J�n���_</param>
    public int[] GetMinCost(int start)
    {
        // �R�X�g���X�^�[�g���_�ȊO�𖳌����
        var cost = new int[N];
        for (int i = 0; i < N; i++) cost[i] = int.MaxValue;
        cost[start] = 0;

        // ���m��̒��_���i�[����D��x�t���L���[(�R�X�g���������قǗD��x������)
        var q = new Queue<Vertex>(256);   //= new PriorityQueue<Vertex>(N * 10, Comparer<Vertex>.Create((a, b) => b.CompareTo(a)));
        q.Enqueue(new Vertex(start, 0));

        while (q.Count > 0)
        {
            var v = q.Dequeue();

            // �L�^����Ă���R�X�g�ƈقȂ�(�R�X�g�����傫��)�ꍇ�͖���
            if (v.cost != cost[v.index]) continue;

            // ����m�肵�����_����Ȃ��钸�_�ɑ΂��čX�V���s��
            foreach (var e in _graph[v.index])
            {
                if (cost[e.to] > v.cost + e.cost)
                {
                    // ���ɋL�^����Ă���R�X�g��菬������΃R�X�g���X�V
                    cost[e.to] = v.cost + e.cost;
                    q.Enqueue(new Vertex(e.to, cost[e.to]));
                }
            }
        }

        // �m�肵���R�X�g��Ԃ�
        return cost;
    }

    public struct Edge
    {
        public int to;                      // �ڑ���̒��_
        public int cost;                   // �ӂ̃R�X�g

        public Edge(int to, int cost)
        {
            this.to = to;
            this.cost = cost;
        }
    }

    public struct Vertex : IComparable<Vertex>
    {
        public int index;                   // ���_�̔ԍ�
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
