using UnityEngine;
using UnityEditor;

namespace uLipSync
{

[CustomEditor(typeof(LipSync))]
public class LipSyncEditor : Editor
{
    LipSync lipSync { get { return target as LipSync; } }
    Config config { get { return lipSync.config; } }

    internal struct Margin
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

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (!lipSync) return;

        DrawFormants();
        DrawLPCSpectralEnvelope();
    }

    void DrawGrid(Rect area, Color axisColor, Color gridColor, Margin margin, Vector2 range, Vector2 div)
    {
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
    }

    void DrawFormants()
    {
        var area = GUILayoutUtility.GetRect(Screen.width, 400f);
        var margin = new Margin(10, 10f, 30f, 40f);
        var range = new Vector2(1000f, 3000f);

        DrawGrid(
            area,
            Color.white, 
            new Color(1f, 1f, 1f, 0.5f), 
            margin,
            range,
            new Vector2(5f, 3f));

        if (!config) return; 

        float xMin = area.x + margin.left;
        float xMax = area.xMax - margin.right;
        float yMin = area.y + margin.top;
        float yMax = area.yMax - margin.bottom;
        float width = xMax - xMin;
        float height = yMax - yMin;

        var colors = new Color[] 
        {
            new Color(1f, 0f, 0f, 1f),
            new Color(0f, 1f, 0f, 1f),
            new Color(0f, 0f, 1f, 1f),
            new Color(1f, 1f, 0f, 1f),
            new Color(0f, 1f, 1f, 1f),
        };

        var formants = new FormantPair[]
        {
            config.formantA,
            config.formantI,
            config.formantU,
            config.formantE,
            config.formantO,
        };

        var vowels = new string[]
        {
            "A",
            "I",
            "U",
            "E",
            "O",
        };

        for (int i = 0; i < formants.Length; ++i)
        {
            var f = formants[i];
            float dx = width / range.x; 
            float dy = height / range.y;
            float x = xMin + dx * f.f1;
            float y = yMin + (height - dy * f.f2);
            float rx = config.maxError * dx;
            float ry = config.maxError * dy;
            var origColor = Handles.color;
            var center = new Vector3(x, y, 0f);
            var color = colors[i];
            Handles.color = color;
            Handles.DrawSolidDisc(center, Vector3.forward, 5f);
            color.a = 0.15f;
            Handles.color = color;
            DrawEllipse(center, rx, ry, new Rect(xMin, yMin, width, height));
            EditorGUI.LabelField(new Rect(x + 5f, y - 20f, 20f, 20f), vowels[i]);
            Handles.color = origColor;
        }
    }

    void DrawEllipse(Vector3 center, float rx, float ry, Rect area)
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

    void DrawLPCSpectralEnvelope()
    {
        var area = GUILayoutUtility.GetRect(Screen.width, 400f);
        var margin = new Margin(10, 10f, 30f, 40f);
        var range = new Vector2(3000f, 10f);

        DrawGrid(
            area,
            Color.white, 
            new Color(1f, 1f, 1f, 0.5f), 
            margin,
            range,
            new Vector2(3f, 1f));

        var H = lipSync.editorOnlyHForDebug;
        if (H == null) return;

        float xMin = area.x + margin.left;
        float xMax = area.xMax - margin.right;
        float yMin = area.y + margin.top;
        float yMax = area.yMax - margin.bottom;
        float width = xMax - xMin;
        float height = yMax - yMin;

        float df = lipSync.deltaFreq;
        int n = Mathf.CeilToInt(range.x / df);
        var points = new Vector3[n];
        float min = Mathf.Log10(1e-3f);
        for (int i = 0; i < n && i < H.Length; ++i)
        {
            float val = H[i];
            val = Mathf.Log10(10f * val);
            val = (val - min) / (1f - min);
            val = Mathf.Max(val, 0f);
            float x = xMin + width * i / (n - 1);
            float y = yMax - height * val;
            points[i] = new Vector3(x, y, 0f);
        }

        var origColor = Handles.color;
        Handles.color = Color.red;
        Handles.DrawAAPolyLine(5f, points);
        Handles.color = origColor;
    }
}

}
