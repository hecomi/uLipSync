using UnityEngine;
using UnityEditor;
using System.Linq;

namespace uLipSync
{

[CustomEditor(typeof(uLipSync))]
public class uLipSyncEditor : Editor
{
    uLipSync lipSync { get { return target as uLipSync; } }
    Profile profile { get { return lipSync.profile; } }

    void OnEnable()
    {
        if (lipSync.config == null)
        {
            var configPath = AssetDatabase.FindAssets("Default-Config")
                .Select(x => AssetDatabase.GUIDToAssetPath(x))
                .FirstOrDefault();
            lipSync.config = AssetDatabase.LoadAssetAtPath<Config>(configPath);
        }

        if (lipSync.profile == null)
        {
            var profilePath = AssetDatabase.FindAssets("Profile-Man")
                .Select(x => AssetDatabase.GUIDToAssetPath(x))
                .FirstOrDefault();
            lipSync.profile = AssetDatabase.LoadAssetAtPath<Profile>(profilePath);
        }
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (!lipSync) return;

        lipSync.foldOutVisualizer = EditorGUILayout.Foldout(lipSync.foldOutVisualizer, "Visualizer");
        if (lipSync.foldOutVisualizer)
        {
            DrawFormants();
            DrawLPCSpectralEnvelope();
        }
    }

    void DrawFormants()
    {
        var origColor = Handles.color;

        var area = GUILayoutUtility.GetRect(Screen.width, 400f);
        var margin = new EditorUtil.Margin(10, 10f, 30f, 40f);
        var range = new Vector2(1200f, 4000f);

        EditorUtil.DrawGrid(
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

        var result = lipSync.result;
        int vowelIndex = (int)LipSyncUtil.GetVowel(result.formant, profile).vowel;

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
            color.a = (i == vowelIndex) ? 0.5f : 0.15f;
            Handles.color = color;
            DrawEllipse(center, rx, ry, new Rect(xMin, yMin, width, height));
            EditorGUI.LabelField(new Rect(x + 5f, y - 20f, 20f, 20f), vowelLabels[i]);
        }

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
        var origColor = Handles.color;

        var area = GUILayoutUtility.GetRect(Screen.width, 400f);
        var margin = new EditorUtil.Margin(10, 10f, 30f, 40f);
        var range = new Vector2(4000f, 1f);

        EditorUtil.DrawGrid(
            area,
            Color.white, 
            new Color(1f, 1f, 1f, 0.5f), 
            margin,
            range,
            new Vector2(8f, 1f));

        if (!Application.isPlaying) return;

        float xMin = area.x + margin.left;
        float xMax = area.xMax - margin.right;
        float yMin = area.y + margin.top;
        float yMax = area.yMax - margin.bottom;
        float width = xMax - xMin;
        float height = yMax - yMin;

        var H = lipSync.lpcSpectralEnvelopeForEditorOnly;
        float maxH = Algorithm.GetMaxValue(ref H);
        float df = AudioSettings.outputSampleRate / H.Length;
        int n = Mathf.CeilToInt(range.x / df);
        var points = new Vector3[n];
        float min = Mathf.Log10(1e-3f);
        for (int i = 0; i < n && i < H.Length; ++i)
        {
            float val = H[i] / maxH;
            val = Mathf.Log10(10f * val);
            val = (val - min) / (1f - min);
            val = Mathf.Max(val, 0f);
            float x = xMin + width * i / (n - 1);
            float y = yMax - height * val;
            points[i] = new Vector3(x, y, 0f);
        }

        Handles.color = Color.red;
        Handles.DrawAAPolyLine(5f, points);

        var ddH = lipSync.ddLpcSpectralEnvelopeForEditorOnly;
        float maxDdH = Algorithm.GetMaxValue(ref ddH);
        min = Mathf.Log10(1e-8f);
        for (int i = 0; i < n && i < ddH.Length; ++i)
        {
            float val = ddH[i] / maxDdH;
            val = Mathf.Log10(10f * val);
            val = (val - min) / (1f - min);
            val = Mathf.Max(val, 0f);
            float x = xMin + width * i / (n - 1);
            float y = yMax - height * val;
            points[i] = new Vector3(x, y, 0f);
        }

        Handles.color = new Color(0f, 0f, 1f, 0.2f);
        Handles.DrawAAPolyLine(3f, points);

        {
            var result = lipSync.result;
            float xF1 = xMin + width * (result.formant.f1 / df) / n;
            float xF2 = xMin + width * (result.formant.f2 / df) / n;
            var pointsF1 = new Vector3[2] { new Vector3(xF1, yMin, 0f), new Vector3(xF1, yMax, 0f) };
            var pointsF2 = new Vector3[2] { new Vector3(xF2, yMin, 0f), new Vector3(xF2, yMax, 0f) };
            Handles.color = Color.green;
            Handles.DrawAAPolyLine(3f, pointsF1);
            Handles.color = Color.blue;
            Handles.DrawAAPolyLine(3f, pointsF2);
            EditorGUI.LabelField(new Rect(xF1 + 10f, yMin + 10f, 200f, 20f), $"f1: {(int)result.formant.f1} Hz");
            EditorGUI.LabelField(new Rect(xF2 + 10f, yMin + 30f, 200f, 20f), $"f2: {(int)result.formant.f2} Hz");
        }

        Handles.color = origColor;
    }
}

}
