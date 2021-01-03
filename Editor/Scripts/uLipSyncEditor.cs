using UnityEngine;
using UnityEditor;

namespace uLipSync
{

[CustomEditor(typeof(uLipSync))]
public class uLipSyncEditor : Editor
{
    uLipSync lipSync { get { return target as uLipSync; } }
    Profile profile { get { return lipSync.profile; } }

    Editor profileEditor_;
    Editor configEditor_;

    static bool showLpc = true;
    static bool showDLpc = false;
    static bool showFft = false;
    static bool showFormant = true;

    void OnEnable()
    {
        if (lipSync.profile == null)
        {
            lipSync.profile = EditorUtil.FindAsset<Profile>(Common.defaultProfileWoman);
        }

        if (lipSync.config == null)
        {
            lipSync.config = EditorUtil.FindAsset<Config>(Common.defaultConfig);
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

        RequireRepaintIfNeeded();
    }

    void RequireRepaintIfNeeded()
    {
        if (Application.isPlaying && EditorUtil.IsFoldOutOpened("Visualizer"))
        {
            Repaint();
        }
    }

    void DrawProfile()
    {
        if (EditorUtil.Foldout("Profile", true))
        {
            ++EditorGUI.indentLevel;

            EditorUtil.DrawProperty(serializedObject, nameof(lipSync.profile));
            DrawSetProfileButtons();

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

    void DrawSetProfileButtons()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Man", EditorStyles.miniButtonLeft, GUILayout.Width(80)))
        {
            lipSync.profile = EditorUtil.FindAsset<Profile>(Common.defaultProfileMan);
        }
        if (GUILayout.Button("Woman", EditorStyles.miniButtonMid, GUILayout.Width(80)))
        {
            lipSync.profile = EditorUtil.FindAsset<Profile>(Common.defaultProfileWoman);
        }
        if (GUILayout.Button("Create", EditorStyles.miniButtonRight, GUILayout.Width(80)))
        {
            lipSync.profile = EditorUtil.CreateAssetInRoot<Profile>($"{Common.assetName}-Profile-New");
        }
        EditorGUILayout.EndHorizontal();
    }

    void DrawConfig()
    {
        if (EditorUtil.Foldout("Config", false))
        {
            ++EditorGUI.indentLevel;

            EditorUtil.DrawProperty(serializedObject, nameof(lipSync.config));
            DrawSetConfigButtons();

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

    void DrawSetConfigButtons()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Default", EditorStyles.miniButtonLeft, GUILayout.Width(80)))
        {
            lipSync.config = EditorUtil.FindAsset<Config>(Common.defaultConfig);
        }
        if (GUILayout.Button("Calibration", EditorStyles.miniButtonMid, GUILayout.Width(80)))
        {
            lipSync.config = EditorUtil.FindAsset<Config>(Common.calibrationConfig);
        }
        if (GUILayout.Button("Create", EditorStyles.miniButtonRight, GUILayout.Width(80)))
        {
            lipSync.config = EditorUtil.CreateAssetInRoot<Config>($"{Common.assetName}-Config-New");
        }
        EditorGUILayout.EndHorizontal();
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

            if (EditorUtil.SimpleFoldout("Volume", true))
            {
                ++EditorGUI.indentLevel;

                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("Normalized Volume");
                    var rect = EditorGUILayout.GetControlRect(true);
                    rect.y += rect.height * 0.3f;
                    rect.height *= 0.4f;
                    Handles.DrawSolidRectangleWithOutline(rect, new Color(0f, 0f, 0f, 0.2f), new Color(0f, 0f, 0f, 0.5f));
                    rect.width -= 2;
                    rect.width *= Mathf.Clamp(lipSync.result.volume, 0f, 1f);
                    rect.height -= 2;
                    rect.y += 1;
                    rect.x += 1;
                    Handles.DrawSolidRectangleWithOutline(rect, Color.green, new Color(0f, 0f, 0f, 0f));
                    EditorGUILayout.EndHorizontal();
                }

                EditorUtil.DrawProperty(serializedObject, nameof(lipSync.autoVolume));

                if (lipSync.autoVolume)
                {
                    EditorUtil.DrawProperty(serializedObject, nameof(lipSync.minVolume));
                    EditorUtil.DrawProperty(serializedObject, nameof(lipSync.autoVolumeAmp));
                    EditorUtil.DrawProperty(serializedObject, nameof(lipSync.autoVolumeFilter));
                }
                else
                {
                    EditorUtil.DrawProperty(serializedObject, nameof(lipSync.minVolume));
                    EditorUtil.DrawProperty(serializedObject, nameof(lipSync.maxVolume));
                }

                --EditorGUI.indentLevel;
            }
            if (EditorUtil.SimpleFoldout("Smoothness", true))
            {
                ++EditorGUI.indentLevel;
                EditorUtil.DrawProperty(serializedObject, nameof(lipSync.openSmoothness));
                EditorUtil.DrawProperty(serializedObject, nameof(lipSync.closeSmoothness));
                EditorUtil.DrawProperty(serializedObject, nameof(lipSync.vowelTransitionSmoothness));
                --EditorGUI.indentLevel;
            }
            if (EditorUtil.SimpleFoldout("Output", true))
            {
                ++EditorGUI.indentLevel;
                EditorUtil.DrawProperty(serializedObject, nameof(lipSync.outputSoundGain));
                --EditorGUI.indentLevel;
            }

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
        var range = new Vector2(lipSync.maxFreq, 1f);

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
        float df = (float)lipSync.maxFreq / H.Length;
        float nf = range.x / df;
        int n = (int)nf;

        if (showFft)
        {
            var spectrum = lipSync.fftDataEditor;
            float maxSpectrum = Algorithm.GetMaxValue(ref spectrum);
            float dfFft = (float)AudioSettings.outputSampleRate / spectrum.Length;
            float nfFft = range.x / dfFft;
            int nFft = (int)nfFft;
            var pointsFft = new Vector3[nFft];
            float min = Mathf.Log10(1e-3f);
            for (int i = 0; i < nFft && i < spectrum.Length; ++i)
            {
                float val = spectrum[i] / maxSpectrum;
                val = Mathf.Log10(10f * val);
                val = (val - min) / (1f - min);
                val = Mathf.Max(val, 0f);
                float x = xMin + width * dfFft * i / lipSync.maxFreq;
                float y = yMax - height * val;
                pointsFft[i] = new Vector3(x, y, 0f);
            }

            Handles.color = Color.gray;
            Handles.DrawAAPolyLine(3f, pointsFft);
        }

        if (showDLpc)
        {
            var ddH = lipSync.ddLpcSpectralEnvelopeForEditorOnly;
            float maxDdH = Algorithm.GetMaxValue(ref ddH);
            var points = new Vector3[n];
            float min = Mathf.Log10(1e-8f);
            for (int i = 0; i < n && i < ddH.Length; ++i)
            {
                float val = ddH[i] / maxDdH;
                val = Mathf.Log10(10f * val);
                val = (val - min) / (1f - min);
                val = Mathf.Max(val, 0f);
                float x = xMin + width * df * i / lipSync.maxFreq;
                float y = yMax - height * val;
                points[i] = new Vector3(x, y, 0f);
            }

            Handles.color = new Color(0f, 0f, 1f, 0.2f);
            Handles.DrawAAPolyLine(3f, points);
        }

        if (showLpc)
        {
            float maxH = Algorithm.GetMaxValue(ref H);
            var points = new Vector3[n];
            float min = Mathf.Log10(1e-3f);
            for (int i = 0; i < n && i < H.Length; ++i)
            {
                float val = H[i] / maxH;
                val = Mathf.Log10(10f * val);
                val = (val - min) / (1f - min);
                val = Mathf.Max(val, 0f);
                float x = xMin + width * df * i / lipSync.maxFreq;
                float y = yMax - height * val;
                points[i] = new Vector3(x, y, 0f);
            }

            Handles.color = Color.red;
            Handles.DrawAAPolyLine(5f, points);
        }

        if (showFormant)
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

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        showLpc = EditorGUILayout.ToggleLeft("LPC", showLpc, GUILayout.MaxWidth(100));
        showDLpc = EditorGUILayout.ToggleLeft("dLPC", showDLpc, GUILayout.MaxWidth(100));
        showFft = EditorGUILayout.ToggleLeft("FFT", showFft, GUILayout.MaxWidth(100));
        showFormant = EditorGUILayout.ToggleLeft("Formant", showFormant, GUILayout.MaxWidth(100));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        Handles.color = origColor;
    }
}

}
