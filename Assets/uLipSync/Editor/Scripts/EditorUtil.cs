using UnityEngine;
using UnityEditor;

namespace uLipSync
{

public static class EditorUtil
{
    public struct Margin
    {
        public float top;
        public float right;
        public float bottom;
        public float left;

        public Margin(float t, float r, float b, float l)
        {
            top = t;
            right = r;
            bottom = b;
            left = l;
        }
    }

    public static void DrawGrid(Rect area, Color axisColor, Color gridColor, Margin margin, Vector2 range, Vector2 div)
    {
        var origColor = Handles.color;

        float xMin = area.x + margin.left;
        float xMax = area.xMax - margin.right;
        float yMin = area.y + margin.top;
        float yMax = area.yMax - margin.bottom;
        float yRange = yMax - yMin;
        float xRange = xMax - xMin;

        // axes
        Handles.DrawSolidRectangleWithOutline(new Vector3[] 
        {
            new Vector2(xMin, yMin),
            new Vector2(xMax, yMin),
            new Vector2(xMax, yMax),
            new Vector2(xMin, yMax)
        }, new Color(0f, 0f, 0f, 0f), axisColor); 

        // grids
        Handles.color = gridColor;
        int subDiv = 5;
        for (int i = 1; i < div.y * subDiv; ++i) 
        {
            float y = yRange / (div.y * subDiv) * i;
            if (i % subDiv == 0)
            {
                Handles.DrawLine(
                    new Vector2(xMin, yMin + y),
                    new Vector2(xMax, yMin + y));
            }
            else
            {
                Handles.DrawDottedLine(
                    new Vector2(xMin, yMin + y),
                    new Vector2(xMax, yMin + y),
                    1f);
            }
        }

        for (int i = 1; i < div.x * subDiv; ++i) 
        {
            float x = xRange / (div.x * subDiv) * i;
            if (i % subDiv == 0)
            {
                Handles.DrawLine(
                    new Vector2(xMin + x, yMin),
                    new Vector2(xMin + x, yMax));
            }
            else
            {
                Handles.DrawDottedLine(
                    new Vector2(xMin + x, yMin),
                    new Vector2(xMin + x, yMax),
                    1f);
            }
        }

        for (int i = 0; i <= div.y; ++i) 
        {
            float y = yRange * ((div.y - i) / div.y);
            var rect = new Rect(area.x, area.y + y, margin.left, 20f);
            EditorGUI.LabelField(rect, (range.y * i / div.y).ToString());
        }

        for (int i = 0; i <= div.x; ++i) 
        {
            float x = margin.left + xRange * (i / div.x);
            var rect = new Rect(x, yMax, 40f, 20f);
            EditorGUI.LabelField(rect, (range.x * i / div.x).ToString());
        }

        Handles.color = origColor;
    }

    public static void DrawEllipse(Vector3 center, float rx, float ry, Rect area)
    {
        var points = new Vector3[64];
        var n = points.Length;
        for (int i = 0; i < n; ++i)
        {
            float ang = 2 * Mathf.PI * i / n;
            float x = rx * Mathf.Cos(ang);
            float y = ry * Mathf.Sin(ang);
            var pos = center + new Vector3(x, y, 0f);
            pos.x = Mathf.Max(Mathf.Min(pos.x, area.xMax), area.xMin);
            pos.y = Mathf.Max(Mathf.Min(pos.y, area.yMax), area.yMin);
            points[i] = pos;
        }
        Handles.DrawAAConvexPolygon(points);
    }
}

}
