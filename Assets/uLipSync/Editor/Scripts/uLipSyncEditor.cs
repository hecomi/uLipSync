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

    Editor profileEditor_;
    Editor configEditor_;

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
        serializedObject.Update();

        DrawProfile();
        DrawConfig();
        DrawCallback();
        DrawParameter();
        DrawVisualizer();

        serializedObject.ApplyModifiedProperties();
    }

    public override bool RequiresConstantRepaint()
    {
        return uLipSync.foldOutVisualizer;
    }

    void DrawProfile()
    {
        if (EditorUtil.Foldout("Profile", true))
        {
            ++EditorGUI.indentLevel;
            EditorUtil.DrawProperty(serializedObject, nameof(lipSync.profile));
            if (lipSync.profile) 
            {
                CreateCachedEditor(lipSync.profile, typeof(ProfileEditor), ref profileEditor_);
                var editor = profileEditor_ as ProfileEditor;
                if (editor)
                {
                    editor.Draw(true, false);
                }
            }
            --EditorGUI.indentLevel;
            EditorGUILayout.Separator();
        }
    }

    void DrawConfig()
    {
        if (EditorUtil.Foldout("Config", false))
        {
            ++EditorGUI.indentLevel;
            EditorUtil.DrawProperty(serializedObject, nameof(lipSync.config));
            if (lipSync.config)
            {
                CreateCachedEditor(lipSync.config, typeof(ConfigEditor), ref configEditor_);
                if (configEditor_)
                {
                    configEditor_.OnInspectorGUI();
                }
            }
            --EditorGUI.indentLevel;
            EditorGUILayout.Separator();
        }
    }

    void DrawCallback()
    {
        if (EditorUtil.Foldout("Callback", true))
        {
            ++EditorGUI.indentLevel;
            EditorUtil.DrawProperty(serializedObject, nameof(lipSync.onLipSyncUpdate));
            --EditorGUI.indentLevel;
            EditorGUILayout.Separator();
        }
    }

    void DrawParameter()
    {
        if (EditorUtil.Foldout("Parameter", true))
        {
            ++EditorGUI.indentLevel;
            EditorUtil.DrawProperty(serializedObject, nameof(lipSync.outputSoundGain));
            EditorUtil.DrawProperty(serializedObject, nameof(lipSync.openFilter));
            EditorUtil.DrawProperty(serializedObject, nameof(lipSync.closeFilter));
            EditorUtil.DrawProperty(serializedObject, nameof(lipSync.minVolume));
            EditorUtil.DrawProperty(serializedObject, nameof(lipSync.maxVolume));
            --EditorGUI.indentLevel;
            EditorGUILayout.Separator();
        }
    }

    void DrawVisualizer()
    {
        if (EditorUtil.Foldout("Visualizer", false))
        {
            ++EditorGUI.indentLevel;

            if (EditorUtil.SimpleFoldout("Formant Map", true))
            {
                ++EditorGUI.indentLevel;
                EditorUtil.DrawFormants(profile, lipSync.result);
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.Separator();

            if (EditorUtil.SimpleFoldout("LPC Spectral Envelope", true))
            {
                ++EditorGUI.indentLevel;
                DrawLPCSpectralEnvelope();
                --EditorGUI.indentLevel;
            }

            EditorGUILayout.Separator();

            --EditorGUI.indentLevel;
        }
    }

    void DrawLPCSpectralEnvelope()
    {
        var origColor = Handles.color;

        var area = GUILayoutUtility.GetRect(Screen.width, 300f);
        area = EditorGUI.IndentedRect(area);
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
        float df = (float)AudioSettings.outputSampleRate / H.Length;
        float fn = range.x / df;
        int n = Mathf.CeilToInt(fn);
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
