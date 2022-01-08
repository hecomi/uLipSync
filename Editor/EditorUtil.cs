using UnityEngine;
using UnityEditor;
using System.Linq;

namespace uLipSync
{

public static class EditorUtil
{
    public static float lineHeightWithMargin 
    { 
        get=>
            EditorGUIUtility.singleLineHeight + 
            EditorGUIUtility.standardVerticalSpacing;
    }

    public static string GetKey(string title, string category)
    {
        return $"{Common.assetName}-{category}-{title}";
    }

    private static string GetFoldOutKey(string title)
    {
        return GetKey(title, "FoldOut");
    }

    public static bool EditorOnlyToggle(string title, string category, bool initialState)
    {
        var keyTitle = title.Replace(" ", "_");
        var key = GetKey(keyTitle, category);
        var value = EditorPrefs.GetBool(key, initialState);
        var newValue = EditorGUILayout.Toggle(title, value);
        if (newValue != value)
        {
            EditorPrefs.SetBool(key, newValue);
        }
        return newValue;
    }

    public static bool IsFoldOutOpened(string title, bool initialState = false, string additionalKey = "")
    {
        var key = GetFoldOutKey(title + additionalKey);
        if (!EditorPrefs.HasKey(key)) return initialState;
        return EditorPrefs.GetBool(key);
    }

    public static bool Foldout(string title, bool initialState, string additionalKey = "")
    {
        var style = new GUIStyle("ShurikenModuleTitle");
        style.font = new GUIStyle(EditorStyles.label).font;
        style.border = new RectOffset(15, 7, 4, 4);
        style.fixedHeight = 22;
        style.contentOffset = new Vector2(20f, -2f);
        style.margin = new RectOffset((EditorGUI.indentLevel + 1) * 16, 0, 0, 0);

        var key = GetFoldOutKey(title + additionalKey);
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

    public static bool SimpleFoldout(Rect rect, string title, bool initialState, string additionalKey = "")
    {
        var key = GetFoldOutKey(title + additionalKey);
        bool display = EditorPrefs.GetBool(key, initialState);
        bool newDisplay = EditorGUI.Foldout(rect, display, title);
        if (newDisplay != display) EditorPrefs.SetBool(key, newDisplay);
        return newDisplay;
    }

    public static bool SimpleFoldout(string title, bool initialState, string additionalKey = "")
    {
        var key = GetFoldOutKey(title + additionalKey);
        bool display = EditorPrefs.GetBool(key, initialState);
        bool newDisplay = EditorGUILayout.Foldout(display, title, EditorStyles.foldoutHeader);
        if (newDisplay != display) EditorPrefs.SetBool(key, newDisplay);
        return newDisplay;
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

    public static void DrawMfcc(float[] array, float max, float min, float height)
    {
        var area = GUILayoutUtility.GetRect(Screen.width, height);
        area = EditorGUI.IndentedRect(area);

        var width = area.width / 12;
        var maxMinusMin = max - min;
        for (int i = 0; i < 12; ++i)
        {
            var x = width * i;
            var rect = new Rect(area.x + x, area.y, width, height);
            var value = (array[i] - min) / maxMinusMin;
            var color = ToRGB(value);
            Handles.DrawSolidRectangleWithOutline(rect, color, color);
        }
    }

    public static void DrawMfcc(Rect area, float[] array, float max, float min, float height)
    {
        area = EditorGUI.IndentedRect(area);
        var width = area.width / 12;
        var maxMinusMin = max - min;
        for (int i = 0; i < 12; ++i)
        {
            var x = width * i;
            var rect = new Rect(area.x + x, area.y, width, height);
            var value = (array[i] - min) / maxMinusMin;
            var color = ToRGB(value);
            Handles.DrawSolidRectangleWithOutline(rect, color, color);
        }
    }

    public static void DrawBackgroundRect(Rect rect, Color bg, Color line)
    {
        Handles.DrawSolidRectangleWithOutline(rect, bg, line);
    }

    public static void DrawBackgroundRect(Rect rect)
    {
        DrawBackgroundRect(
            rect, 
            new Color(0f, 0f, 0f, 0.2f),
            new Color(1f, 1f, 1f, 0.2f));
    }

    public class DrawWaveOption
    {
        public System.Func<float, Color> colorFunc = null;
        public float waveScale = 0.95f;
    }

    public static void DrawWave(Rect rect, AudioClip clip, DrawWaveOption option = null)
    {
        if (!clip) return;

        var minMaxData = AudioUtil.GetMinMaxData(clip);
        int channels = clip.channels;
        var height = (float)rect.height / channels;
        int samples = (minMaxData == null) ? 0 : (minMaxData.Length / (2 * channels));

        for (int ch = 0; ch < channels; ch++)
        {
            var chRect = new Rect(rect.x, rect.y + height * ch, rect.width, height);
            var curveColor = new Color(1.0f, 140.0f / 255.0f, 0.0f, 1.0f);
            AudioCurveRendering.AudioMinMaxCurveAndColorEvaluator dlg = delegate(
                float x, 
                out Color col, 
                out float minValue, 
                out float maxValue)
            {
                col = (option != null && option.colorFunc != null) ? 
                    option.colorFunc(x) : 
                    curveColor;

                if (samples <= 0)
                {
                    minValue = 0.0f;
                    maxValue = 0.0f;
                }
                else
                {
                    float p = Mathf.Clamp(x * (samples - 2), 0.0f, samples - 2);
                    int i = (int)Mathf.Floor(p);
                    int offset1 = (i * channels + ch) * 2;
                    int offset2 = offset1 + channels * 2;
                    var scale = option != null ? option.waveScale : 0.95f;
                    minValue = Mathf.Min(minMaxData[offset1 + 1], minMaxData[offset2 + 1]) * scale;
                    maxValue = Mathf.Max(minMaxData[offset1 + 0], minMaxData[offset2 + 0]) * scale;
                    if (minValue > maxValue) { float tmp = minValue; minValue = maxValue; maxValue = tmp; }
                }
            };

            AudioCurveRendering.DrawMinMaxFilledCurve(rect, dlg);
        }
    }

    public static Color ToRGB(float hue)
    {
        hue = 1f - hue;
        hue = hue * 5f;
        var x = 1 - Mathf.Abs(hue % 2f - 1f);
        return
            hue < 1f ? new Color(1f, x, 0f) :
            hue < 2f ? new Color(x, 1f, 0f) :
            hue < 3f ? new Color(0f, 1f, x) :
            hue < 4f ? new Color(0f, x, 1f) :
            new Color(x * 0.5f, 0f, 0.5f);
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
}

}
