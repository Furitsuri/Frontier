using System.Collections.Generic;
using UnityEngine;

namespace Frontier.Field
{
    /// <summary>
    /// FieldData のノード間経路を LineRenderer で描画する。
    /// FieldSceneController から Build() を呼び出す。
    /// </summary>
    public class FieldPathRenderer : MonoBehaviour
    {
        private const int BezierSegments = 20;

        [SerializeField] private Material _lineMaterial   = null;
        [SerializeField] private float    _lineWidth       = 0.1f;
        [SerializeField] private Color    _lineColor       = new Color(0.8f, 0.8f, 0.8f, 1f);

        private readonly List<LineRenderer> _lines = new List<LineRenderer>();

        public void Build(FieldData fieldData, Dictionary<int, Vector3> nodePositions)
        {
            Clear();

            // 重複描画を防ぐため描画済みペアを記録 (小さいId, 大きいId)
            var drawn = new HashSet<(int, int)>();

            foreach (var node in fieldData.Nodes)
            {
                if (!nodePositions.TryGetValue(node.Id, out var fromPos)) continue;

                if (node.PathToNext != null && node.PathToNext.Length > 0)
                {
                    foreach (var path in node.PathToNext)
                    {
                        var key = node.Id < path.ToId ? (node.Id, path.ToId) : (path.ToId, node.Id);
                        if (!drawn.Add(key)) continue;
                        if (!nodePositions.TryGetValue(path.ToId, out var toPos)) continue;

                        bool hasBezier = path.CtrlX != 0f || path.CtrlY != 0f;
                        var ctrl = hasBezier ? new Vector3(path.CtrlX, path.CtrlY, 0f) : Vector3.zero;
                        CreateLine(fromPos, toPos, hasBezier, ctrl);
                    }
                }
                else if (node.NextIds != null)
                {
                    foreach (var toId in node.NextIds)
                    {
                        var key = node.Id < toId ? (node.Id, toId) : (toId, node.Id);
                        if (!drawn.Add(key)) continue;
                        if (!nodePositions.TryGetValue(toId, out var toPos)) continue;

                        CreateLine(fromPos, toPos, false, Vector3.zero);
                    }
                }
            }
        }

        public void Clear()
        {
            foreach (var lr in _lines)
            {
                if (lr != null) Destroy(lr.gameObject);
            }
            _lines.Clear();
        }

        private void CreateLine(Vector3 from, Vector3 to, bool bezier, Vector3 ctrl)
        {
            var go = new GameObject("Path");
            go.transform.SetParent(transform, false);

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace    = true;
            lr.startWidth       = _lineWidth;
            lr.endWidth         = _lineWidth;
            lr.startColor       = _lineColor;
            lr.endColor         = _lineColor;
            lr.sortingOrder     = -1;

            if (_lineMaterial != null) lr.material = _lineMaterial;

            if (bezier)
            {
                var points = new Vector3[BezierSegments + 1];
                for (int i = 0; i <= BezierSegments; i++)
                {
                    float t = (float)i / BezierSegments;
                    points[i] = QuadBezier(from, ctrl, to, t);
                }
                lr.positionCount = points.Length;
                lr.SetPositions(points);
            }
            else
            {
                lr.positionCount = 2;
                lr.SetPosition(0, from);
                lr.SetPosition(1, to);
            }

            _lines.Add(lr);
        }

        private static Vector3 QuadBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }
    }
}
