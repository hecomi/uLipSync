using UnityEngine;
using UnityEditor;
using System.Linq;

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

    public static string GetKey(string title, string category)
    {
        return $"{Common.assetName}-{category}-{title}";
    }

    private static string GetFoldOutKey(string title)
    {
        return GetKey(title, "FoldOut");
    }

    public static bool IsFoldOutOpened(string title)
    {
        return EditorPrefs.HasKey(GetFoldOutKey(title));
    }

    public static bool Foldout(string title, bool initialState)
    {
        var style = new GUIStyle("ShurikenModuleTitle");
        style.font = new GUIStyle(EditorStyles.label).font;
        style.border = new RectOffset(15, 7, 4, 4);
        style.fixedHeight = 22;
        style.contentOffset = new Vector2(20f, -2f);

        var key = GetFoldOutKey(title);
        bool display = EditorPrefs.GetBool(key, initialState);

        var rect = GUILayoutUtility.GetRect(16f, 22f, style);
        GUI.Box(rect, title, style);

        var e = Event.current;

        var toggleRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
        if (e.type == EventType.Repaint) 
        {
            EditorStyles.foldout.Draw(toggleRect, false, false, display, false);
        }

        if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition)) 
        {
            EditorPrefs.SetBool(key, !display);
            e.Use();
        }

        return display;
    }

    public static bool SimpleFoldout(string title, bool initialState)
    {
        var key = GetFoldOutKey(title);
        bool display = EditorPrefs.GetBool(key, initialState);
        bool newDisplay = EditorGUILayout.Foldout(display, title, EditorStyles.foldoutHeader);
        if (newDisplay != display) EditorPrefs.SetBool(key, newDisplay);
        return newDisplay;
    }

    public static T CreateAssetInRoot<T>(string name) where T : ScriptableObject
    {
        var path = AssetDatabase.GenerateUniqueAssetPath($"Assets/{name}.asset");
        var obj = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(obj, path);
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    public static T FindAsset<T>(string name) where T : ScriptableObject
    {
        var path = AssetDatabase.FindAssets(name)
            .Select(x => AssetDatabase.GUIDToAssetPath(x))
            .FirstOrDefault();
        return AssetDatabase.LoadAssetAtPath<T>(path);
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
            var rect = new Rect(margin.left / 2, area.y + y, 100f, 20f);
            EditorGUI.LabelField(rect, (range.y * i / div.y).ToString());
        }

        for (int i = 0; i <= div.x; ++i) 
        {
            float x = margin.left + xRange * (i / div.x);
            var rect = new Rect(x, yMax, 100f, 20f);
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

    public static void DrawFormants(Profile profile, LipSyncInfo result = null)
    {
        var origColor = Handles.color;

        var area = GUILayoutUtility.GetRect(Screen.width, 300f);
        area = EditorGUI.IndentedRect(area);
        var margin = new Margin(10, 10f, 30f, 40f);
        var range = new Vector2(1200f, 4000f);

        DrawGrid(
            area,
            Color.white, 
            new Color(1f, 1f, 1f, 0.5f), 
            margin,
            range,
            new Vector2(6f, 4f));

        if (!profile) return; 

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
            profile.formantA,
            profile.formantI,
            profile.formantU,
            profile.formantE,
            profile.formantO,
        };

        var vowelLabels = new string[]
        {
            "A",
            "I",
            "U",
            "E",
            "O",
        };

        float dx = width / range.x; 
        float dy = height / range.y;
        for (int i = 0; i < formants.Length; ++i)
        {
            var f = formants[i];
            float x = xMin + dx * f.f1;
            float y = yMin + (height - dy * f.f2);
            float rx = profile.maxError * dx;
            float ry = profile.maxError * dy;
            var center = new Vector3(x, y, 0f);
            var color = colors[i];
            Handles.color = color;
            Handles.DrawSolidDisc(center, Vector3.forward, 5f);
            float factor = result != null ? result.vowels[(Vowel)i] : 0f;
            color.a = Mathf.Lerp(0.15f, 0.5f, factor);
            Handles.color = color;
            DrawEllipse(center, rx, ry, new Rect(xMin, yMin, width, height));
            EditorGUI.LabelField(new Rect(x + 5f, y - 20f, 50f, 20f), vowelLabels[i]);
        }

        if (result != null)
        {
            float x = xMin + result.formant.f1 * dx;
            float y = yMin + (height - result.formant.f2 * dy);
            float size = Mathf.Lerp(2f, 20f, Mathf.Min(result.volume / 0.1f, 1f));
            var center = new Vector3(x, y, 0f);
            Handles.color = Color.white;
            Handles.DrawWireDisc(center, Vector3.forward, size);
        }

        Handles.color = origColor;
    }

    public static void DrawProperty(SerializedObject obj, string propName)
    {
        var prop = obj.FindProperty(propName);
        if (prop == null) return;
        EditorGUILayout.PropertyField(prop);
    }

    public static void DrawMicSelector(ref int index)
    {
        var mics = MicUtil.GetDeviceList();
        var micNames = mics.Select(x => x.name).ToArray();
        index = EditorGUILayout.Popup("Device", index, micNames);
    }
}

}
